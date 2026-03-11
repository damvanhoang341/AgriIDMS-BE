using AgriIDMS.Application.DTOs.Payment;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequest request, string userId);

        Task<PaymentResponseDto> GetLatestPaymentAsync(int orderId, string userId);

        Task<PaymentResponseDto> ConfirmCODPaidAsync(int paymentId);
    }
}
