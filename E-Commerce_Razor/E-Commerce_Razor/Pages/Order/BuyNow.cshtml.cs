using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Order
{
    [Authorize]
    public class BuyNowModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<BuyNowModel> _logger;

        public BuyNowModel(IOrderService orderService, ILogger<BuyNowModel> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        public void OnGet()
        {
            // Không có giao diện, chỉ dùng POST
        }

        public async Task<IActionResult> OnPostAsync(int productId, int quantity = 1)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            try
            {
                if (!User.Identity!.IsAuthenticated)
                {
                    if (isAjax)
                    {
                        Response.StatusCode = 401;
                        return new JsonResult(new
                        {
                            success = false,
                            requireLogin = true,
                            message = "Vui lòng đăng nhập để tiếp tục"
                        });
                    }
                    return RedirectToPage("/Account/Login", new { returnUrl = Request.Path.ToString() });
                }

                var userId = GetCurrentUserId();

                _logger.LogInformation($"BuyNow: User {userId}, Product {productId}, Quantity {quantity}");

                var dto = new CreateOrderDto
                {
                    UserId = userId,
                    PaymentMethod = "COD",
                    Country = "Vietnam"
                };

                var order = await _orderService.CreateOrderBuyNowAsync(userId, productId, quantity, dto);

                _logger.LogInformation($"BuyNow: Order created - OrderId {order.OrderId}");

                if (isAjax)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        redirectUrl = Url.Page("./Details", new { id = order.OrderId }),
                        message = "Đặt hàng thành công"
                    });
                }

                return RedirectToPage("./Details", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BuyNow error: {ex.Message}");

                if (isAjax)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

                TempData["Error"] = ex.Message;
                return RedirectToPage("/Shop/Index");
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
