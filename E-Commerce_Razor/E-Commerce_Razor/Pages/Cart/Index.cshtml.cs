using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BLL.IService;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Cart
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ICartService cartService, ILogger<IndexModel> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        public DAL.Entities.Cart? Cart { get; set; }

        public decimal TotalAmount =>
            Cart?.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice) ?? 0;

        public IActionResult OnGet()
        {
            var userId = GetCurrentUserId();
            Cart = _cartService.GetCart(userId);
            return Page();
        }

        // POST /Cart/Add  (AJAX từ trang chi tiết / danh sách sản phẩm)
        public IActionResult OnPostAdd(int productId, int quantity = 1)
        {
            var isAjax = IsAjaxRequest();

            try
            {
                var userId = GetCurrentUserId();
                if (quantity <= 0) quantity = 1;

                _cartService.AddItem(userId, productId, quantity);

                var cart = _cartService.GetCart(userId);
                var count = cart.CartItems.Sum(ci => ci.Quantity);

                if (isAjax)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        count
                    });
                }

                return RedirectToPage("/Cart/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");

                if (isAjax)
                {
                    Response.StatusCode = 500;
                    return new JsonResult(new
                    {
                        success = false,
                        error = ex.Message
                    });
                }

                TempData["Error"] = ex.Message;
                return RedirectToPage("/Shop/Index");
            }
        }

        // GET /Cart/Count  (được gọi trong _LoginPartial để cập nhật badge)
        // Lưu ý: Handler này chạy cùng [Authorize] trên class; nếu user chưa đăng nhập thì script phía client sẽ không cập nhật badge.
        public IActionResult OnGetCount()
        {
            try
            {
                if (!(User.Identity?.IsAuthenticated ?? false))
                {
                    return new JsonResult(0);
                }

                var userId = GetCurrentUserId();
                var cart = _cartService.GetCart(userId);
                var count = cart.CartItems.Sum(ci => ci.Quantity);
                return new JsonResult(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return new JsonResult(0);
            }
        }

        // Cập nhật số lượng trong giỏ từ trang /Cart/Index
        public IActionResult OnPostUpdate(int cartItemId, int quantity)
        {
            var userId = GetCurrentUserId();

            if (quantity <= 0)
            {
                _cartService.RemoveItem(cartItemId);
            }
            else
            {
                _cartService.UpdateItem(cartItemId, quantity);
            }

            return RedirectToPage();
        }

        // Xóa item khỏi giỏ từ trang /Cart/Index
        public IActionResult OnPostRemove(int cartItemId)
        {
            var userId = GetCurrentUserId();
            _cartService.RemoveItem(cartItemId);
            return RedirectToPage();
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);
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

