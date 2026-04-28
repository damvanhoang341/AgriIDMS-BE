using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Export
{
    public class CreateExportReceiptRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId phải lớn hơn 0")]
        public int OrderId { get; set; }
    }

    public class ExportReceiptResponseDto
    {
        public int Id { get; set; }
        public string ExportCode { get; set; } = null!;
        public int OrderId { get; set; }
        public string Status { get; set; } = null!;
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        /// <summary>Đã chốt snapshot in khi chuyển ReadyToExport.</summary>
        public bool HasPrintSnapshot { get; set; }
        public List<ExportDetailDto> Details { get; set; } = new();
    }

    /// <summary>Dữ liệu cho FE render HTML phiếu xuất (schema có thể nâng cấp).</summary>
    public class ExportPrintDataDto
    {
        public string SchemaVersion { get; set; } = "1";
        public DateTime SnapshotAtUtc { get; set; }
        /// <summary>True khi đơn vẫn PendingPick — bản xem trước, chưa chốt.</summary>
        public bool IsPreview { get; set; }

        public int ExportId { get; set; }
        public string ExportCode { get; set; } = null!;
        public string ExportStatus { get; set; } = null!;

        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = null!;
        public string OrderSource { get; set; } = null!;
        public string FulfillmentType { get; set; } = null!;
        public decimal TotalAmount { get; set; }

        public string RecipientFullName { get; set; } = null!;
        public string RecipientPhone { get; set; } = null!;
        public string RecipientAddress { get; set; } = null!;
        public string? CustomerUserId { get; set; }

        public List<ExportPrintLineDto> Lines { get; set; } = new();
    }

    public class ExportPrintLineDto
    {
        public int LineNo { get; set; }
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = null!;
        public int? ProductVariantId { get; set; }
        public string MaSo { get; set; } = string.Empty;
        public string LotCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public decimal BoxWeightKg { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount { get; set; }
        public decimal ActualQuantity { get; set; }
        public string BoxType { get; set; } = null!;
        public bool IsPartial { get; set; }
    }

    public class ExportDetailDto
    {
        public int Id { get; set; }
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = null!;
        public decimal ActualQuantity { get; set; }
        public string BoxStatus { get; set; } = null!;
    }

    public class GetPendingApproveExportsQuery
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
        /// <summary>createdAtDesc (mặc định), createdAtAsc.</summary>
        public string? Sort { get; set; }
    }

    public class PendingApproveExportListItemDto
    {
        public int ExportId { get; set; }
        public string ExportCode { get; set; } = null!;
        public int OrderId { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int BoxCount { get; set; }
    }

    public class RevenueProfitSpecificReportQueryDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? WarehouseId { get; set; }
        public int? ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class RevenueProfitSpecificReportRowDto
    {
        public DateTime ExportedAt { get; set; }
        public int ExportId { get; set; }
        public string ExportCode { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string? CustomerUserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = string.Empty;
        public int LotId { get; set; }
        public string LotCode { get; set; } = string.Empty;
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int? WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? ProductVariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public decimal QuantityKg { get; set; }
        public decimal SaleUnitPrice { get; set; }
        public decimal CostUnitPrice { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }

    public class RevenueProfitSpecificReportResultDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? WarehouseId { get; set; }
        public int? ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public int TotalRows { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<RevenueProfitByCustomerDto> RevenueByCustomers { get; set; } = new();
        public List<RevenueProfitBySupplierDto> RevenueBySuppliers { get; set; } = new();
        public List<RevenueProfitSpecificReportRowDto> Rows { get; set; } = new();
    }

    public class RevenueProfitByCustomerDto
    {
        public string CustomerKey { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }

    public class RevenueProfitBySupplierDto
    {
        public string SupplierKey { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }
}
