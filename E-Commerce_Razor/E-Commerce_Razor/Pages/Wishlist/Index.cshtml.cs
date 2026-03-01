using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;

namespace E_Commerce_Razor.Pages.Wishlist
{
    public class IndexModel : PageModel
    {
        private readonly IWishlistService _wishlistService;

        public List<WishlistProductDTO> WishlistProducts { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public IndexModel(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        public async Task OnGetAsync()
        {
            var result = await _wishlistService.GetUserWishlistAsync();

            if (result.IsSuccess && result.Data != null)
            {
                WishlistProducts = result.Data.ToList();
            }
            else
            {
                ErrorMessage = result.Errors.FirstOrDefault() ?? result.Message;
            }
        }

        public async Task<IActionResult> OnPostRemoveAsync(int wishlistProductId)
        {
            var result = await _wishlistService.RemoveFromWishlistAsync(wishlistProductId);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Errors.FirstOrDefault();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearAsync()
        {
            var result = await _wishlistService.ClearWishlistAsync();

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Errors.FirstOrDefault();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetCheckAsync(int productId)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return new JsonResult(new
                {
                    isInWishlist = false
                });
            }

            var result = await _wishlistService.IsProductInWishlistAsync(productId);

            return new JsonResult(new
            {
                isInWishlist = result
            });
        }

        public async Task<IActionResult> OnPostToggleAsync(int productId)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return new JsonResult(new
                {
                    success = false,
                    requireLogin = true
                });
            }

            var result = await _wishlistService.ToggleWishlistAsync(productId);

            return new JsonResult(new
            {
                success = result.IsSuccess,
                isAdded = result.Data
            });
        }
    }
}