using DAL.Entities;
using BLL.IService; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace E_Commerce_Razor.Pages.User
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;

        public IndexModel(IUserService userService)
        {
            _userService = userService;
        }

        public IEnumerable<DAL.Entities.User> Users { get; set; } = new List<DAL.Entities.User>();

        public void OnGet()
        {
            Users = _userService.GetAllUsers();
        }

        public IActionResult OnPostDelete(int id)
        {
            _userService.DeleteUser(id);
            TempData["SuccessMessage"] = "Khóa tài khoản thành công!";
            return RedirectToPage("./Index");
        }
    }
}