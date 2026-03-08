using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Admin.Orders
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;

        public IndexModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public List<OrderDto> Orders { get; set; } = new();
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var all = await _orderService.GetAllOrdersAsync();
            Orders = string.IsNullOrEmpty(StatusFilter)
                ? all
                : all.Where(o => o.Status == StatusFilter).ToList();
        }
    }
}
