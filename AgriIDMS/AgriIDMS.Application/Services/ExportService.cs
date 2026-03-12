using AgriIDMS.Application.DTOs.Export;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
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
        private readonly IExportReceiptRepository _exportRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IOrderAllocationRepository _allocationRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ExportService> _logger;
        private readonly INotificationService _notificationService;

        public ExportService(
            IExportReceiptRepository exportRepo,
            IOrderRepository orderRepo,
            IOrderAllocationRepository allocationRepo,
            IBoxRepository boxRepo,
            IUnitOfWork uow,
            ILogger<ExportService> logger,
            INotificationService notificationService)
        {
            _exportRepo = exportRepo;
            _orderRepo = orderRepo;
            _allocationRepo = allocationRepo;
            _boxRepo = boxRepo;
            _uow = uow;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<ExportReceiptResponseDto> CreateExportReceiptAsync(int orderId, string userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Status != OrderStatus.Paid)
                throw new InvalidBusinessRuleException(
                    $"Chỉ tạo phiếu xuất kho khi đơn hàng đã thanh toán (Paid). Hiện tại: {order.Status}");

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
                foreach (var detail in receipt.Details)
                {
                    if (detail.Box == null) continue;

                    var slot = detail.Box.Slot;
                    if (slot != null)
                    {
                        slot.CurrentCapacity -= detail.Box.Weight;
                        if (slot.CurrentCapacity < 0)
                            slot.CurrentCapacity = 0;
                    }

                    detail.Box.Status = BoxStatus.Exported;
                    detail.Box.SlotId = null;
                    await _boxRepo.UpdateAsync(detail.Box);
                }

                receipt.Status = ExportStatus.Approved;
                receipt.Order.Status = OrderStatus.Shipping;

                await _uow.CommitAsync();

                _logger.LogInformation(
                    "ExportReceipt {ExportId} approved. {Count} boxes exported. Order {OrderId} → Shipping",
                    exportId, receipt.Details.Count, receipt.OrderId);

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
    }
}
