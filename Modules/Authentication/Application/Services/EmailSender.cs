using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Modules.Authentication.Application.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
    }

    public class DevEmailSender : IEmailSender
    {
        private readonly ILogger<DevEmailSender> _logger;

        public DevEmailSender(ILogger<DevEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            _logger.LogInformation("--- DEV EMAIL DISPATCH ---");
            _logger.LogInformation("To: {ToEmail}", toEmail);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Body: {Body}", bodyHtml);
            _logger.LogInformation("---------------------------");
            return Task.CompletedTask;
        }
    }
}
