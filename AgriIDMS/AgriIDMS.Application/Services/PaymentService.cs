using AgriIDMS.Application.DTOs.Payment;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IOrderRepository orderRepo,
            IUnitOfWork uow,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _orderRepo = orderRepo;
            _uow = uow;
            _logger = logger;
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

            await _uow.BeginTransactionAsync();
            try
            {
                var hasSuccess = await _paymentRepo.HasSuccessPaymentAsync(order.Id);
                if (hasSuccess)
                    throw new InvalidBusinessRuleException("Đơn hàng đã được thanh toán thành công");

                var result = request.PaymentMethod switch
                {
                    PaymentMethod.COD => await ProcessCODAsync(order),
                    PaymentMethod.Banking => await ProcessBankingAsync(order),
                    _ => throw new InvalidBusinessRuleException(
                        $"Phương thức thanh toán '{request.PaymentMethod}' chưa được hỗ trợ. Chỉ hỗ trợ COD hoặc Banking.")
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

        // ===================== COD =====================

        private async Task<PaymentResponseDto> ProcessCODAsync(Order order)
        {
            _logger.LogInformation("Creating COD payment for order {OrderId}", order.Id);

            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Pending,
                Amount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("COD payment {PaymentId} created for order {OrderId}", payment.Id, order.Id);

            return MapToDto(payment);
        }

        public async Task<PaymentResponseDto> ConfirmCODPaidAsync(int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                ?? throw new NotFoundException($"Payment #{paymentId} không tồn tại");

            if (payment.PaymentMethod != PaymentMethod.COD)
                throw new InvalidBusinessRuleException("Chỉ áp dụng cho thanh toán COD");

            if (payment.PaymentStatus == PaymentStatus.Success)
                throw new InvalidBusinessRuleException("Payment này đã được xác nhận thành công rồi");

            if (payment.PaymentStatus != PaymentStatus.Pending)
                throw new InvalidBusinessRuleException(
                    $"Không thể xác nhận payment ở trạng thái {payment.PaymentStatus}");

            var order = payment.Order
                ?? throw new NotFoundException($"Order liên kết với payment #{paymentId} không tồn tại");

            payment.PaymentStatus = PaymentStatus.Success;
            payment.PaidAt = DateTime.UtcNow;
            order.Status = OrderStatus.Completed;

            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "COD payment {PaymentId} confirmed. Order {OrderId} → Completed",
                payment.Id, order.Id);

            return MapToDto(payment);
        }

        // ===================== Banking (PayOS) - placeholder =====================

        private Task<PaymentResponseDto> ProcessBankingAsync(Order order)
        {
            // TODO: Tích hợp PayOS ở giai đoạn sau
            throw new InvalidBusinessRuleException("Thanh toán Banking (PayOS) chưa được triển khai");
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
