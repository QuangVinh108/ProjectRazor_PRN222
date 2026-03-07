using BLL.DTOs;
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
    public class EditModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<AppHub> _hubContext;

        public EditModel(IProductService productService, ICategoryService categoryService, IWebHostEnvironment webHostEnvironment, IHubContext<AppHub> hubContext)
        {
            _productService = productService;
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
        }

        [BindProperty]
        public CreateProductViewModel Input { get; set; } = new CreateProductViewModel();

        [BindProperty(SupportsGet = true)]
        public int? ReturnParentId { get; set; }

        public SelectList CategoriesList { get; set; }

        public IActionResult OnGet(int id, int? returnParentId)
        {
            ReturnParentId = returnParentId;

            // Fetch từ DB
            var product = _productService.GetById(id);
            if (product == null) return NotFound();

            // Gán vào Input form
            Input.ProductId = product.ProductId;
            Input.ProductName = product.ProductName;
            Input.Sku = product.Sku;
            Input.Price = product.Price;
            Input.CategoryId = product.CategoryId;
            Input.Status = product.Status;
            Input.Description = product.Description;
            Input.Image = product.Image;

            LoadCategories();
            return Page();
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
                var existingProduct = _productService.GetById(Input.ProductId);
                Input.Image = existingProduct.Image; // Giữ ảnh cũ mặc định

                if (Input.ImageFile != null)
                {
                    // Lưu ảnh mới
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.ImageFile.FileName;
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.ImageFile.CopyToAsync(fileStream);
                    }
                    Input.Image = "/images/products/" + uniqueFileName;

                    // Xóa ảnh cũ
                    if (!string.IsNullOrEmpty(existingProduct.Image))
                    {
                        string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.Image.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }

                _productService.Update(Input);

                var allCategories = _categoryService.GetAll();
                var categoryName = allCategories.FirstOrDefault(c => c.CategoryId == Input.CategoryId)?.CategoryName ?? "Chưa phân loại";

                var updatedProductData = new
                {
                    productId = Input.ProductId,
                    productName = Input.ProductName,
                    price = Input.Price,
                    image = string.IsNullOrEmpty(Input.Image) ? "https://placehold.co/400x400?text=No+Image" : Input.Image,
                    categoryName = categoryName,
                    status = Input.Status,
                    sku = Input.Sku,
                    description = Input.Description
                };

                // PHÁT LOA: "Sản phẩm này vừa bị đổi thông tin nhé!"
                await _hubContext.Clients.All.SendAsync("ReceiveProductUpdate", updatedProductData);

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToPage("./Index", new { CurrentParentId = ReturnParentId });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                LoadCategories();
                return Page();
            }
        }

        private void LoadCategories()
        {
            var categories = _categoryService.GetAll();
            CategoriesList = new SelectList(categories, "CategoryId", "CategoryName", Input.CategoryId);
        }
    }
}