using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(IDashboardService dashboardService, ILogger<DashboardModel> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public DashboardStatisticsDTO Stats { get; set; } = new DashboardStatisticsDTO();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Stats = await _dashboardService.GetDashboardStatisticsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi load Dashboard");
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnGetRevenueChartDataAsync(int days = 30)
        {
            try
            {
                var result = await _dashboardService.GetRevenueChartDataAsync(days);
                return new JsonResult(new
                {
                    labels = result.Labels,
                    data   = result.Data
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetOrderStatusChartDataAsync()
        {
            try
            {
                var result = await _dashboardService.GetOrderStatusChartAsync();
                return new JsonResult(new
                {
                    labels = result.Labels,
                    data   = result.Data
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetTopProductsAsync()
        {
            try
            {
                var result = await _dashboardService.GetTopProductsAsync(5);
                return new JsonResult(result.Select(p => new
                {
                    productId   = p.ProductId,
                    productName = p.ProductName,
                    image       = p.Image,
                    totalSold   = p.TotalSold,
                    revenue     = p.Revenue
                }));
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetRecentOrdersAsync()
        {
            try
            {
                var result = await _dashboardService.GetRecentOrdersAsync(10);
                return new JsonResult(result.Select(o => new
                {
                    orderId      = o.OrderId,
                    orderDate    = o.OrderDate,
                    customerName = o.CustomerName,
                    totalAmount  = o.TotalAmount,
                    status       = o.Status
                }));
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }
    }
}