using BLL.DTOs;
using BLL.IService;
using BLL.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Shop
{
    public class DetailsModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly IWishlistService _wishlistService;

        public DetailsModel(IProductService productService, IWishlistService wishlistService)
        {
            _productService = productService;
            _wishlistService = wishlistService;
        }

        public ProductViewModel ProductDetail { get; set; }

        public IActionResult OnGet(int id)
        {
            ProductDetail = _productService.GetDetail(id);

            if (ProductDetail == null || ProductDetail.Status != 1)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnGetCheckWishlistAsync(int productId)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return new JsonResult(false);
            }

            var result = await _wishlistService.IsProductInWishlistAsync(productId);

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostToggleWishlistAsync(int productId)
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
                isAdded = result.Data?.IsAdded ?? false,
                count = result.Data?.Count ?? 0
            });
        }
    }
}