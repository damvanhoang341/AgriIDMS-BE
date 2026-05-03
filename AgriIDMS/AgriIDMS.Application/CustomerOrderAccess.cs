using System;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Application;

/// <summary>
/// Đơn online: <see cref="Order.UserId"/> là khách.
/// Đơn POS: <see cref="Order.UserId"/> là nhân viên, khách liên kết qua <see cref="Order.CustomerUserId"/>.
/// </summary>
public static class CustomerOrderAccess
{
    public static bool IsBuyer(Order order, string userId)
    {
        if (order == null || string.IsNullOrWhiteSpace(userId)) return false;
        if (string.Equals(order.UserId, userId, StringComparison.Ordinal)) return true;
        return order.Source == OrderSource.POS
            && !string.IsNullOrWhiteSpace(order.CustomerUserId)
            && string.Equals(order.CustomerUserId, userId, StringComparison.Ordinal);
    }
}
