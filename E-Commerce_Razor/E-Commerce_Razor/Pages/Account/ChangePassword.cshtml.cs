using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Account
{
    [Authorize] // Yêu cầu phải đăng nhập mới được truy cập trang này
    public class ChangePasswordModel : PageModel
    {
        private readonly IUserService _userService;

        public ChangePasswordModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public ChangePasswordViewModel Input { get; set; } = new ChangePasswordViewModel();

        public void OnGet()
        {
            // Chỉ hiển thị form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Trả về form với các thông báo lỗi Validation
            }

            try
            {
                // Lấy UserId từ Cookie/Token
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    TempData["ErrorMessage"] = "Không thể xác thực danh tính người dùng.";
                    return RedirectToPage("/Account/Login");
                }

                int userId = int.Parse(userIdClaim);

                // Gọi Service xử lý đổi mật khẩu
                var result = await _userService.ChangePasswordAsync(userId, Input.CurrentPassword, Input.NewPassword);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToPage("/Index"); // Chuyển về trang chủ
                }
                else
                {
                    // Lỗi từ Service (VD: Mật khẩu cũ không đúng)
                    ModelState.AddModelError(string.Empty, result.Message);
                    return Page();
                }
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống: " + ex.Message);
                return Page();
            }
        }
    }
}