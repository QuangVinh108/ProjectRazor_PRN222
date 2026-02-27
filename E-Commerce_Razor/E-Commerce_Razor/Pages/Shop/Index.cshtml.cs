using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace E_Commerce_Razor.Pages.Shop
{
    public class IndexModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public IndexModel(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        // Dữ liệu hiển thị
        public IEnumerable<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public IEnumerable<dynamic> Categories { get; set; } = new List<dynamic>();

        // Các thuộc tính dùng để Lọc & Sắp xếp (Tự động giữ giá trị trên URL)
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; }

        public void OnGet()
        {
            // Set giá trị mặc định cho SortOrder nếu chưa có
            if (string.IsNullOrEmpty(SortOrder))
            {
                SortOrder = "price_desc";
            }

            // Gọi Service để lấy dữ liệu
            Products = _productService.GetFilteredProducts(SearchTerm, CategoryId, MinPrice, MaxPrice, SortOrder);
            Categories = _categoryService.GetAll();
        }
    }
}