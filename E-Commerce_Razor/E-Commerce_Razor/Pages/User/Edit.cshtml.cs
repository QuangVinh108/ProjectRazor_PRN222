using BLL.DTOs;
using BLL.IService; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace E_Commerce_Razor.Pages.User
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public EditModel(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        [BindProperty]
        public EditUserViewModel Input { get; set; } = new EditUserViewModel();

        public SelectList Roles { get; set; }

        public IActionResult OnGet(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null) return NotFound();

            Input = new EditUserViewModel
            {
                UserId = user.UserId,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                RoleId = user.RoleId,
                IsActive = user.IsActive
            };

            LoadRolesDropdown();
            return Page();
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
                _userService.UpdateUser(Input);
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToPage("./Index");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi cập nhật: " + ex.Message);
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