using PaperService.DTOs;

namespace PaperService.Clients
{
    public interface IUserServiceClient
    {
        Task TriggerNotificationAsync(NotificationTriggerDto dto);
    }
}
