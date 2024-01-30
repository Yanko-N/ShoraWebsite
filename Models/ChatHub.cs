using Microsoft.AspNetCore.SignalR;

namespace ShoraWebsite.Models
{
    public class ChatHub : Hub
    {
        private static List<Message> messageHistory = new List<Message>();

        public async Task SendMessage(string user, string message)
        {
          
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        public async Task GetChatHistory()
        {
        }
    }
}
