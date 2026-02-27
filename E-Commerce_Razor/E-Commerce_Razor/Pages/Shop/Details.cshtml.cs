using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Shop
{
    public class DetailsModel : PageModel
    {
        private readonly IProductService _productService;

        public DetailsModel(IProductService productService)
        {
            _productService = productService;
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
    }
}