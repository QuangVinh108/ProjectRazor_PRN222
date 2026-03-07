using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Account
{
    [Authorize] // Bắt buộc đăng nhập
    public class EditProfileModel : PageModel
    {
        private readonly IUserService _userService;

        public EditProfileModel(IUserService userService)
        {
            _userService = userService;
        }

        // Bắt dữ liệu Form
        [BindProperty]
        public UpdateProfileViewModel Input { get; set; } = new UpdateProfileViewModel();

        // Biến để view biết user đã verify hay chưa
        public bool IsVerified { get; set; }

        public IActionResult OnGet()
        {
            // Lấy User ID hiện tại
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToPage("/Account/Login");

            int userId = int.Parse(userIdClaim);

            // Lấy thông tin user từ Service
            var user = _userService.GetUserById(userId);
            if (user == null)
                return NotFound();

            // Gán trạng thái xác thực để View xử lý UI
            IsVerified = user.IsIdentityVerified;

            // Map dữ liệu sang ViewModel để hiển thị lên form
            Input = new UpdateProfileViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Address = user.Address
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            // Cần gán lại IsVerified nếu form bị lỗi, vì OnPost sẽ mất trạng thái này
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToPage("/Account/Login");
            int userId = int.Parse(userIdClaim);

            if (!ModelState.IsValid)
            {
                var user = _userService.GetUserById(userId);
                if (user != null) IsVerified = user.IsIdentityVerified;

                return Page(); // Trả về form nếu có lỗi validation
            }

            try
            {
                // Gọi Service để cập nhật
                _userService.UpdateProfile(userId, Input);

                // Thông báo và chuyển hướng về trang Profile
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToPage("./Profile"); // Quay về thư mục hiện tại (Account/Profile)
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);

                var user = _userService.GetUserById(userId);
                if (user != null) IsVerified = user.IsIdentityVerified;

                return Page();
            }
        }
    }
}