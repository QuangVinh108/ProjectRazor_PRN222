using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR; 
using E_Commerce_Razor.Hubs;      

namespace E_Commerce_Razor.Pages.Product
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IHubContext<AppHub> _hubContext;

        public IndexModel(IProductService productService, ICategoryService categoryService, IHubContext<AppHub> hubContext)
        {
            _productService = productService;
            _categoryService = categoryService;
            _hubContext = hubContext;
        }

        public IEnumerable<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public IEnumerable<CategoryDTO> AllCategories { get; set; } = new List<CategoryDTO>();

        [BindProperty(SupportsGet = true)]
        public int? CurrentParentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CurrentCategoryId { get; set; }

        public void OnGet()
        {
            // 1. Lấy danh sách danh mục để vẽ Tab/Menu
            AllCategories = _categoryService.GetAll();

            // 2. Lấy danh sách sản phẩm theo bộ lọc
            Products = _productService.GetProductsForAdmin(CurrentParentId, CurrentCategoryId);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int? parentId)
        {
            // Code xóa dưới DB của bạn
            _productService.Delete(id);
            TempData["SuccessMessage"] = "Đã xóa sản phẩm thành công!";

            // PHÁT LOA ĐẾN TẤT CẢ KHÁCH HÀNG: "Sản phẩm có ID này bị xóa rồi!"
            await _hubContext.Clients.All.SendAsync("ReceiveProductDelete", id);

            return RedirectToPage("./Index", new { CurrentParentId = parentId });
        }
    }
}