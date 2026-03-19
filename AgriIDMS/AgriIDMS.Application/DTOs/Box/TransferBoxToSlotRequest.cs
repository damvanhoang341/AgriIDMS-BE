using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Box
{
    /// <summary>Chuyển 1 box đã xếp từ slot hiện tại sang slot khác (cùng kho).</summary>
    public class TransferBoxToSlotRequest
    {
        [Required(ErrorMessage = "BoxId không được để trống")]
        public int BoxId { get; set; }

        [Required(ErrorMessage = "ToSlotId không được để trống")]
        public int ToSlotId { get; set; }
    }
}

