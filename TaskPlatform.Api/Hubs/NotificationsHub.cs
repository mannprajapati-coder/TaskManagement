using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskPlatform.Api.Hubs
{
    [Authorize]
    public class NotificationsHub : Hub
    {
    }
}
