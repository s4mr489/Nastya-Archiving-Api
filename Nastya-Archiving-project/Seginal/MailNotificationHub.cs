using Microsoft.AspNetCore.SignalR;

namespace Nastya_Archiving_project.Seginal
{
    public class MailNotificationHub : Hub
    {
        // When a user connects to the hub
        public override async Task OnConnectedAsync()
        {
            // Get the username from the authenticated user
            var username = Context.User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
            {
                // Add the user to a group with their username
                await Groups.AddToGroupAsync(Context.ConnectionId, username);
            }

            await base.OnConnectedAsync();
        }

        // When a user disconnects from the hub
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var username = Context.User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
            {
                // Remove from the group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
