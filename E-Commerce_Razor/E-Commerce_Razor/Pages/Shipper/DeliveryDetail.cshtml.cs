using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Shipper
{
    [Authorize(Roles = "Shipper")]
    public class DeliveryDetailModel : PageModel
    {
        private readonly IOrderService _orderService;

        public DeliveryDetailModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public OrderDto? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var shipperId = GetCurrentUserId();
            Order = await _orderService.GetOrderByIdForAdminAsync(id);

            // Kiểm tra đúng shipper mới xem được
            if (Order == null || Order.Shipping?.ShipperId != shipperId)
                return RedirectToPage("/Shipper/Index");

            return Page();
        }

        public async Task<IActionResult> OnPostDeliveredAsync(int orderId)
        {
            var shipperId = GetCurrentUserId();
            var success = await _orderService.MarkDeliveredAsync(orderId, shipperId);
            if (success)
                TempData["Success"] = "Xác nhận giao hàng thành công!";
            else
                TempData["Error"] = "Không thể cập nhật. Đơn hàng phải ở trạng thái Shipped.";

            return RedirectToPage(new { id = orderId });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(claim ?? throw new Exception("Vui lòng đăng nhập"));
        }
    }
}
