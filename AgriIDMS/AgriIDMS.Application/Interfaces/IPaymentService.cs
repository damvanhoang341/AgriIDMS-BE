using AgriIDMS.Application.DTOs.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequest request, string userId);

        /// <summary>
        /// Sale/kho tạo thanh toán cho đơn online PayBefore sau khi thống nhất với khách (bỏ qua chặn quá hạn 24h của khách tự thanh toán).
        /// </summary>
        Task<PaymentResponseDto> CreateStaffOnlinePayBeforePaymentAsync(CreatePaymentRequest request, string staffUserId);

        Task<PaymentResponseDto> GetLatestPaymentAsync(int orderId, string userId);

        Task<IList<PendingCashPaymentItemDto>> GetPendingCashPaymentsAsync(GetPendingCashPaymentsQuery query);

        /// <summary>Xác nhận đã thu tiền mặt (Cash) — Pending → Paid.</summary>
        Task<PaymentResponseDto> ConfirmCashPaymentPaidAsync(int paymentId);

        Task HandlePayOSWebhookAsync(string webhookBody);

        Task<PaymentResponseDto> CancelBankingPaymentAsync(int paymentId, string userId);
    }
}
