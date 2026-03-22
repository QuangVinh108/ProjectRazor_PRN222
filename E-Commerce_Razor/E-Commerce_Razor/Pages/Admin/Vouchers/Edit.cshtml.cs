using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Admin.Vouchers
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly IVoucherService _voucherService;

        public EditModel(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [BindProperty]
        public CreateVoucherDto Input { get; set; } = new();

        public int VoucherId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var voucher = await _voucherService.GetByIdAsync(id);
            if (voucher == null) return NotFound();

            VoucherId = id;
            Input = new CreateVoucherDto
            {
                Code          = voucher.Code,
                Description   = voucher.Description,
                DiscountType  = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                MinOrderValue = voucher.MinOrderValue,
                MaxDiscount   = voucher.MaxDiscount,
                UsageLimit    = voucher.UsageLimit,
                StartDate     = voucher.StartDate,
                EndDate       = voucher.EndDate,
                IsActive      = voucher.IsActive
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                VoucherId = id;
                return Page();
            }

            try
            {
                await _voucherService.UpdateAsync(id, Input);
                TempData["SuccessMessage"] = "Đã cập nhật voucher thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                VoucherId = id;
                return Page();
            }
        }
    }
}
