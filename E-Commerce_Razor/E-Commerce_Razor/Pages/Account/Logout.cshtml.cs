using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public void OnGet()
        {
            Response.Redirect("/");
        }

        public IActionResult OnPost()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refresh_token");

            TempData.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";

            return RedirectToPage("/Index"); // Trỏ về trang chủ
        }
    }
}