using AgriIDMS.Application.DTOs.Payment;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PaymentService> _logger;
        private readonly INotificationService _notificationService;
        private readonly PayOSClient _payOSClient;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IOrderRepository orderRepo,
            IUnitOfWork uow,
            ILogger<PaymentService> logger,
            INotificationService notificationService,
            PayOSClient payOSClient,
            IConfiguration config)
        {
            _paymentRepo = paymentRepo;
            _orderRepo = orderRepo;
            _uow = uow;
            _logger = logger;
            _notificationService = notificationService;
            _payOSClient = payOSClient;
            _returnUrl = config["PayOS:ReturnUrl"]!;
            _cancelUrl = config["PayOS:CancelUrl"]!;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequest request, string userId)
        {
            var order = await _orderRepo.GetByIdWithPaymentsAsync(request.OrderId)
                ?? throw new NotFoundException($"Order #{request.OrderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền thanh toán đơn hàng này");

            if (order.Status != OrderStatus.Confirmed)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể thanh toán đơn hàng ở trạng thái Confirmed. Hiện tại: {order.Status}");

            if (!order.PaymentTiming.HasValue)
                throw new InvalidBusinessRuleException(
                    "Vui lòng chọn hình thức thanh toán (trả trước hoặc trả sau) trước khi thanh toán.");

            await _uow.BeginTransactionAsync();
            try
            {
                var hasPaid = await _paymentRepo.HasPaidPaymentAsync(order.Id);
                if (hasPaid)
                    throw new InvalidBusinessRuleException("Đơn hàng đã được thanh toán thành công");

                var result = request.PaymentMethod switch
                {
                    PaymentMethod.Cash => await ProcessCashPaymentAsync(order),
                    PaymentMethod.Banking => await ProcessBankingAsync(order),
                    _ => throw new InvalidBusinessRuleException(
                        $"Phương thức thanh toán '{request.PaymentMethod}' chưa được hỗ trợ. Chỉ hỗ trợ Cash hoặc Banking.")
                };

                await _uow.CommitAsync();
                return result;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        // ===================== Cash (tiền mặt) =====================

        private async Task<PaymentResponseDto> ProcessCashPaymentAsync(Order order)
        {
            _logger.LogInformation("Creating Cash payment for order {OrderId}", order.Id);

            if (await _paymentRepo.HasPendingCashPaymentAsync(order.Id))
                throw new InvalidBusinessRuleException("Đơn đã có thanh toán tiền mặt đang chờ xác nhận. Không tạo trùng.");

            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.Cash,
                PaymentStatus = PaymentStatus.Pending,
                Amount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Cash payment {PaymentId} created for order {OrderId}", payment.Id, order.Id);

            return MapToDto(payment);
        }

        public async Task<PaymentResponseDto> ConfirmCashPaymentPaidAsync(int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                ?? throw new NotFoundException($"Payment #{paymentId} không tồn tại");

            if (payment.PaymentMethod != PaymentMethod.Cash)
                throw new InvalidBusinessRuleException("Chỉ áp dụng cho thanh toán tiền mặt (Cash)");

            if (payment.PaymentStatus == PaymentStatus.Paid)
                throw new InvalidBusinessRuleException("Payment này đã được xác nhận thành công rồi");

            if (payment.PaymentStatus != PaymentStatus.Pending)
                throw new InvalidBusinessRuleException(
                    $"Không thể xác nhận payment ở trạng thái {payment.PaymentStatus}");

            var order = payment.Order
                ?? throw new NotFoundException($"Order liên kết với payment #{paymentId} không tồn tại");

            payment.PaymentStatus = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;

            // TakeAway: mặc định thanh toán xong = hoàn tất tại quầy. PayBefore (thu trước pick): chờ xuất kho rồi mới Delivered.
            if (order.FulfillmentType == FulfillmentType.TakeAway && order.Status != OrderStatus.Delivered)
            {
                if (!DeferTakeAwayDeliveredUntilAfterExport(order))
                {
                    order.Status = OrderStatus.Delivered;
                    order.DeliveredAt = DateTime.UtcNow;
                }
            }

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Cash payment {PaymentId} confirmed. Order {OrderId} status {Status}",
                payment.Id, order.Id, order.Status);

            await _notificationService.NotifyOrderPaidAsync(order.Id);

            return MapToDto(payment);
        }

        /// <summary>POS TakeAway + trả trước pick: không chuyển Delivered khi vừa xác nhận tiền mặt.</summary>
        private static bool DeferTakeAwayDeliveredUntilAfterExport(Order order) =>
            order.Source == OrderSource.POS
            && order.FulfillmentType == FulfillmentType.TakeAway
            && order.PaymentTiming == PaymentTiming.PayBefore;

        // ===================== Banking (PayOS) =====================

        private async Task<PaymentResponseDto> ProcessBankingAsync(Order order)
        {
            _logger.LogInformation("Creating Banking (PayOS) payment for order {OrderId}", order.Id);

            var orderCode = GenerateOrderCode(order.Id);

            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.Banking,
                PaymentStatus = PaymentStatus.Processing,
                Amount = order.TotalAmount,
                TransactionCode = orderCode.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _uow.SaveChangesAsync();

            var description = $"DH{order.Id}";
            if (description.Length > 25)
                description = description[..25];

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)Math.Round(order.TotalAmount),
                Description = description,
                ReturnUrl = _returnUrl,
                CancelUrl = _cancelUrl
            };

            CreatePaymentLinkResponse paymentLink;
            try
            {
                paymentLink = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS CreatePaymentLink failed for order {OrderId}", order.Id);
                payment.PaymentStatus = PaymentStatus.Failed;
                await _uow.SaveChangesAsync();
                await _notificationService.NotifyOrderPaymentFailedAsync(order.Id);
                throw new InvalidBusinessRuleException(
                    $"Không thể tạo link thanh toán PayOS: {ex.Message}");
            }

            _logger.LogInformation(
                "PayOS payment link created. Payment {PaymentId}, OrderCode {OrderCode}, CheckoutUrl: {Url}",
                payment.Id, orderCode, paymentLink.CheckoutUrl);

            var dto = MapToDto(payment);
            dto.CheckoutUrl = paymentLink.CheckoutUrl;
            return dto;
        }
        
        private static long GenerateOrderCode(int orderId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 100000;
            return orderId * 100000L + timestamp;
        }

        // ===================== Query =====================

        public async Task<PaymentResponseDto> GetLatestPaymentAsync(int orderId, string userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền xem thanh toán của đơn hàng này");

            var payment = await _paymentRepo.GetLatestByOrderIdAsync(orderId)
                ?? throw new NotFoundException("Không tìm thấy thanh toán cho đơn hàng này");

            return MapToDto(payment);
        }

        public async Task<IList<PendingCashPaymentItemDto>> GetPendingCashPaymentsAsync(GetPendingCashPaymentsQuery query)
        {
            query ??= new GetPendingCashPaymentsQuery();
            var skip = Math.Max(0, query.Skip);
            var take = Math.Clamp(query.Take, 1, 200);

            var payments = await _paymentRepo.GetPaymentsByMethodAndStatusAsync(
                PaymentMethod.Cash,
                PaymentStatus.Pending,
                query.OrderId,
                query.CustomerUserId,
                skip,
                take);

            return payments.Select(p => new PendingCashPaymentItemDto
            {
                PaymentId = p.Id,
                OrderId = p.OrderId,
                CustomerUserId = p.Order?.UserId ?? string.Empty,
                Amount = p.Amount,
                PaymentStatus = p.PaymentStatus.ToString(),
                PaymentMethod = p.PaymentMethod.ToString(),
                OrderStatus = p.Order?.Status.ToString() ?? string.Empty,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        // ===================== Webhook (PayOS) =====================

        public async Task HandlePayOSWebhookAsync(string webhookBody)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            Webhook? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<Webhook>(webhookBody, jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Cannot deserialize PayOS webhook body");
                return;
            }

            if (webhook == null)
            {
                _logger.LogWarning("PayOS webhook body is null after deserialization");
                return;
            }

            WebhookData verifiedData;
            try
            {
                verifiedData = await _payOSClient.Webhooks.VerifyAsync(webhook);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PayOS webhook signature verification failed");
                return;
            }

            var payment = await _paymentRepo.GetByTransactionCodeAsync(verifiedData.OrderCode.ToString());
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for PayOS orderCode {OrderCode}", verifiedData.OrderCode);
                return;
            }

            if (payment.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.LogInformation("Payment {PaymentId} already Paid, skipping webhook", payment.Id);
                return;
            }

            if (webhook.Success && webhook.Code == "00")
            {
                payment.PaymentStatus = PaymentStatus.Paid;
                payment.PaidAt = DateTime.UtcNow;

                if (payment.Order != null
                    && payment.Order.FulfillmentType == FulfillmentType.TakeAway
                    && payment.Order.Status != OrderStatus.Delivered)
                {
                    if (!DeferTakeAwayDeliveredUntilAfterExport(payment.Order))
                    {
                        payment.Order.Status = OrderStatus.Delivered;
                        payment.Order.DeliveredAt = DateTime.UtcNow;
                    }
                }

                _logger.LogInformation(
                    "Banking payment {PaymentId} succeeded. Order {OrderId} payment marked Paid",
                    payment.Id, payment.OrderId);
            }
            else
            {
                payment.PaymentStatus = PaymentStatus.Failed;
                _logger.LogWarning(
                    "Banking payment {PaymentId} failed. WebhookCode: {Code}",
                    payment.Id, webhook.Code);
            }

            await _uow.SaveChangesAsync();

            if (payment.PaymentStatus == PaymentStatus.Paid)
                await _notificationService.NotifyOrderPaidAsync(payment.OrderId);
            else if (payment.PaymentStatus == PaymentStatus.Failed)
                await _notificationService.NotifyOrderPaymentFailedAsync(payment.OrderId);
        }

        // ===================== Cancel Banking =====================

        public async Task<PaymentResponseDto> CancelBankingPaymentAsync(int paymentId, string userId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                ?? throw new NotFoundException($"Payment #{paymentId} không tồn tại");

            if (payment.Order == null)
                throw new NotFoundException($"Order liên kết với payment #{paymentId} không tồn tại");

            if (payment.Order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền hủy thanh toán này");

            if (payment.PaymentMethod != PaymentMethod.Banking)
                throw new InvalidBusinessRuleException("Chỉ có thể hủy thanh toán Banking");

            if (payment.PaymentStatus != PaymentStatus.Processing)
                throw new InvalidBusinessRuleException(
                    $"Không thể hủy payment ở trạng thái {payment.PaymentStatus}. Chỉ hủy được khi đang Processing.");

            if (!string.IsNullOrEmpty(payment.TransactionCode))
            {
                try
                {
                    var orderCode = long.Parse(payment.TransactionCode);
                    await _payOSClient.PaymentRequests.CancelAsync(orderCode, "Người dùng hủy thanh toán");
                    _logger.LogInformation("PayOS payment link cancelled for orderCode {OrderCode}", orderCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cancel PayOS payment link for payment {PaymentId}", paymentId);
                }
            }

            payment.PaymentStatus = PaymentStatus.Cancelled;
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Banking payment {PaymentId} cancelled by user", paymentId);
            await _notificationService.NotifyOrderPaymentCancelledAsync(payment.OrderId);

            return MapToDto(payment);
        }

        // ===================== Mapping =====================

        private static PaymentResponseDto MapToDto(Payment payment)
        {
            return new PaymentResponseDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                PaymentMethod = payment.PaymentMethod.ToString(),
                PaymentStatus = payment.PaymentStatus.ToString(),
                TransactionCode = payment.TransactionCode,
                Amount = payment.Amount,
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt
            };
        }
    }
}
