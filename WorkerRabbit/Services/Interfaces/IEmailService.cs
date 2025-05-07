using WorkerRabbit.Models;

namespace WorkerRabbit.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(NotificationEvent notification);
    }
}

