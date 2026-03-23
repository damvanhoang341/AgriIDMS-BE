using AgriIDMS.Application.DTOs.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequest request, string userId);

        Task<PaymentResponseDto> GetLatestPaymentAsync(int orderId, string userId);

        Task<IList<PendingCodPaymentItemDto>> GetPendingCodPaymentsAsync(GetPendingCodPaymentsQuery query);

        Task<PaymentResponseDto> ConfirmCODPaidAsync(int paymentId);

        Task HandlePayOSWebhookAsync(string webhookBody);

        Task<PaymentResponseDto> CancelBankingPaymentAsync(int paymentId, string userId);
    }
}
