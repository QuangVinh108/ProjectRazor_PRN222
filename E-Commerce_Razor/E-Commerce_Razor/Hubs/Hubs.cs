using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace E_Commerce_Razor.Hubs
{
    [Authorize] // Bắt buộc đăng nhập mới được dùng Chat (để hệ thống biết ai đang gửi)
    public class AppHub : Hub
    {
        // 1. Khi một người mở trang web và kết nối vào Chat
        public override async Task OnConnectedAsync()
        {
            // Kiểm tra xem người này có phải là Support hoặc Admin không
            if (Context.User.IsInRole("Admin") || Context.User.IsInRole("Support"))
            {
                // Thêm họ vào một phòng kín tên là "SupportTeam"
                await Groups.AddToGroupAsync(Context.ConnectionId, "SupportTeam");
            }
            await base.OnConnectedAsync();
        }

        // 2. Hàm dành cho KHÁCH HÀNG gửi tin cho SUPPORT
        public async Task SendMessageToSupport(string message)
        {
            // Lấy ID và Tên của khách hàng đang chat
            var customerId = Context.UserIdentifier;
            var customerName = Context.User.Identity.Name;

            // Gửi tin nhắn này cho TẤT CẢ nhân viên trong phòng "SupportTeam"
            await Clients.Group("SupportTeam").SendAsync("ReceiveMessageFromCustomer", customerId, customerName, message);

            // Gửi ngược lại cho chính khách hàng để hiển thị lên màn hình của họ
            await Clients.Caller.SendAsync("ReceiveMessage", "Bạn", message);
        }

        // 3. Hàm dành cho SUPPORT trả lời KHÁCH HÀNG
        public async Task ReplyToCustomer(string customerId, string message)
        {
            var supportName = Context.User.Identity.Name;

            // Gửi đích danh cho ID của khách hàng đó (chỉ họ mới thấy)
            await Clients.User(customerId).SendAsync("ReceiveMessage", "CSKH " + supportName, message);

            // Gửi ngược lại cho nhân viên Support để hiển thị lên màn hình của họ
            await Clients.Caller.SendAsync("ReceiveReplyEcho", customerId, "Bạn", message);
        }
    }
}