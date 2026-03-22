using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using E_Commerce_Razor.Hubs;

namespace E_Commerce_Razor.Pages.Admin.Vouchers
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly IVoucherService _voucherService;
        private readonly IHubContext<AppHub> _hubContext;

        public CreateModel(IVoucherService voucherService, IHubContext<AppHub> hubContext)
        {
            _voucherService = voucherService;
            _hubContext = hubContext;
        }

        [BindProperty]
        public CreateVoucherDto Input { get; set; } = new CreateVoucherDto
        {
            StartDate    = DateTime.Today,
            EndDate      = DateTime.Today.AddMonths(1),
            UsageLimit   = 100,
            IsActive     = true,
            DiscountType = "Fixed"
        };

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (Input.EndDate <= Input.StartDate)
            {
                ModelState.AddModelError("Input.EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                return Page();
            }

            if (Input.DiscountType == "Percent" && Input.DiscountValue > 100)
            {
                ModelState.AddModelError("Input.DiscountValue", "Phần trăm giảm giá không được vượt quá 100%.");
                return Page();
            }

            try
            {
                await _voucherService.CreateAsync(Input);
                
                // 🔥 Broadcast new voucher to ALL connected clients using SignalR
                var discountStr = Input.DiscountType == "Percent" ? $"{Input.DiscountValue}%" : $"{Input.DiscountValue:N0}đ";
                var msg = $"Mã giảm mới {Input.Code}: Giảm {discountStr} - {Input.Description}";
                await _hubContext.Clients.All.SendAsync("ReceiveNewVoucher", Input.Code, msg);

                TempData["SuccessMessage"] = $"Đã tạo mã voucher '{Input.Code}' thành công!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
