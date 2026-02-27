using E_Commerce_Razor.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace E_Commerce_Razor.Pages.Account
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public RegisterViewModel Input { get; set; } = new RegisterViewModel();

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: BLL xử lý Validate OTP và thêm user vào database ở đây
            // var result = _userService.Register(Input);

            // Tạm thời hiển thị success message
            TempData["SuccessMessage"] = "Đăng ký thành công!";
            return RedirectToPage("/Account/Login");
        }

        // Xử lý gửi OTP khi AJAX fetch '?handler=SendOtp'
        public IActionResult OnPostSendOtp([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new JsonResult(new { success = false, message = "Email không hợp lệ" });
            }

            try
            {
                // TODO: Gọi BLL để tạo OTP và gửi qua Email
                // _emailService.SendOtp(email);

                // Giả lập gửi thành công
                return new JsonResult(new { success = true, message = "Mã OTP đã được gửi đến email" });
            }
            catch (System.Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}