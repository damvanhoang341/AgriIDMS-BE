using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Supplier
{
    public class CreateSupplierRequest
    {
        public string Name { get; set; } = null!;

        public string? Address { get; set; }

        public string? Phone { get; set; }
    }
    public class UpdateSupplierRequest
    {
        public string Name { get; set; } = null!;

        public string? Address { get; set; }

        public string? Phone { get; set; }
    }

    public class UpdateStatusSupplierRequest
    {
        [Required(ErrorMessage = "Chuyển trạng thái không được để trống")]
        public SupplierStatus status { get; set; }

    }
    public class SupplierResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }
}
