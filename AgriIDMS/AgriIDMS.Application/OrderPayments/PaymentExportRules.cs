using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using System.Linq;

namespace AgriIDMS.Application.OrderPayments
{
    /// <summary>Điều kiện xuất kho / thanh toán theo <see cref="PaymentTiming"/>.</summary>
    public static class PaymentExportRules
    {
        public static bool OrderHasExportEligiblePayments(Order order)
        {
            if (!order.PaymentTiming.HasValue)
                return false;

            var payments = order.Payments;

            // TakeAway: luôn phải Paid mới xuất (kể cả bản ghi cũ từng PayAfter).
            if (order.FulfillmentType == FulfillmentType.TakeAway)
            {
                if (payments == null || payments.Count == 0)
                    return false;
                return payments.Any(p => p.PaymentStatus == PaymentStatus.Paid);
            }

            if (order.PaymentTiming == PaymentTiming.PayBefore)
            {
                if (payments == null || payments.Count == 0)
                    return false;
                return payments.Any(p => p.PaymentStatus == PaymentStatus.Paid);
            }

            // PayAfter: được xuất khi chưa có thanh toán; hoặc đã Paid; hoặc tiền mặt Pending.
            if (payments == null || payments.Count == 0)
                return true;

            return payments.Any(p => p.PaymentStatus == PaymentStatus.Paid)
                   || payments.Any(p =>
                       p.PaymentMethod == PaymentMethod.Cash && p.PaymentStatus == PaymentStatus.Pending);
        }
    }
}
