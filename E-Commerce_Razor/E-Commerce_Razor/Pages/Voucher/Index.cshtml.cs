using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Voucher
{
    // Cần đăng nhập để lưu mã
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IVoucherService _voucherService;

        public IndexModel(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        // Tạo 1 ViewModel để chứa VoucherDto và cờ IsSaved
        public class VoucherDisplayItem
        {
            public VoucherDto Voucher { get; set; } = null!;
            public bool IsSaved { get; set; }
        }

        public List<VoucherDisplayItem> Vouchers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return;

            var allVouchers = await _voucherService.GetAllAsync();
            var savedVoucherIdsList = await _voucherService.GetAllSavedVoucherIdsAsync(userId);
            var savedVoucherIds = savedVoucherIdsList.ToHashSet();

            var now = DateTime.Now;

            // Hiển thị tất cả voucher đang active, chưa hết hạn, và số lượt trên hệ thống còn
            Vouchers = allVouchers.Where(v =>
                v.IsActive &&
                v.EndDate >= now &&
                v.UsedCount < v.UsageLimit)
                .OrderBy(v => v.MinOrderValue)
                .Select(v => new VoucherDisplayItem
                {
                    Voucher = v,
                    IsSaved = savedVoucherIds.Contains(v.VoucherId)
                })
                .ToList();
        }

        // AJAX POST: /Voucher?handler=Save
        public async Task<IActionResult> OnPostSaveAsync([FromBody] int voucherId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return new JsonResult(new { isSuccess = false, errorMessage = "Vui lòng đăng nhập" });

            try
            {
                await _voucherService.SaveVoucherAsync(userId, voucherId);
                return new JsonResult(new { isSuccess = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { isSuccess = false, errorMessage = ex.Message });
            }
        }
    }
}
