using BLL.DTOs;
using BLL.IService;
using DAL.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Admin.Orders
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IOrderRepository _orderRepo;

        public IndexModel(IOrderService orderService, IOrderRepository orderRepo)
        {
            _orderService = orderService;
            _orderRepo = orderRepo;
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

        // [NÚT BÍ MẬT DÀNH CHO DEMO THUYẾT TRÌNH]
        public async Task<IActionResult> OnPostDemoDeliverAsync(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id, includeDetails: true);
            if (order != null && (order.Status == "Paid" || order.Status == "Shipped"))
            {
                order.Status = "Delivered";
                
                // Giả định Shipping cho đẹp Database nếu chưa có
                if (order.Shipping != null)
                {
                    order.Shipping.DeliveryDate = DateTime.Now;
                    if (!order.Shipping.ShippedDate.HasValue) order.Shipping.ShippedDate = DateTime.Now.AddHours(-12);
                }

                await _orderRepo.UpdateAsync(order);
                TempData["Success"] = $"[ĐÃ MÔ PHỎNG] Đơn hàng #{id} đã được đánh dấu là GIAO MÔ PHỎNG THÀNH CÔNG để test đổi/trả!";
            }
            return RedirectToPage();
        }
    }
}
