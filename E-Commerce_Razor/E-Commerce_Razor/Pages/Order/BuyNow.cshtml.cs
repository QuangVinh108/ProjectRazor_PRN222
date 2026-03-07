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
        private readonly ICartService _cartService;
        private readonly ILogger<BuyNowModel> _logger;

        public BuyNowModel(ICartService cartService, ILogger<BuyNowModel> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        public void OnGet() { }

        /// <summary>
        /// BuyNow: thêm sản phẩm vào giỏ rồi chuyển sang Checkout để người dùng
        /// nhập địa chỉ giao hàng và chọn phương thức thanh toán (COD / VNPAY).
        /// </summary>
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
                if (quantity <= 0) quantity = 1;

                // Thêm/thay thế 1 sản phẩm vào giỏ → sau đó vào Checkout nhập địa chỉ
                await _cartService.AddOrReplaceSingleItemAsync(userId, productId, quantity);

                _logger.LogInformation("BuyNow: User {UserId}, Product {ProductId}, Qty {Qty} → Checkout", userId, productId, quantity);

                var checkoutUrl = Url.Page("/Order/Checkout");

                if (isAjax)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        redirectUrl = checkoutUrl,
                        message = "Chuyển sang trang thanh toán"
                    });
                }

                return Redirect(checkoutUrl!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BuyNow error: {Message}", ex.Message);

                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = ex.Message });
                }

                TempData["Error"] = ex.Message;
                return RedirectToPage("/Shop/Index");
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) throw new Exception("Vui lòng đăng nhập");
            return int.Parse(userIdClaim);
        }
    }
}
