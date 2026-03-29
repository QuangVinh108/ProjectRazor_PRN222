using BLL.DTOs;
using BLL.IService;
using DalUser = DAL.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Admin.Orders
{
    [Authorize(Roles = "Admin")]
    public class AssignShipperModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;

        public AssignShipperModel(IOrderService orderService, IUserService userService)
        {
            _orderService = orderService;
            _userService = userService;
        }

        [BindProperty] public int OrderId { get; set; }
        [BindProperty] public int ShipperId { get; set; }
        [BindProperty] public string? TrackingNumber { get; set; }
        [BindProperty] public string? Carrier { get; set; }

        public OrderDto? Order { get; set; }
        public List<DalUser> Shippers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            OrderId = id;
            Order = await _orderService.GetOrderByIdForAdminAsync(id);
            if (Order == null || Order.Status != "Paid")
            {
                TempData["Error"] = "Đơn hàng không tồn tại hoặc không ở trạng thái Paid.";
                return RedirectToPage("/Admin/Orders/Index");
            }
            Shippers = await _userService.GetShippersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var success = await _orderService.AssignShipperAsync(OrderId, ShipperId, TrackingNumber, Carrier);
            if (success)
                TempData["Success"] = $"Đã gán Shipper cho đơn #{OrderId} thành công!";
            else
                TempData["Error"] = "Gán Shipper thất bại. Đơn hàng phải ở trạng thái Paid.";

            return RedirectToPage("/Admin/Orders/Index");
        }
    }
}
