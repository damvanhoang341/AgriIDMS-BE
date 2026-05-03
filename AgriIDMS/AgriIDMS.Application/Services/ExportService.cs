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
using System.Text.Json;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class ExportService : IExportService
    {
        private const decimal DefaultColdStorageHours = 48m;

        private static readonly JsonSerializerOptions PrintJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
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
            var pickWarnings = new List<string>();

            await _uow.ExecuteInRetryableTransactionAsync(async () =>
            {
                foreach (var detail in receipt.Details)
                {
                    if (detail.Box != null)
                    {
                        if (TryGetColdStoragePickNotice(detail.Box, out var notice) && !string.IsNullOrWhiteSpace(notice))
                            pickWarnings.Add(notice);

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
            });

            if (pickWarnings.Count > 0)
            {
                _logger.LogWarning(
                    "ExportReceipt {ExportId} confirm-pick with cold-storage notices: {Messages}",
                    exportId, string.Join(" | ", pickWarnings));
            }

            _logger.LogInformation(
                "ExportReceipt {ExportId} confirmed pick → ReadyToExport. {Count} boxes picking.",
                exportId, receipt.Details.Count);

            var forPrint = await _exportRepo.GetByIdWithDetailsForPrintAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");
            var printDto = BuildExportPrintData(forPrint, isPreview: false);
            forPrint.PrintDataSnapshotJson = JsonSerializer.Serialize(printDto, PrintJsonOptions);
            await _uow.SaveChangesAsync();

            var dto = MapToDto(forPrint);
            dto.Warnings = pickWarnings;
            return dto;
        }

        public async Task<ExportReceiptResponseDto> ApproveExportAsync(int exportId, string userId)
        {
            var receipt = await _exportRepo.GetByIdWithDetailsAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");

            if (receipt.Status != ExportStatus.ReadyToExport)
                throw new InvalidBusinessRuleException(
                    $"Chỉ duyệt phiếu xuất ở trạng thái ReadyToExport. Hiện tại: {receipt.Status}");

            await _uow.ExecuteInRetryableTransactionAsync(async () =>
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
                    // TakeAway POS: đã Paid mới duyệt xuất; kho xác nhận giao tại quầy mới chuyển Delivered.
                    ord.Status = OrderStatus.ApprovedExport;
                    ord.ShippingStatus = ShippingStatus.None;
                }
            });

            _logger.LogInformation(
                "ExportReceipt {ExportId} approved. {Count} boxes exported. Order {OrderId} status {OrderStatus}",
                exportId, receipt.Details.Count, receipt.OrderId, receipt.Order.Status);

            await _notificationService.NotifyExportApprovedAsync(receipt.Id);

            return MapToDto(receipt);
        }

        public async Task<ExportReceiptResponseDto> CancelExportAsync(int exportId, string userId, bool isManagerOrAdmin)
        {
            _ = userId;

            var receipt = await _exportRepo.GetByIdWithDetailsAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");

            if (receipt.Status == ExportStatus.Approved)
                throw new InvalidBusinessRuleException("Không thể hủy phiếu xuất đã được duyệt / đã xuất kho.");

            if (receipt.Status == ExportStatus.Cancelled)
                throw new InvalidBusinessRuleException("Phiếu xuất đã bị hủy trước đó");

            if (receipt.Status == ExportStatus.ReadyToExport && !isManagerOrAdmin)
                throw new ForbiddenException(
                    "Phiếu đã xác nhận sẵn sàng xuất (ReadyToExport). Chỉ Quản lý hoặc Admin mới được hủy.");

            var allocations = await _allocationRepo.GetByOrderIdAsync(receipt.OrderId);

            await _uow.ExecuteInRetryableTransactionAsync(async () =>
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
            });

            _logger.LogInformation(
                "ExportReceipt {ExportId} cancelled. Boxes reverted to Stored.",
                exportId);

            return MapToDto(receipt);
        }

        public async Task<ExportPrintDataDto> GetExportPrintDataAsync(int exportId)
        {
            var receipt = await _exportRepo.GetByIdWithDetailsForPrintAsync(exportId)
                ?? throw new NotFoundException($"Phiếu xuất #{exportId} không tồn tại");

            if (receipt.Status == ExportStatus.Cancelled)
                throw new InvalidBusinessRuleException("Phiếu đã hủy, không còn dữ liệu in hợp lệ.");

            if (receipt.Status == ExportStatus.PendingPick)
                return BuildExportPrintData(receipt, isPreview: true);

            if (!string.IsNullOrWhiteSpace(receipt.PrintDataSnapshotJson))
            {
                try
                {
                    var fromDb = JsonSerializer.Deserialize<ExportPrintDataDto>(
                        receipt.PrintDataSnapshotJson,
                        PrintJsonOptions);
                    if (fromDb != null)
                    {
                        fromDb.IsPreview = false;
                        return fromDb;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Export {ExportId}: snapshot JSON lỗi, build lại từ DB.", exportId);
                }
            }

            if (receipt.Status is ExportStatus.ReadyToExport or ExportStatus.Approved)
                return BuildExportPrintData(receipt, isPreview: false);

            throw new InvalidBusinessRuleException($"Không có dữ liệu in cho trạng thái phiếu: {receipt.Status}.");
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
                HasPrintSnapshot = !string.IsNullOrWhiteSpace(receipt.PrintDataSnapshotJson),
                Details = receipt.Details.Select(d => new ExportDetailDto
                {
                    Id = d.Id,
                    BoxId = d.BoxId,
                    BoxCode = d.Box?.BoxCode ?? "N/A",
                    WarehouseName = d.Box?.Slot?.Rack?.Zone?.Warehouse?.Name ?? "Chưa rõ kho",
                    BoxQrCode = d.Box?.QRCode ?? d.Box?.QrImageUrl,
                    ActualQuantity = d.ActualQuantity,
                    BoxStatus = d.Box?.Status.ToString() ?? "N/A"
                }).ToList()
            };
        }

        private static ExportPrintDataDto BuildExportPrintData(ExportReceipt receipt, bool isPreview)
        {
            var order = receipt.Order
                ?? throw new InvalidBusinessRuleException("Thiếu thông tin đơn hàng trên phiếu xuất.");
            var allocationByBoxId = order.Allocations
                .GroupBy(a => a.BoxId)
                .ToDictionary(g => g.Key, g => g.First());

            var lines = new List<ExportPrintLineDto>();
            var n = 0;
            foreach (var d in receipt.Details.OrderBy(x => x.Id))
            {
                n++;
                var box = d.Box;
                var lotCode = box?.Lot?.LotCode ?? "N/A";
                var productName = "N/A";
                var grade = "";

                var pv = box?.Lot?.GoodsReceiptDetail?.ProductVariant;
                if (pv != null)
                {
                    var pname = pv.Product?.Name?.Trim();
                    var vname = pv.Name?.Trim();
                    productName = string.IsNullOrEmpty(pname)
                        ? (vname ?? "N/A")
                        : (string.IsNullOrEmpty(vname) ? pname! : $"{pname} ({vname})");
                    grade = pv.Grade.ToString();
                }

                decimal requestedQuantity = 0;
                decimal unitPrice = 0;
                var variantId = box?.Lot?.GoodsReceiptDetail?.ProductVariantId;
                if (allocationByBoxId.TryGetValue(d.BoxId, out var alloc))
                {
                    requestedQuantity = alloc.ReservedQuantity;
                    unitPrice = alloc.OrderDetail?.UnitPrice ?? 0;
                }
                else
                {
                    // Backward-compatible fallback: map by variant when allocation is missing.
                    var orderDetail = variantId.HasValue
                        ? order.Details.FirstOrDefault(od => od.ProductVariantId == variantId.Value)
                        : null;
                    requestedQuantity = d.ActualQuantity;
                    unitPrice = orderDetail?.UnitPrice ?? 0;
                }
                var lineAmount = Math.Round(d.ActualQuantity * unitPrice, 2, MidpointRounding.AwayFromZero);

                lines.Add(new ExportPrintLineDto
                {
                    LineNo = n,
                    BoxId = d.BoxId,
                    BoxCode = box?.BoxCode ?? "N/A",
                    ProductVariantId = variantId,
                    MaSo = variantId.HasValue ? $"PV-{variantId.Value:D4}" : "N/A",
                    LotCode = lotCode,
                    ProductName = productName,
                    Grade = grade,
                    BoxWeightKg = box?.Weight ?? 0,
                    RequestedQuantity = requestedQuantity,
                    UnitPrice = unitPrice,
                    LineAmount = lineAmount,
                    ActualQuantity = d.ActualQuantity,
                    BoxType = box?.BoxType.ToString() ?? "Unknown",
                    IsPartial = box?.IsPartial ?? false
                });
            }

            return new ExportPrintDataDto
            {
                SchemaVersion = "1",
                SnapshotAtUtc = DateTime.UtcNow,
                IsPreview = isPreview,
                ExportId = receipt.Id,
                ExportCode = receipt.ExportCode,
                ExportStatus = receipt.Status.ToString(),
                OrderId = order.Id,
                OrderStatus = order.Status.ToString(),
                OrderSource = order.Source.ToString(),
                FulfillmentType = order.FulfillmentType.ToString(),
                TotalAmount = order.TotalAmount,
                RecipientFullName = !string.IsNullOrWhiteSpace(order.RecipientFullName)
                    ? order.RecipientFullName.Trim()
                    : (order.CustomerName?.Trim() ?? string.Empty),
                RecipientPhone = !string.IsNullOrWhiteSpace(order.RecipientPhone)
                    ? order.RecipientPhone.Trim()
                    : (order.CustomerPhone?.Trim() ?? string.Empty),
                RecipientAddress = order.RecipientAddress?.Trim() ?? string.Empty,
                CustomerUserId = order.CustomerUserId,
                Lines = lines
            };
        }

        /// <summary>
        /// Kho lạnh: không chặn lấy hàng; trả về cảnh báo nếu chưa đủ thời gian lưu lạnh hoặc thiếu mốc PlacedInColdAt.
        /// </summary>
        private static bool TryGetColdStoragePickNotice(Box box, out string? notice)
        {
            notice = null;
            var warehouse = box.Slot?.Rack?.Zone?.Warehouse;
            if (warehouse == null || warehouse.TitleWarehouse != TitleWarehouse.Cold)
                return false;

            var minHours = warehouse.MinColdStorageHours ?? DefaultColdStorageHours;
            if (minHours <= 0)
                return false;

            if (!box.PlacedInColdAt.HasValue)
            {
                notice =
                    $"Chú ý: Box {box.BoxCode} đang ở kho lạnh nhưng chưa có thời điểm vào kho lạnh — vẫn cho phép lấy hàng.";
                return true;
            }

            if (ColdStorageExportRule.CanExportFromCold(box.PlacedInColdAt, minHours))
                return false;

            var hoursElapsed = (DateTime.UtcNow - box.PlacedInColdAt.Value).TotalHours;
            notice =
                $"Chú ý: Box {box.BoxCode} chưa đủ thời gian lưu lạnh (yêu cầu tối thiểu {minHours} giờ, đã {hoursElapsed:F1} giờ) — đã cho phép lấy hàng.";
            return true;
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

        public async Task<IList<PendingApproveExportListItemDto>> GetApprovedExportsAsync(GetPendingApproveExportsQuery query)
        {
            query ??= new GetPendingApproveExportsQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            var list = await _exportRepo.GetApprovedExportsAsync(skip, take, query.Sort);

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

        public async Task<RevenueProfitSpecificReportResultDto> GetRevenueProfitSpecificReportAsync(RevenueProfitSpecificReportQueryDto query)
        {
            query ??= new RevenueProfitSpecificReportQueryDto();
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = Math.Clamp(query.PageSize, 1, 500);
            var normalizedFromDate = query.FromDate?.Date;
            var normalizedToDate = query.ToDate?.Date.AddDays(1).AddTicks(-1);

            var exports = await _exportRepo.GetApprovedExportsForRevenueReportAsync(
                normalizedFromDate,
                normalizedToDate,
                query.WarehouseId,
                query.ProductId,
                query.ProductVariantId);
            var (disposedKg, stockAdjustmentLossKg) = await _inventoryTranRepo.GetLossSummaryAsync(
                normalizedFromDate,
                normalizedToDate,
                query.WarehouseId,
                query.ProductId,
                query.ProductVariantId);
            var lossByLots = await _inventoryTranRepo.GetLossByLotSummaryAsync(
                normalizedFromDate,
                normalizedToDate,
                query.WarehouseId,
                query.ProductId,
                query.ProductVariantId);

            var rows = new List<RevenueProfitSpecificReportRowDto>();

            foreach (var receipt in exports)
            {
                var allocByBoxId = (receipt.Order?.Allocations ?? Enumerable.Empty<OrderAllocation>())
                    .GroupBy(a => a.BoxId)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var d in receipt.Details)
                {
                    var box = d.Box;
                    var lot = box?.Lot;
                    var pv = lot?.ProductVariant;
                    var wh = lot?.GoodsReceiptDetail?.GoodsReceipt?.Warehouse;
                    var supplier = lot?.GoodsReceiptDetail?.GoodsReceipt?.Supplier;
                    var customerDisplayName = BuildCustomerDisplayName(receipt.Order);

                    if (query.WarehouseId.HasValue && query.WarehouseId.Value > 0 && wh?.Id != query.WarehouseId.Value)
                        continue;
                    if (query.ProductId.HasValue && query.ProductId.Value > 0 && pv?.ProductId != query.ProductId.Value)
                        continue;
                    if (query.ProductVariantId.HasValue && query.ProductVariantId.Value > 0 && pv?.Id != query.ProductVariantId.Value)
                        continue;

                    var quantity = d.ActualQuantity;
                    var saleUnitPrice = 0m;
                    var costUnitPrice = lot?.CostUnitPrice ?? 0m;

                    if (allocByBoxId.TryGetValue(d.BoxId, out var alloc))
                    {
                        saleUnitPrice = alloc.OrderDetail?.UnitPrice ?? 0m;
                        if (alloc.CostUnitPriceSnapshot.HasValue && alloc.CostUnitPriceSnapshot.Value > 0)
                            costUnitPrice = alloc.CostUnitPriceSnapshot.Value;
                    }

                    var revenue = quantity * saleUnitPrice;
                    var cost = quantity * costUnitPrice;

                    rows.Add(new RevenueProfitSpecificReportRowDto
                    {
                        ExportedAt = receipt.CreatedAt,
                        ExportId = receipt.Id,
                        ExportCode = receipt.ExportCode,
                        OrderId = receipt.OrderId,
                        CustomerUserId = receipt.Order?.CustomerUserId,
                        CustomerName = customerDisplayName,
                        BoxId = d.BoxId,
                        BoxCode = box?.BoxCode ?? string.Empty,
                        LotId = lot?.Id ?? 0,
                        LotCode = lot?.LotCode ?? string.Empty,
                        SupplierId = supplier?.Id,
                        SupplierName = supplier?.Name?.Trim() ?? "Không xác định",
                        WarehouseId = wh?.Id,
                        WarehouseName = wh?.Name ?? string.Empty,
                        ProductId = pv?.ProductId,
                        ProductName = pv?.Product?.Name ?? string.Empty,
                        ProductVariantId = pv?.Id,
                        VariantName = pv?.Name ?? string.Empty,
                        QuantityKg = quantity,
                        SaleUnitPrice = saleUnitPrice,
                        CostUnitPrice = costUnitPrice,
                        Revenue = revenue,
                        Cost = cost,
                        Profit = revenue - cost
                    });
                }
            }

            var orderedRows = rows
                .OrderByDescending(r => r.ExportedAt)
                .ThenByDescending(r => r.ExportId)
                .ToList();

            var totalRows = orderedRows.Count;
            var totalRevenue = orderedRows.Sum(r => r.Revenue);
            var totalCost = orderedRows.Sum(r => r.Cost);
            var totalProfit = orderedRows.Sum(r => r.Profit);
            var totalPages = totalRows == 0 ? 1 : (int)Math.Ceiling(totalRows / (double)pageSize);
            if (page > totalPages) page = totalPages;

            var pageRows = orderedRows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var revenueByCustomers = orderedRows
                .GroupBy(r => BuildCustomerKey(r.CustomerUserId, r.CustomerName))
                .Select(g => new RevenueProfitByCustomerDto
                {
                    CustomerKey = g.Key,
                    CustomerName = g.First().CustomerName,
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost),
                    Profit = g.Sum(x => x.Profit)
                })
                .OrderByDescending(x => x.Revenue)
                .ThenBy(x => x.CustomerName)
                .ToList();
            var revenueBySuppliers = orderedRows
                .GroupBy(r => BuildSupplierKey(r.SupplierId, r.SupplierName))
                .Select(g => new RevenueProfitBySupplierDto
                {
                    SupplierKey = g.Key,
                    SupplierName = g.First().SupplierName,
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost),
                    Profit = g.Sum(x => x.Profit)
                })
                .OrderByDescending(x => x.Cost)
                .ThenBy(x => x.SupplierName)
                .ToList();

            return new RevenueProfitSpecificReportResultDto
            {
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                WarehouseId = query.WarehouseId,
                ProductId = query.ProductId,
                ProductVariantId = query.ProductVariantId,
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalProfit = totalProfit,
                TotalDisposedKg = disposedKg,
                TotalStockAdjustmentLossKg = stockAdjustmentLossKg,
                ProfitMarginPercent = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100m : 0m,
                TotalRows = totalRows,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                RevenueByCustomers = revenueByCustomers,
                RevenueBySuppliers = revenueBySuppliers,
                LossByLots = lossByLots.Select(x => new RevenueLossByLotDto
                {
                    LotId = x.lotId,
                    LotCode = x.lotCode,
                    DisposedKg = x.disposedKg,
                    StockAdjustmentLossKg = x.stockAdjustmentLossKg
                }).ToList(),
                Rows = pageRows
            };
        }

        public async Task<RevenueProfitSpecificReportResultDto> GetEstimatedRevenueProfitSpecificReportAsync(RevenueProfitSpecificReportQueryDto query)
        {
            query ??= new RevenueProfitSpecificReportQueryDto();
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = Math.Clamp(query.PageSize, 1, 500);
            var normalizedFromDate = query.FromDate?.Date;
            var normalizedToDate = query.ToDate?.Date.AddDays(1).AddTicks(-1);

            var allocations = await _allocationRepo.GetForRevenueEstimateReportAsync(
                normalizedFromDate,
                normalizedToDate,
                query.WarehouseId,
                query.ProductId,
                query.ProductVariantId);
            var (disposedKg, stockAdjustmentLossKg) = await _inventoryTranRepo.GetLossSummaryAsync(
                normalizedFromDate,
                normalizedToDate,
                query.WarehouseId,
                query.ProductId,
                query.ProductVariantId);
            var lossByLots = await _inventoryTranRepo.GetLossByLotSummaryAsync(
                normalizedFromDate,
                normalizedToDate,
                query.WarehouseId,
                query.ProductId,
                query.ProductVariantId);

            var rows = allocations.Select(a =>
            {
                var quantity = a.PickedQuantity ?? a.ReservedQuantity;
                var saleUnitPrice = a.OrderDetail?.UnitPrice ?? 0m;
                var costUnitPrice = a.CostUnitPriceSnapshot
                    ?? a.Box?.Lot?.CostUnitPrice
                    ?? 0m;
                var revenue = quantity * saleUnitPrice;
                var cost = quantity * costUnitPrice;
                var lot = a.Box?.Lot;
                var warehouse = lot?.GoodsReceiptDetail?.GoodsReceipt?.Warehouse;
                var supplier = lot?.GoodsReceiptDetail?.GoodsReceipt?.Supplier;
                var variant = a.OrderDetail?.ProductVariant;
                var customerDisplayName = BuildCustomerDisplayName(a.Order);
                return new RevenueProfitSpecificReportRowDto
                {
                    ExportedAt = a.ReservedAt,
                    ExportId = 0,
                    ExportCode = "DU_KIEN",
                    OrderId = a.OrderId,
                    CustomerUserId = a.Order?.CustomerUserId,
                    CustomerName = customerDisplayName,
                    BoxId = a.BoxId,
                    BoxCode = a.Box?.BoxCode ?? string.Empty,
                    LotId = lot?.Id ?? 0,
                    LotCode = lot?.LotCode ?? string.Empty,
                    SupplierId = supplier?.Id,
                    SupplierName = supplier?.Name?.Trim() ?? "Không xác định",
                    WarehouseId = warehouse?.Id,
                    WarehouseName = warehouse?.Name ?? string.Empty,
                    ProductId = variant?.ProductId,
                    ProductName = variant?.Product?.Name ?? string.Empty,
                    ProductVariantId = variant?.Id,
                    VariantName = variant?.Name ?? string.Empty,
                    QuantityKg = quantity,
                    SaleUnitPrice = saleUnitPrice,
                    CostUnitPrice = costUnitPrice,
                    Revenue = revenue,
                    Cost = cost,
                    Profit = revenue - cost
                };
            })
            .OrderByDescending(r => r.ExportedAt)
            .ToList();

            var totalRows = rows.Count;
            var totalRevenue = rows.Sum(r => r.Revenue);
            var totalCost = rows.Sum(r => r.Cost);
            var totalProfit = rows.Sum(r => r.Profit);
            var totalPages = totalRows == 0 ? 1 : (int)Math.Ceiling(totalRows / (double)pageSize);
            if (page > totalPages) page = totalPages;
            var pageRows = rows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var revenueByCustomers = rows
                .GroupBy(r => BuildCustomerKey(r.CustomerUserId, r.CustomerName))
                .Select(g => new RevenueProfitByCustomerDto
                {
                    CustomerKey = g.Key,
                    CustomerName = g.First().CustomerName,
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost),
                    Profit = g.Sum(x => x.Profit)
                })
                .OrderByDescending(x => x.Revenue)
                .ThenBy(x => x.CustomerName)
                .ToList();
            var revenueBySuppliers = rows
                .GroupBy(r => BuildSupplierKey(r.SupplierId, r.SupplierName))
                .Select(g => new RevenueProfitBySupplierDto
                {
                    SupplierKey = g.Key,
                    SupplierName = g.First().SupplierName,
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost),
                    Profit = g.Sum(x => x.Profit)
                })
                .OrderByDescending(x => x.Cost)
                .ThenBy(x => x.SupplierName)
                .ToList();

            return new RevenueProfitSpecificReportResultDto
            {
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                WarehouseId = query.WarehouseId,
                ProductId = query.ProductId,
                ProductVariantId = query.ProductVariantId,
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalProfit = totalProfit,
                TotalDisposedKg = disposedKg,
                TotalStockAdjustmentLossKg = stockAdjustmentLossKg,
                ProfitMarginPercent = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100m : 0m,
                TotalRows = totalRows,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                RevenueByCustomers = revenueByCustomers,
                RevenueBySuppliers = revenueBySuppliers,
                LossByLots = lossByLots.Select(x => new RevenueLossByLotDto
                {
                    LotId = x.lotId,
                    LotCode = x.lotCode,
                    DisposedKg = x.disposedKg,
                    StockAdjustmentLossKg = x.stockAdjustmentLossKg
                }).ToList(),
                Rows = pageRows
            };
        }

        private static string BuildCustomerDisplayName(Order? order)
        {
            if (order == null)
                return "Khách lẻ / không xác định";

            if (!string.IsNullOrWhiteSpace(order.RecipientFullName))
                return order.RecipientFullName.Trim();
            if (!string.IsNullOrWhiteSpace(order.CustomerName))
                return order.CustomerName.Trim();
            if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
                return order.CustomerPhone.Trim();

            return "Khách lẻ / không xác định";
        }

        private static string BuildCustomerKey(string? customerUserId, string customerName)
        {
            if (!string.IsNullOrWhiteSpace(customerUserId))
                return $"user:{customerUserId.Trim()}";
            if (!string.IsNullOrWhiteSpace(customerName))
                return $"name:{customerName.Trim().ToLowerInvariant()}";

            return "guest:unknown";
        }

        private static string BuildSupplierKey(int? supplierId, string supplierName)
        {
            if (supplierId.HasValue && supplierId.Value > 0)
                return $"supplier:{supplierId.Value}";
            if (!string.IsNullOrWhiteSpace(supplierName))
                return $"name:{supplierName.Trim().ToLowerInvariant()}";

            return "supplier:unknown";
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
                HasPrintSnapshot = !string.IsNullOrWhiteSpace(s.PrintDataSnapshotJson),
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
