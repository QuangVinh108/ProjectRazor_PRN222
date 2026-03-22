using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Admin.Returns
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IReturnRequestService _returnService;

        public IndexModel(IReturnRequestService returnService)
        {
            _returnService = returnService;
        }

        public List<ReturnRequestDto> Returns { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Lấy tất cả hoặc chỉ chờ duyệt tùy logic, ở đây lấy tất cả
            Returns = await _returnService.GetAllAsync();
        }
    }
}
