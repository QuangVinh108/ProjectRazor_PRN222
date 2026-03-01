using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace E_Commerce_Razor.Hubs
{
    // Kế thừa từ class Hub của thư viện SignalR
    public class AppHub : Hub
    {
        // Hàm này dùng cho tính năng Chat (Ai gọi hàm này, nó sẽ phát loa cho tất cả mọi người)
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}