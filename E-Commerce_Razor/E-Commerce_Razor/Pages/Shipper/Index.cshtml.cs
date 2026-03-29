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
        private readonly IUserService _userService; // Thêm userService

        public IndexModel(IOrderService orderService, IUserService userService) // Thêm IUserService vào DI
        {
            _orderService = orderService;
            _userService = userService;
        }

        public List<OrderDto> Orders { get; set; } = new();
        public string ShipperName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var shipperId = GetCurrentUserId();
            
            // KIỂM TRA EKYC CHO SHIPPER
            var shipper = _userService.GetUserById(shipperId);
            if (shipper == null || !shipper.IsIdentityVerified)
            {
                // Nếu chưa xác thực, thông báo và điều hướng đến trang EKYC
                TempData["ErrorMessage"] = "Bạn cần xác thực danh tính (eKYC) trước khi xem và nhận đơn giao hàng.";
                return RedirectToPage("/Account/Ekyc");
            }

            ShipperName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Shipper";
            Orders = await _orderService.GetShipperOrdersAsync(shipperId);

            return Page();
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
