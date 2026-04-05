using AgriIDMS.Application.DTOs.Export;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.OrderPayments;
using AgriIDMS.Domain;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class ExportService : IExportService
    {
        private const decimal DefaultColdStorageHours = 48m;
        private readonly IExportReceiptRepository _exportRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IOrderAllocationRepository _allocationRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ExportService> _logger;
        private readonly INotificationService _notificationService;

        public ExportService(
            IExportReceiptRepository exportRepo,
            IOrderRepository orderRepo,
            IOrderAllocationRepository allocationRepo,
            IBoxRepository boxRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            IUnitOfWork uow,
            ILogger<ExportService> logger,
            INotificationService notificationService)
        {
            _exportRepo = exportRepo;
            _orderRepo = orderRepo;
            _allocationRepo = allocationRepo;
            _boxRepo = boxRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _uow = uow;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<ExportReceiptResponseDto> CreateExportReceiptAsync(int orderId, string userId)
        {
            var order = await _orderRepo.GetByIdWithPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!CanCreateExportReceipt(order))
                throw new InvalidBusinessRuleException(
                    "Chỉ tạo phiếu xuất khi đơn Confirmed, khách đã chọn trả trước/trả sau, và thanh toán đủ điều kiện: "
                    + "PayBefore phải đã Paid; PayAfter cho phép chưa thanh toán, hoặc Cash Pending, hoặc đã Paid. "
                    + $"Hiện tại trạng thái đơn: {order.Status}.");

            var alreadyExists = await _exportRepo.ExistsForOrderAsync(orderId);
            if (alreadyExists)
                throw new InvalidBusinessRuleException(
                    "Đơn hàng này đã có phiếu xuất kho (chưa bị hủy). Không thể tạo trùng.");

            var allocations = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Reserved);
            if (!allocations.Any())
                throw new InvalidBusinessRuleException(
                    "Không tìm thấy allocation (Reserved) cho đơn hàng này.");

            var exportCode = $"EXP-{orderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var receipt = new ExportReceipt
            {
                ExportCode = exportCode,
                OrderId = orderId,
                Status = ExportStatus.PendingPick,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var alloc in allocations)
            {
                receipt.Details.Add(new ExportDetail
                {
                    BoxId = alloc.BoxId,
                    ActualQuantity = alloc.ReservedQuantity
                });
            }

            await _exportRepo.AddAsync(receipt);
            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "ExportReceipt {ExportCode} created for order {OrderId} with {BoxCount} boxes",
                exportCode, orderId, allocations.Count);

            var saved = await _exportRepo.GetByIdWithDetailsAsync(receipt.Id);
            return MapToDto(saved!);
        }

        public async Task<ExportReceiptResponseDto> ConfirmPickAsync(int exportId, string userId)
        {
            var receipt = await _exportRepo.GetByIdWithDetailsAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");

            if (receipt.Status != ExportStatus.PendingPick)
                throw new InvalidBusinessRuleException(
                    $"Chỉ xác nhận pick khi phiếu ở trạng thái PendingPick. Hiện tại: {receipt.Status}");

            var allocations = await _allocationRepo.GetByOrderIdAsync(receipt.OrderId, AllocationStatus.Reserved);
            var allocByBox = allocations.ToDictionary(a => a.BoxId);

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var detail in receipt.Details)
                {
                    if (detail.Box != null)
                    {
                        if (!IsColdStorageEligibleForExport(detail.Box, out var notEligibleMessage))
                            throw new InvalidBusinessRuleException(notEligibleMessage!);

                        detail.Box.Status = BoxStatus.Picking;
                        await _boxRepo.UpdateAsync(detail.Box);
                    }

                    if (allocByBox.TryGetValue(detail.BoxId, out var alloc))
                    {
                        alloc.Status = AllocationStatus.Picked;
                        alloc.PickedQuantity = alloc.ReservedQuantity;
                    }
                }

                receipt.Status = ExportStatus.ReadyToExport;
                await _uow.CommitAsync();

                _logger.LogInformation(
                    "ExportReceipt {ExportId} confirmed pick → ReadyToExport. {Count} boxes picking.",
                    exportId, receipt.Details.Count);

                return MapToDto(receipt);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<ExportReceiptResponseDto> ApproveExportAsync(int exportId, string userId)
        {
            var receipt = await _exportRepo.GetByIdWithDetailsAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");

            if (receipt.Status != ExportStatus.ReadyToExport)
                throw new InvalidBusinessRuleException(
                    $"Chỉ duyệt phiếu xuất ở trạng thái ReadyToExport. Hiện tại: {receipt.Status}");

            await _uow.BeginTransactionAsync();
            try
            {
                var exportTransactions = new List<InventoryTransaction>();

                foreach (var detail in receipt.Details)
                {
                    if (detail.Box == null) continue;

                    var box = detail.Box;
                    var fromSlotId = box.SlotId;

                    var slot = box.Slot;
                    if (slot != null)
                    {
                        slot.CurrentCapacity -= box.Weight;
                        if (slot.CurrentCapacity < 0)
                            slot.CurrentCapacity = 0;
                    }

                    exportTransactions.Add(new InventoryTransaction
                    {
                        BoxId = box.Id,
                        TransactionType = InventoryTransactionType.Export,
                        ReferenceType = ReferenceType.GoodsIssue,
                        ExportReceiptId = receipt.Id,
                        FromSlotId = fromSlotId,
                        ToSlotId = null,
                        Quantity = box.Weight,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    });

                    box.Status = BoxStatus.Exported;
                    box.SlotId = null;
                    await _boxRepo.UpdateAsync(box);
                }

                await _inventoryTranRepo.AddRangeAsync(exportTransactions);

                receipt.Status = ExportStatus.Approved;
                var ord = receipt.Order;
                if (ord.FulfillmentType == FulfillmentType.Delivery)
                {
                    ord.Status = OrderStatus.ApprovedExport;
                    ord.ShippingStatus = ShippingStatus.ShippingPendingPickup;
                }
                else if (ord.Source == OrderSource.POS
                         && ord.FulfillmentType == FulfillmentType.TakeAway
                         && ord.Payments != null
                         && ord.Payments.Any(p => p.PaymentStatus == PaymentStatus.Paid)
                         && ord.Status != OrderStatus.Delivered)
                {
                    // TakeAway POS: đã Paid mới xuất được; hoàn tất tại quầy — không dùng tiến trình ship (khách tự mang).
                    ord.Status = OrderStatus.Delivered;
                    ord.DeliveredAt = DateTime.UtcNow;
                    ord.ShippingStatus = ShippingStatus.None;
                }

                await _uow.CommitAsync();

                _logger.LogInformation(
                    "ExportReceipt {ExportId} approved. {Count} boxes exported. Order {OrderId} status {OrderStatus}",
                    exportId, receipt.Details.Count, receipt.OrderId, ord.Status);

                await _notificationService.NotifyExportApprovedAsync(receipt.Id);

                return MapToDto(receipt);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<ExportReceiptResponseDto> CancelExportAsync(int exportId, string userId)
        {
            var receipt = await _exportRepo.GetByIdWithDetailsAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");

            if (receipt.Status == ExportStatus.Approved)
                throw new InvalidBusinessRuleException("Không thể hủy phiếu xuất đã được duyệt");

            if (receipt.Status == ExportStatus.Cancelled)
                throw new InvalidBusinessRuleException("Phiếu xuất đã bị hủy trước đó");

            var allocations = await _allocationRepo.GetByOrderIdAsync(receipt.OrderId);

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var detail in receipt.Details)
                {
                    if (detail.Box != null
                        && (detail.Box.Status == BoxStatus.Reserved || detail.Box.Status == BoxStatus.Picking))
                    {
                        detail.Box.Status = BoxStatus.Stored;
                        await _boxRepo.UpdateAsync(detail.Box);
                    }
                }

                foreach (var alloc in allocations)
                {
                    if (alloc.Status == AllocationStatus.Reserved || alloc.Status == AllocationStatus.Picked)
                        alloc.Status = AllocationStatus.Cancelled;
                }

                receipt.Status = ExportStatus.Cancelled;
                await _uow.CommitAsync();

                _logger.LogInformation(
                    "ExportReceipt {ExportId} cancelled. Boxes reverted to Stored.",
                    exportId);

                return MapToDto(receipt);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<ExportReceiptResponseDto> GetExportReceiptAsync(int exportId)
        {
            var receipt = await _exportRepo.GetByIdWithDetailsAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");

            return MapToDto(receipt);
        }

        private static ExportReceiptResponseDto MapToDto(ExportReceipt receipt)
        {
            return new ExportReceiptResponseDto
            {
                Id = receipt.Id,
                ExportCode = receipt.ExportCode,
                OrderId = receipt.OrderId,
                Status = receipt.Status.ToString(),
                CreatedBy = receipt.CreatedBy,
                CreatedAt = receipt.CreatedAt,
                Details = receipt.Details.Select(d => new ExportDetailDto
                {
                    Id = d.Id,
                    BoxId = d.BoxId,
                    BoxCode = d.Box?.BoxCode ?? "N/A",
                    ActualQuantity = d.ActualQuantity,
                    BoxStatus = d.Box?.Status.ToString() ?? "N/A"
                }).ToList()
            };
        }

        /// <summary>
        /// Chặn pick/export cho box kho lạnh khi chưa đủ thời gian lưu lạnh.
        /// Reserve vẫn cho phép để giữ hàng.
        /// </summary>
        private static bool IsColdStorageEligibleForExport(Box box, out string? message)
        {
            var warehouse = box.Slot?.Rack?.Zone?.Warehouse;
            if (warehouse == null || warehouse.TitleWarehouse != TitleWarehouse.Cold)
            {
                message = null;
                return true;
            }

            var minHours = warehouse.MinColdStorageHours ?? DefaultColdStorageHours;
            if (ColdStorageExportRule.CanExportFromCold(box.PlacedInColdAt, minHours))
            {
                message = null;
                return true;
            }

            message = ColdStorageExportRule.GetNotEligibleMessage(box.BoxCode, minHours, box.PlacedInColdAt);
            return false;
        }

        /// <summary>Xuất kho: đơn Confirmed + payment theo <see cref="PaymentTiming"/>.</summary>
        private static bool CanCreateExportReceipt(Order order)
        {
            if (order.Status != OrderStatus.Confirmed)
                return false;

            return PaymentExportRules.OrderHasExportEligiblePayments(order);
        }

        public async Task<IList<PendingApproveExportListItemDto>> GetPendingApproveExportsAsync(GetPendingApproveExportsQuery query)
        {
            query ??= new GetPendingApproveExportsQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            var list = await _exportRepo.GetReadyToExportPendingApproveAsync(skip, take, query.Sort);

            return list.Select(e => new PendingApproveExportListItemDto
            {
                ExportId = e.Id,
                ExportCode = e.ExportCode,
                OrderId = e.OrderId,
                Status = e.Status.ToString(),
                CreatedAt = e.CreatedAt,
                BoxCount = e.Details?.Count ?? 0
            }).ToList();
        }

        public async Task<IEnumerable<ExportReceiptResponseDto>> GetAllExport()
        {
            var exportsList = await _exportRepo.GetAllExport();
            return exportsList.Select(s => new ExportReceiptResponseDto
            {
                Id = s.Id,
                ExportCode = s.ExportCode,
                OrderId = s.OrderId,
                Status = s.Status.ToString(),
                CreatedBy = s.CreatedBy,
                CreatedAt = s.CreatedAt,
                Details = s.Details.Select(d => new ExportDetailDto
                {
                    Id = d.Id,
                    BoxId = d.BoxId,
                    BoxCode = d.Box?.BoxCode ?? "N/A",
                    ActualQuantity = d.ActualQuantity,
                    BoxStatus = d.Box?.Status.ToString() ?? "N/A"
                }).ToList()
            });
        }
    }
}
