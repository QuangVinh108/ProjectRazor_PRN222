using BLL.DTOs.InventoryDTOs;
using BLL.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Inventory
{
    public class EditModel : PageModel
    {
        private readonly IInventoryService _service;

        public EditModel(IInventoryService service)
        {
            _service = service;
        }

        [BindProperty]
        public UpdateInventoryDto Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ProductId { get; set; }

        public async Task<IActionResult> OnGetAsync(int productId)
        {
            ProductId = productId;

            var result = await _service.GetByProductIdAsync(productId);

            // 1️⃣ Service thất bại (ví dụ lỗi DB, logic...)
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("",
                    result.Errors.FirstOrDefault() ?? result.Message ?? "Có lỗi xảy ra");

                return Page();
            }

            // 2️⃣ Không tìm thấy dữ liệu
            if (result.Data == null)
            {
                return NotFound();
            }

            // 3️⃣ Map dữ liệu
            Input = new UpdateInventoryDto
            {
                Quantity = result.Data.Quantity,
                Warehouse = result.Data.Warehouse
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var result = await _service.UpdateAsync(ProductId, Input);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.Message);
                return Page();
            }

            TempData["Success"] = "Cập nhật thành công!";
            return RedirectToPage("Index");
        }
    }
}