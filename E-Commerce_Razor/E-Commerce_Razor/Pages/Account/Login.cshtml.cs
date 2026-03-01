using BLL.IService;
using DAL.IRepository;
using E_Commerce_Razor.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.SqlServer.Server;
using Google.Apis.Auth; 
using DAL.Entities;
namespace E_Commerce_Razor.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository; // Bổ sung Repository

        // Tiêm IUserRepository vào constructor
        public LoginModel(IAuthService authService, IUserRepository userRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
        }

        [BindProperty]
        public LoginViewModel Input { get; set; } = new LoginViewModel();

        public IActionResult OnGet(string returnUrl = "/")
        {
            var jwtCookie = Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(jwtCookie))
            {
                return RedirectToPage("/Product/Index");
            }

            Input.ReturnUrl = returnUrl;
            return Page();
        }

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

                SetCookies(accessToken, refreshToken);
                TempData["SuccessMessage"] = "Đăng nhập thành công!";

                return RedirectBasedOnRole(role, Input.ReturnUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in Login: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.");
                return Page();
            }
        }

        // --- XỬ LÝ ĐĂNG NHẬP GOOGLE ---
        public async Task<IActionResult> OnPostGoogleLoginAsync([FromForm] string idToken, [FromForm] string? returnUrl = "/")
        {
            // 1. QUAN TRỌNG: Xóa lỗi Validation của Username/Password
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(idToken))
            {
                TempData["ErrorMessage"] = "Không nhận được thông tin xác thực từ Google.";
                return RedirectToPage();
            }

            try
            {
                // 2. Xác thực Token với Google
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { "477307811776-7f6ptpobvega67l3cd6ghin732dntka2.apps.googleusercontent.com" }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                // 3. Kiểm tra user trong Database
                var user = await _userRepository.GetByEmailAsync(payload.Email);
                string roleName = "Customer"; // Mặc định role

                if (user == null)
                {
                    // Tạo tài khoản mới nếu chưa tồn tại
                    var customerRole = await _userRepository.GetRoleByNameAsync("Customer");

                    user = new DAL.Entities.User
                    {
                        UserName = payload.Email.Split('@')[0] + "_google",
                        Email = payload.Email,
                        FullName = payload.Name,
                        PasswordHash = null,
                        LoginProvider = "Google",
                        GoogleId = payload.Subject,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        EmailConfirmed = true,
                        EmailConfirmedAt = DateTime.UtcNow,
                        RoleId = customerRole?.RoleId ?? 2
                    };

                    await _userRepository.AddUserAsync(user);
                    await _userRepository.SaveChangesAsync();

                    roleName = customerRole?.RoleName ?? "Customer";
                    Console.WriteLine($"✅ Google User created: {user.UserName}");
                }
                else
                {
                    // Nếu user đã tồn tại, lấy role name của họ
                    var userWithRole = await _userRepository.GetByIdWithRoleAsync(user.UserId);
                    roleName = userWithRole?.Role?.RoleName ?? "Customer";
                }

                // 4. Sinh Token JWT bằng AuthService có sẵn của bạn
                var (accessToken, refreshToken) = await _authService.GenerateTokensAsync(user.UserId);

                if (accessToken == null)
                {
                    TempData["ErrorMessage"] = "Tài khoản của bạn đang bị khóa hoặc không hợp lệ.";
                    return RedirectToPage();
                }

                // 5. Lưu Token vào Cookie và chuyển hướng
                SetCookies(accessToken, refreshToken);
                TempData["SuccessMessage"] = "Đăng nhập Google thành công!";

                return RedirectBasedOnRole(roleName, returnUrl);
            }
            catch (InvalidJwtException)
            {
                TempData["ErrorMessage"] = "Token xác thực của Google không hợp lệ hoặc đã hết hạn.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GoogleLogin: {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi đăng nhập bằng Google.";
                return RedirectToPage();
            }
        }

        // --- HÀM HỖ TRỢ DÙNG CHUNG ---

        private void SetCookies(string accessToken, string? refreshToken)
        {
            Response.Cookies.Append("jwt", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(25)
            });

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
        }

        private IActionResult RedirectBasedOnRole(string? role, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(role))
            {
                switch (role.ToLower())
                {
                    case "admin":
                        return RedirectToPage("/Admin/Dashboard");
                    case "manager":
                        return RedirectToPage("/Manager/Dashboard");
                    default:
                        return LocalRedirect(returnUrl ?? "/Product/Index");
                }
            }
            return LocalRedirect(returnUrl ?? "/Product/Index");
        }
    }
}