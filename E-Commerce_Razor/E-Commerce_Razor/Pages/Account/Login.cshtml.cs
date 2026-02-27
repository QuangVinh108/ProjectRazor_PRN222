using BLL.IService;
using E_Commerce_Razor.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        //private readonly IGoogleAuthService _googleAuthService;

        public LoginModel(IAuthService authService)
        {
            _authService = authService;
            //_googleAuthService = googleAuthService;
        }

        [BindProperty]
        public LoginViewModel Input { get; set; } = new LoginViewModel();

        // Tương đương [HttpGet] Login
        public IActionResult OnGet(string returnUrl = "/")
        {
            // Nếu đã login, redirect về trang Product
            var jwtCookie = Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(jwtCookie))
            {
                return RedirectToPage("/Product/Index");
            }

            Input.ReturnUrl = returnUrl;
            return Page();
        }

        // Tương đương [HttpPost] Login
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Console.WriteLine($"=== LOGIN REQUEST === Username: {Input.Username}");
            try
            {
                var (accessToken, refreshToken, role) = await _authService.LoginAsync(Input.Username, Input.Password);

                if (accessToken == null)
                {
                    Console.WriteLine("❌ Login failed - Invalid credentials");
                    ModelState.AddModelError(string.Empty, "Tên tài khoản hoặc mật khẩu không đúng");
                    return Page();
                }

                Console.WriteLine($"✅ Login successful for {Input.Username} - Role: {role}");

                // Set JWT cookie
                Response.Cookies.Append("jwt", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddMinutes(25)
                });

                // Optional: Set refresh token
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    });
                }

                TempData["SuccessMessage"] = "Đăng nhập thành công!";

                // ✅ REDIRECT THEO ROLE
                if (!string.IsNullOrEmpty(role))
                {
                    switch (role.ToLower())
                    {
                        case "admin":
                            return RedirectToPage("/Admin/Dashboard"); // Trỏ về thư mục Pages/Admin/Dashboard

                        case "customer":
                        default:
                            return LocalRedirect(Input.ReturnUrl ?? "/Product/Index");
                    }
                }

                return LocalRedirect(Input.ReturnUrl ?? "/Product/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in Login: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.");
                return Page();
            }
        }

        // Tương đương [HttpPost] GoogleLogin
        // Được gọi thông qua URL: /Account/Login?handler=GoogleLogin
        //public async Task<IActionResult> OnPostGoogleLoginAsync(string idToken, string? returnUrl = "/")
        //{
        //    if (string.IsNullOrWhiteSpace(idToken))
        //    {
        //        TempData["ErrorMessage"] = "Lỗi xác thực Google";
        //        return RedirectToPage(); // Tải lại chính trang Login
        //    }

        //    try
        //    {
        //        // Verify Google token
        //        var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(idToken);
        //        if (googleUser == null)
        //        {
        //            TempData["ErrorMessage"] = "Google token không hợp lệ";
        //            return RedirectToPage();
        //        }

        //        // Handle login
        //        var result = await _googleAuthService.HandleGoogleLoginAsync(googleUser);
        //        if (!result.Success)
        //        {
        //            TempData["ErrorMessage"] = result.Message ?? "Đăng nhập Google thất bại";
        //            return RedirectToPage();
        //        }

        //        // Set JWT cookie
        //        Response.Cookies.Append("jwt", result.AccessToken!, new CookieOptions
        //        {
        //            HttpOnly = true,
        //            Secure = Request.IsHttps,
        //            SameSite = SameSiteMode.Lax,
        //            Expires = DateTime.UtcNow.AddMinutes(25)
        //        });

        //        // Set refresh token cookie
        //        if (!string.IsNullOrEmpty(result.RefreshToken))
        //        {
        //            Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
        //            {
        //                HttpOnly = true,
        //                Secure = Request.IsHttps,
        //                SameSite = SameSiteMode.Strict,
        //                Expires = DateTime.UtcNow.AddDays(7)
        //            });
        //        }

        //        TempData["SuccessMessage"] = result.Message;

        //        if (!string.IsNullOrEmpty(result.Role))
        //        {
        //            switch (result.Role.ToLower())
        //            {
        //                case "admin":
        //                    return RedirectToPage("/Admin/Dashboard");
        //                case "manager":
        //                    return RedirectToPage("/Manager/Dashboard");
        //                default:
        //                    return LocalRedirect(returnUrl ?? "/Product/Index");
        //            }
        //        }

        //        return LocalRedirect(returnUrl ?? "/Product/Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Exception in GoogleLogin: {ex.Message}");
        //        TempData["ErrorMessage"] = "Đã xảy ra lỗi khi đăng nhập Google";
        //        return RedirectToPage();
        //    }
        //}
    }
}