using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Order
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IOrderService orderService, ILogger<DetailsModel> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        public OrderDto? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = GetCurrentUserId();
            Order = await _orderService.GetOrderByIdAsync(id, userId);

            if (Order == null)
                return RedirectToPage("./Index");

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.CancelOrderAsync(id, userId);

                if (result)
                    TempData["Success"] = "Hủy đơn hàng thành công";
                else
                    TempData["Error"] = "Không thể hủy đơn hàng";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel order error");
                TempData["Error"] = ex.Message;
            }

            return RedirectToPage("./Details", new { id });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new Exception("Vui lòng đăng nhập");
            return int.Parse(userIdClaim);
        }
    }
}
