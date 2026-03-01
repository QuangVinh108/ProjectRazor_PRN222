using BLL.DTOs;
using BLL.Helper;
using BLL.IService;
using E_Commerce_Razor.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using System.Linq;

namespace E_Commerce_Razor.Pages.Product
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly GeminiHelper _geminiHelper;
        private readonly IHubContext<AppHub> _hubContext;

        public CreateModel(IProductService productService, ICategoryService categoryService, IWebHostEnvironment webHostEnvironment, GeminiHelper geminiHelper, IHubContext<AppHub> hubContext)
        {
            _productService = productService;
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;
            _geminiHelper = geminiHelper;
            _hubContext = hubContext;
        }

        [BindProperty]
        public CreateProductViewModel Input { get; set; } = new CreateProductViewModel();

        [BindProperty(SupportsGet = true)]
        public int? ReturnParentId { get; set; }

        public SelectList CategoriesList { get; set; }

        public void OnGet()
        {
            LoadCategories();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            try
            {
                // Xử lý upload ảnh
                if (Input.ImageFile != null)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.ImageFile.FileName;
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.ImageFile.CopyToAsync(fileStream);
                    }
                    Input.Image = "/images/products/" + uniqueFileName;
                }

                int realProductId = _productService.Create(Input);

                var allCategories = _categoryService.GetAll();
                var categoryName = allCategories.FirstOrDefault(c => c.CategoryId == Input.CategoryId)?.CategoryName ?? "Chưa phân loại";

                var newProductData = new
                {
                    productId = realProductId, 
                    productName = Input.ProductName,
                    price = Input.Price,
                    image = string.IsNullOrEmpty(Input.Image) ? "https://placehold.co/400x400?text=No+Image" : Input.Image,
                    categoryName = categoryName,
                    sku = Input.Sku,
                    status = Input.Status
                };

                await _hubContext.Clients.All.SendAsync("ReceiveProductCreate", newProductData);

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToPage("./Index", new { CurrentParentId = ReturnParentId });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                LoadCategories();
                return Page();
            }
        }

        // HANDLER CHO GEMINI AI
        public async Task<IActionResult> OnPostAnalyzeImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new JsonResult(new { success = false, message = "Vui lòng chọn ảnh" });

            try
            {
                var categories = _categoryService.GetAll()
                                                 .Select(c => c.CategoryName)
                                                 .ToList();

                var result = await _geminiHelper.AnalyzeImageAsync(file, categories);

                if (result != null)
                {
                    return new JsonResult(new { success = true, data = result });
                }

                return new JsonResult(new { success = false, message = "Không thể phân tích ảnh" });
            }
            catch (System.Exception ex)
            {
                return new JsonResult(new { success = false, message = "Lỗi phân tích: " + ex.Message });
            }
        }

        private void LoadCategories()
        {
            var allCats = _categoryService.GetAll();
            IEnumerable<CategoryDTO> catsForDropdown;

            if (ReturnParentId.HasValue)
            {
                catsForDropdown = allCats.Where(c => c.ParentId == ReturnParentId);
            }
            else
            {
                catsForDropdown = allCats.Where(c => c.ParentId != null);
            }

            CategoriesList = new SelectList(catsForDropdown, "CategoryId", "CategoryName", Input.CategoryId);
        }
    }
}