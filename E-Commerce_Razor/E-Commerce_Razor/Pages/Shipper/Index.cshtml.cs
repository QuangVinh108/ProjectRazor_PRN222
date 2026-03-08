using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Shipper
{
    [Authorize(Roles = "Shipper")]
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;

        public IndexModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public List<OrderDto> Orders { get; set; } = new();
        public string ShipperName { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var shipperId = GetCurrentUserId();
            ShipperName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Shipper";
            Orders = await _orderService.GetShipperOrdersAsync(shipperId);
        }

        public async Task<IActionResult> OnPostDeliveredAsync(int orderId)
        {
            var shipperId = GetCurrentUserId();
            var success = await _orderService.MarkDeliveredAsync(orderId, shipperId);
            if (success)
                TempData["Success"] = $"Đơn #{orderId} đã được xác nhận giao thành công!";
            else
                TempData["Error"] = "Không thể cập nhật trạng thái. Kiểm tra lại đơn hàng.";
            return RedirectToPage();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(claim ?? throw new Exception("Vui lòng đăng nhập"));
        }
    }
}
