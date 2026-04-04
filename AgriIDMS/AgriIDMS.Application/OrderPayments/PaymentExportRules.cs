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
            var payments = order.Payments;
            if (payments == null || payments.Count == 0)
                return false;

            if (order.PaymentTiming == PaymentTiming.PayBefore)
                return payments.Any(p => p.PaymentStatus == PaymentStatus.Paid);

            return payments.Any(p => p.PaymentStatus == PaymentStatus.Paid)
                   || payments.Any(p =>
                       p.PaymentMethod == PaymentMethod.Cash && p.PaymentStatus == PaymentStatus.Pending);
        }
    }
}
