using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
    
            
namespace CollegeIssueSystem.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            if (Context.UserIdentifier != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{Context.UserIdentifier}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.UserIdentifier != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{Context.UserIdentifier}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(string userId, string title, string message)
        {
            await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", title, message, DateTime.Now);
        }

        public async Task NotifyAdmin(string title, string message)
        {
            await Clients.Group("admins").SendAsync("AdminNotification", title, message, DateTime.Now);
        }
    }
}
