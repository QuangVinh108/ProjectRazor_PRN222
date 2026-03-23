using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Voucher
{
    [Authorize]
    public class MyVouchersModel : PageModel
    {
        private readonly IVoucherService _voucherService;

        public MyVouchersModel(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        public List<VoucherDto> Vouchers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return;

            Vouchers = await _voucherService.GetSavedVouchersAsync(userId);
        }
    }
}
