using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Box
{
    public class AssignBoxToSlotRequest
    {
        [Required(ErrorMessage = "BoxId không được để trống")]
        public int BoxId { get; set; }

        [Required(ErrorMessage = "SlotId không được để trống")]
        public int SlotId { get; set; }
    }
}
