using BLL.DTOs;
// using BLL.IService; // Nhớ mở comment khi có file thật
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace E_Commerce_Razor.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        // TODO: Mở comment dòng dưới khi bạn đã khai báo Service
        // private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardModel> _logger;

        // Bỏ comment IDashboardService khi tiêm thật
        public DashboardModel(/* IDashboardService dashboardService, */ ILogger<DashboardModel> logger)
        {
            // _dashboardService = dashboardService;
            _logger = logger;
        }

        // Biến để hiển thị ngoài HTML
        public DashboardStatisticsDTO Stats { get; set; } = new DashboardStatisticsDTO();

        // 1. Hàm load giao diện chính
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Stats = await _dashboardService.GetDashboardStatisticsAsync();

                // TODO: Bỏ code giả lập dưới đây khi có BLL
                Stats = new DashboardStatisticsDTO
                {
                    TotalRevenue = 150000000,
                    RevenueThisMonth = 45000000,
                    RevenueGrowthPercent = 12,
                    TotalOrders = 1250,
                    OrdersThisMonth = 320,
                    OrderGrowthPercent = 5,
                    TotalUsers = 5400,
                    NewUsersThisMonth = 120,
                    UserGrowthPercent = 8,
                    TotalProducts = 850,
                    AverageOrderValue = 120000
                };

                if (Stats == null)
                {
                    TempData["ErrorMessage"] = "Không thể tải dữ liệu dashboard";
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi load Dashboard");
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return Page();
            }
        }

        // ==========================================
        // API ENDPOINTS (Dành cho Chart và Bảng)
        // ==========================================

        public async Task<IActionResult> OnGetRevenueChartDataAsync(int days = 30)
        {
            try
            {
                // var data = await _dashboardService.GetRevenueChartDataAsync(days);

                // Giả lập
                var data = new
                {
                    labels = new[] { "01/03", "02/03", "03/03", "04/03", "05/03" },
                    data = new[] { 1500000, 2300000, 1800000, 3100000, 2800000 }
                };
                return new JsonResult(data);
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
                // var data = await _dashboardService.GetOrderStatusChartAsync();

                // Giả lập
                var data = new
                {
                    labels = new[] { "Pending", "Processing", "Hoàn thành", "Cancelled", "Shipped" },
                    data = new[] { 15, 25, 120, 5, 45 }
                };
                return new JsonResult(data);
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
                // var data = await _dashboardService.GetTopProductsAsync(5);

                // Giả lập
                var data = new[] {
                    new { productId = "SP01", productName = "Iphone 15", image = "", totalSold = 150, revenue = 45000000 },
                    new { productId = "SP02", productName = "Samsung S24", image = "", totalSold = 120, revenue = 36000000 }
                };
                return new JsonResult(data);
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
                // var data = await _dashboardService.GetRecentOrdersAsync(10);

                // Giả lập
                var data = new[] {
                    new { orderId = "ORD001", orderDate = DateTime.Now, customerName = "Nguyễn Văn A", totalAmount = 1500000, status = "Hoàn thành" },
                    new { orderId = "ORD002", orderDate = DateTime.Now.AddDays(-1), customerName = "Trần Thị B", totalAmount = 2300000, status = "Pending" }
                };
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }
    }
}