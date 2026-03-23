using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DAL.IRepository;
namespace BLL.DTOs
{
    // ==========================================
    // CÁC DTO MỚI CHO TÍNH NĂNG BÁO CÁO NÂNG CAO
    // ==========================================

    public class AdvancedReportResponseDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal CompareTotalRevenue { get; set; }
        public double GrowthPercentage { get; set; }
        public List<CompareChartPointDTO> ChartData { get; set; } = new();
        public List<ReportResultDTO> TableData { get; set; } = new();

        public List<ReportResultDTO> CompareTableData { get; set; } = new();
    }

    public class CompareChartPointDTO
    {
        public string Label { get; set; } = string.Empty;           // Ngày kỳ hiện tại (VD: "28/02")
        public string CompareLabel { get; set; } = string.Empty;    // Ngày kỳ so sánh (VD: "28/01")
        public decimal CurrentValue { get; set; }
        public decimal CompareValue { get; set; }
    }

}
