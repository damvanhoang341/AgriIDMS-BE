using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.PurchaseRequest
{
    public class CreatePurchaseRequestRequest
    {
        public string? Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreatePurchaseRequestDetailRequest> Details { get; set; } = new();
    }

    public class CreatePurchaseRequestDetailRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal RequestedWeight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TargetUnitPrice { get; set; }
    }

    public class CreatePurchaseOrderFromRequestRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreatePurchaseOrderFromRequestDetailRequest> Details { get; set; } = new();
    }

    public class CreatePurchaseOrderFromRequestDetailRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int PurchaseRequestDetailId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal OrderedWeight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, 100)]
        public decimal TolerancePercent { get; set; } = 2;

        [Required]
        public DateTime HarvestDate { get; set; }
    }

    public class PurchaseRequestDetailResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal RequestedWeight { get; set; }
        public decimal AllocatedWeight { get; set; }
        public decimal RemainingWeight { get; set; }
        public decimal TargetUnitPrice { get; set; }
    }

    public class PurchaseRequestResponse
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseRequestDetailResponse> Details { get; set; } = new();
    }
}
