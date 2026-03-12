using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);

        Task<bool> ExistsAsync(
            NotificationType type,
            string message,
            string? referenceType,
            int? referenceId);
    }
}

