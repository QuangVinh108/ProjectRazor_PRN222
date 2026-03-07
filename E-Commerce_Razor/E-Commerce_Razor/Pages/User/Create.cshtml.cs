using BLL.DTOs;
using BLL.IService; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace E_Commerce_Razor.Pages.User
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public CreateModel(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        [BindProperty]
        public CreateUserViewModel Input { get; set; } = new CreateUserViewModel();

        public SelectList Roles { get; set; }

        public void OnGet()
        {
            LoadRolesDropdown();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadRolesDropdown();
                return Page();
            }

            try
            {
                _userService.CreateUser(Input);
                TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                return RedirectToPage("./Index");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra: " + ex.Message);
                LoadRolesDropdown();
                return Page();
            }
        }

        private void LoadRolesDropdown()
        {
            var roles = _roleService.GetAllRoles();
            // Cài tạm dữ liệu giả
            //var roles = new[] { new { RoleId = 1, RoleName = "Admin" }, new { RoleId = 2, RoleName = "User" } };
            Roles = new SelectList(roles, "RoleId", "RoleName");
        }
    }
}