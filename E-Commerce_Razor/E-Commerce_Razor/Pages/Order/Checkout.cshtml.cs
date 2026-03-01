using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Order
{
    [Authorize]
    public class CheckoutModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(IOrderService orderService, ILogger<CheckoutModel> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [BindProperty]
        public CreateOrderDto Input { get; set; } = new CreateOrderDto
        {
            PaymentMethod = "COD",
            Country = "Vietnam"
        };

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                Input.UserId = GetCurrentUserId();
                var order = await _orderService.CreateOrderAsync(Input);
                return RedirectToPage("./Details", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout error");
                TempData["Error"] = ex.Message;
                return Page();
            }
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
