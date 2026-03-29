using BLL.DTOs;
using BLL.Helper;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Account
{
    [Authorize]
    public class EkycModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly GeminiHelper _geminiHelper;

        public EkycModel(IUserService userService, GeminiHelper geminiHelper)
        {
            _userService = userService;
            _geminiHelper = geminiHelper;
        }

        [BindProperty]
        public EkycRequestViewModel Input { get; set; } = new EkycRequestViewModel();

        public bool IsVerified { get; set; }

        public void OnGet()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId > 0)
            {
                var user = _userService.GetUserById(userId);
                IsVerified = user?.IsIdentityVerified ?? false;
            }
        }

        // Sửa hàm cũ thành hàm này
        public async Task<IActionResult> OnPostAnalyzeCccdAsync(IFormFile fileFront, string liveFaceBase64)
        {
            if (fileFront == null || fileFront.Length == 0)
                return new JsonResult(new { success = false, message = "Không tìm thấy ảnh mặt trước CCCD" });

            if (string.IsNullOrEmpty(liveFaceBase64))
                return new JsonResult(new { success = false, message = "Vui lòng chụp ảnh khuôn mặt!" });

            try
            {
                // Gửi cả ảnh Căn cước và Ảnh chụp thẳng vào Gemini AI
                var result = await _geminiHelper.AnalyzeIdCardAsync(fileFront, liveFaceBase64);

                if (result == null)
                    return new JsonResult(new { success = false, message = "Lỗi kết nối AI để phân tích ảnh." });

                // 1. Kiểm tra coi có nhìn ra CCCD không
                if (!result.IsValid)
                    return new JsonResult(new { success = false, message = result.Reason ?? "Không nhận diện được CCCD hợp lệ." });

                // 2. Chặn lại NẾU KHUÔN MẶT KHÔNG KHỚP
                if (!result.IsFaceMatch)
                    return new JsonResult(new { success = false, message = result.Reason ?? "Khuôn mặt chụp được KHÔNG KHỚP với ảnh trên CCCD!" });

                // Vượt qua hết thì trả về Data
                return new JsonResult(new { success = true, data = result.Data });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Account/Login");

            int userId = int.Parse(userIdStr);
            var user = _userService.GetUserById(userId);
            if (user == null) return NotFound();

            IsVerified = user.IsIdentityVerified;
            if (IsVerified) return Page();

            // Loại bỏ Validation cho 2 file up lên vì ở form submit lúc này là submit dữ liệu thông tin, 
            // các ảnh thực chất chưa lên Server qua form thuần nếu bấm Accept từ AI (Phải xử lý dạng IFormFile riêng nếu cần)
            ModelState.Remove("Input.FrontImage");
            ModelState.Remove("Input.BackImage");
            ModelState.Remove("Input.LiveFaceBase64");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng hoàn thành đầy đủ thông tin.";
                return Page();
            }

            try
            {
                var existingUser = _userService.GetUserByCccd(Input.CccdNumber);
                if (existingUser != null && existingUser.UserId != userId)
                {
                    TempData["ErrorMessage"] = "Số CCCD này đã được liên kết với một tài khoản khác.";
                    return Page();
                }

                user.CccdNumber = Input.CccdNumber;
                user.FullName = Input.FullName;
                user.DateOfBirth = Input.DateOfBirth;
                user.Address = Input.Address;

                user.IsIdentityVerified = true;
                user.IdentityRejectReason = null;

                _userService.UpdateUser(user);

                TempData["SuccessMessage"] = "Xác thực danh tính và khuôn mặt thành công hệ thống tự động!";
                IsVerified = true;
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống khi lưu thông tin: " + ex.Message;
                return Page();
            }
        }
    }
}