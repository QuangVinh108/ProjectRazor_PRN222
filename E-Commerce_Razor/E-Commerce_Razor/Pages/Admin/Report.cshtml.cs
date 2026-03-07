using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace E_Commerce_Razor.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReportModel : PageModel
    {
        // private readonly IDashboardService _dashboardService;
        private readonly ILogger<ReportModel> _logger;

        public ReportModel(/* IDashboardService dashboardService, */ ILogger<ReportModel> logger)
        {
            // _dashboardService = dashboardService;
            _logger = logger;
        }

        public void OnGet()
        {
            // Chỉ load khung HTML
        }

        // API trả về biểu đồ người dùng mới
        public async Task<IActionResult> OnGetUserGrowthChartDataAsync(int months = 6)
        {
            try
            {
                // var data = await _dashboardService.GetUserGrowthChartAsync(months);

                // Giả lập
                var data = new
                {
                    labels = new[] { "Tháng 10", "Tháng 11", "Tháng 12", "Tháng 1", "Tháng 2", "Tháng 3" },
                    data = new[] { 45, 60, 55, 80, 120, 150 }
                };
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        // API trả về bảng báo cáo
        public async Task<IActionResult> OnGetReportDataAsync(DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                if (startDate == DateTime.MinValue || endDate == DateTime.MinValue)
                    return new JsonResult(new { success = false, message = "Vui lòng chọn ngày" });

                if (startDate > endDate)
                    return new JsonResult(new { success = false, message = "Ngày bắt đầu lớn hơn ngày kết thúc" });

                // var data = await _dashboardService.GetReportDataAsync(startDate, endDate, reportType);

                // Giả lập dữ liệu trả về cho bảng Doanh thu
                var fakeData = new[] {
                    new { label = "01/03/2026", count = 15, value = 15000000, extraInfo = "" },
                    new { label = "02/03/2026", count = 22, value = 25000000, extraInfo = "" }
                };

                return new JsonResult(new { success = true, data = fakeData, type = reportType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetReportData");
                return new JsonResult(new { success = false, message = "Lỗi tạo báo cáo" });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync(DateTime startDate, DateTime endDate, string reportType)
        {
            // Logic xuất file Excel...
            await Task.Delay(100);
            return BadRequest("Tính năng xuất Excel đang phát triển.");
        }
    }
}