using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ClosedXML.Excel;
namespace E_Commerce_Razor.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReportModel : PageModel
    {
        // 1. MỞ COMMENT Ở ĐÂY ĐỂ DÙNG SERVICE THẬT
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<ReportModel> _logger;

        public ReportModel(IDashboardService dashboardService, ILogger<ReportModel> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public void OnGet()
        {
            // Chỉ load khung HTML
        }

        public async Task<IActionResult> OnGetUserGrowthChartDataAsync(int months = 6)
        {
            try
            {
                // Bỏ fake, dùng data thật cho Chart người dùng
                var data = await _dashboardService.GetUserGrowthChartAsync(months);
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetUserGrowthChartData");
                return new JsonResult(new { error = ex.Message });
            }
        }

        // 2. NHẬN THÊM BIẾN compareType TỪ GIAO DIỆN GỬI LÊN
        public async Task<IActionResult> OnGetReportDataAsync(DateTime startDate, DateTime endDate, string reportType, string compareType = "None")
        {
            try
            {
                if (startDate == DateTime.MinValue || endDate == DateTime.MinValue)
                    return new JsonResult(new { success = false, message = "Vui lòng chọn ngày" });

                if (startDate > endDate)
                    return new JsonResult(new { success = false, message = "Ngày bắt đầu lớn hơn ngày kết thúc" });

                // 3. GỌI XUỐNG SERVICE THẬT ĐỂ LẤY DỮ LIỆU TỪ SQL
                var data = await _dashboardService.GetAdvancedReportDataAsync(startDate, endDate, reportType, compareType);

                return new JsonResult(new { success = true, data = data, type = reportType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetReportData");
                return new JsonResult(new { success = false, message = "Lỗi hệ thống khi tạo báo cáo" });
            }
        }
        public async Task<IActionResult> OnGetExportExcelAsync(DateTime startDate, DateTime endDate, string reportType, string compareType = "None")
        {
            try
            {
                if (startDate == DateTime.MinValue || endDate == DateTime.MinValue)
                    return BadRequest("Vui lòng chọn ngày hợp lệ.");

                // 1. Gọi Service lấy dữ liệu giống y hệt như lúc vẽ bảng trên Web
                var data = await _dashboardService.GetAdvancedReportDataAsync(startDate, endDate, reportType, compareType);

                // Lấy dữ liệu của kỳ hiện tại để xuất Excel
                var tableData = data.TableData;

                // 2. Tạo File Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Báo Cáo");
                    int currentRow = 1;

                    // 3. Tạo Header (Tiêu đề cột) tùy theo loại báo cáo
                    switch (reportType.ToLower())
                    {
                        case "revenue":
                            worksheet.Cell(currentRow, 1).Value = "Ngày";
                            worksheet.Cell(currentRow, 2).Value = "Số đơn hoàn thành";
                            worksheet.Cell(currentRow, 3).Value = "Doanh thu (VNĐ)";
                            break;
                        case "products":
                            worksheet.Cell(currentRow, 1).Value = "Tên sản phẩm";
                            worksheet.Cell(currentRow, 2).Value = "Số lượng đã bán";
                            worksheet.Cell(currentRow, 3).Value = "Doanh thu (VNĐ)";
                            break;
                        case "categories":
                            worksheet.Cell(currentRow, 1).Value = "Tên danh mục";
                            worksheet.Cell(currentRow, 2).Value = "Số lượng bán";
                            worksheet.Cell(currentRow, 3).Value = "Đóng góp doanh thu (VNĐ)";
                            break;
                        case "payment_methods":
                            worksheet.Cell(currentRow, 1).Value = "Phương thức thanh toán";
                            worksheet.Cell(currentRow, 2).Value = "Số giao dịch";
                            worksheet.Cell(currentRow, 3).Value = "Tổng giá trị (VNĐ)";
                            break;
                    }

                    // Format in đậm và tô nền xám cho dòng Header
                    var headerRange = worksheet.Range(1, 1, 1, 3);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // 4. Đổ dữ liệu từ danh sách vào các dòng tiếp theo
                    foreach (var item in tableData)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = item.Label;
                        worksheet.Cell(currentRow, 2).Value = item.Count;
                        worksheet.Cell(currentRow, 3).Value = (double)item.Value;
                        // Format cột số 3 thành định dạng tiền tệ có dấu phẩy (VD: 15,000,000)
                        worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "#,##0";
                    }

                    // Tự động kéo dãn độ rộng các cột cho vừa với chữ
                    worksheet.Columns().AdjustToContents();

                    // 5. Chuyển File thành mảng byte và trả về cho trình duyệt tải xuống
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        string fileName = $"BaoCao_{reportType}_{DateTime.Now:ddMMyyyy}.xlsx";

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất file Excel");
                return BadRequest("Đã xảy ra lỗi khi xuất Excel. Vui lòng thử lại.");
            }
        }
    }
}