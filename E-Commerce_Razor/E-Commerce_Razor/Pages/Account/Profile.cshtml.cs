using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Account
{
    [Authorize] // Bắt buộc đăng nhập mới được xem
    public class ProfileModel : PageModel
    {
        private readonly IUserService _userService;

        public ProfileModel(IUserService userService)
        {
            _userService = userService;
        }

        // Biến chứa dữ liệu user để hiển thị ra HTML
        public DAL.Entities.User UserProfile { get; set; }

        public IActionResult OnGet()
        {
            // Lấy User ID từ Cookie (Token)
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            if (string.IsNullOrEmpty(userIdClaim))
            {
                // Nếu không lấy được ID, bắt đăng nhập lại
                return RedirectToPage("/Account/Login");
            }

            int userId = int.Parse(userIdClaim);

            // Gọi Service lấy thông tin User
            UserProfile = _userService.GetUserById(userId);

            if (UserProfile == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}