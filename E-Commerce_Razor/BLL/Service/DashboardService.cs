using BLL.DTOs;
using BLL.IService;
using DAL.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
namespace BLL.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IUserRepository _userRepo;
        private readonly IProductRepository _productRepo;

        public DashboardService(
            IOrderRepository orderRepo,
            IUserRepository userRepo,
            IProductRepository productRepo)
        {
            _orderRepo = orderRepo;
            _userRepo = userRepo;
            _productRepo = productRepo;
        }

        public async Task<DashboardStatisticsDTO> GetDashboardStatisticsAsync()
        {
            var totalUsers = await _userRepo.GetTotalUserCountAsync();
            var totalProducts = await _productRepo.GetTotalProductCountAsync();
            var totalOrders = await _orderRepo.GetTotalOrderCountAsync();
            var totalRevenue = await _orderRepo.GetTotalRevenueAsync();

            // ✅ Gọi trực tiếp từ Repository
            var revenueThisMonth = await _orderRepo.GetCompletedRevenueThisMonthAsync();
            var revenueLastMonth = await _orderRepo.GetCompletedRevenueLastMonthAsync();

            var ordersThisMonth = await _orderRepo.GetOrdersThisMonthAsync();
            var ordersLastMonth = await _orderRepo.GetOrdersLastMonthAsync();
            var ordersCountThisMonth = ordersThisMonth.Count;
            var ordersCountLastMonth = ordersLastMonth.Count;

            var newUsersThisMonth = await _userRepo.GetNewUsersCountThisMonthAsync();
            var newUsersLastMonth = await _userRepo.GetNewUsersCountLastMonthAsync();

            // ✅ Service chỉ tính toán business logic
            var revenueGrowth = revenueLastMonth > 0
                ? ((double)(revenueThisMonth - revenueLastMonth) / (double)revenueLastMonth) * 100
                : 0;

            var orderGrowth = ordersCountLastMonth > 0
                ? ((double)(ordersCountThisMonth - ordersCountLastMonth) / ordersCountLastMonth) * 100
                : 0;

            var userGrowth = newUsersLastMonth > 0
                ? ((double)(newUsersThisMonth - newUsersLastMonth) / newUsersLastMonth) * 100
                : 0;

            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            return new DashboardStatisticsDTO
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                RevenueThisMonth = revenueThisMonth,
                OrdersThisMonth = ordersCountThisMonth,
                NewUsersThisMonth = newUsersThisMonth,
                AverageOrderValue = avgOrderValue,
                RevenueGrowthPercent = Math.Round(revenueGrowth, 2),
                OrderGrowthPercent = Math.Round(orderGrowth, 2),
                UserGrowthPercent = Math.Round(userGrowth, 2)
            };
        }

        public async Task<RevenueChartDTO> GetRevenueChartDataAsync(int days = 30)
        {
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-days);

            // ✅ Gọi Repository để lấy data
            var revenueByDay = await _orderRepo.GetDailyRevenueAsync(startDate, endDate);

            var labels = new List<string>();
            var data = new List<decimal>();

            // ✅ Service chỉ format dữ liệu
            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                labels.Add(date.ToString("dd/MM"));

                var dayRevenue = revenueByDay.ContainsKey(date) ? revenueByDay[date] : 0;
                data.Add(dayRevenue);
            }

            return new RevenueChartDTO { Labels = labels, Data = data };
        }

        public async Task<List<TopProductDTO>> GetTopProductsAsync(int top = 5)
        {
            // ✅ Repository xử lý toàn bộ query
            return await _orderRepo.GetTopSellingProductsAsync(top);
        }

        public async Task<List<RecentOrderDTO>> GetRecentOrdersAsync(int count = 10)
        {
            var recentOrders = await _orderRepo.GetRecentOrdersAsync(count);

            // ✅ Mapping DTO có thể giữ ở Service (acceptable)
            return recentOrders.Select(o => new RecentOrderDTO
            {
                OrderId = o.OrderId,
                CustomerName = o.User?.FullName ?? o.User?.UserName ?? "N/A",
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status
            }).ToList();
        }

        public async Task<OrderStatusChartDTO> GetOrderStatusChartAsync()
        {
            var ordersByStatus = await _orderRepo.GetOrdersByStatusAsync();

            return new OrderStatusChartDTO
            {
                Labels = ordersByStatus.Keys.ToList(),
                Data = ordersByStatus.Values.ToList()
            };
        }

        public async Task<UserGrowthChartDTO> GetUserGrowthChartAsync(int months = 6)
        {
            var userGrowth = await _userRepo.GetUserGrowthByMonthAsync(months);

            var labels = new List<string>();
            var data = new List<int>();

            var now = DateTime.Now;
            for (int i = months - 1; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var monthKey = month.Year * 100 + month.Month;

                labels.Add($"{month.Month}/{month.Year}");
                data.Add(userGrowth.ContainsKey(monthKey) ? userGrowth[monthKey] : 0);
            }

            return new UserGrowthChartDTO { Labels = labels, Data = data };
        }

        public async Task<List<ReportResultDTO>> GetReportDataAsync(DateTime startDate, DateTime endDate, string reportType)
        {
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            switch (reportType.ToLower())
            {
                case "revenue":
                    return await _orderRepo.GetRevenueReportAsync(startDate, adjustedEndDate);

                case "products":
                    return await _orderRepo.GetTopSellingProductsAsync(startDate, adjustedEndDate, 10);

                case "categories":
                    return await _orderRepo.GetCategoryRevenueReportAsync(startDate, adjustedEndDate);

                case "payment_methods":
                    return await _orderRepo.GetRevenueByPaymentMethodAsync(startDate, adjustedEndDate);

                default:
                    return new List<ReportResultDTO>();
            }
        }
        public async Task<AdvancedReportResponseDTO> GetAdvancedReportDataAsync(DateTime startDate, DateTime endDate, string reportType, string compareType)
        {
            var response = new AdvancedReportResponseDTO();

            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            TimeSpan duration = adjustedEndDate - startDate;
            int totalDays = (int)duration.TotalDays;

            // 1. TÍNH TOÁN NGÀY SO SÁNH
            DateTime compareStartDate = startDate;
            DateTime compareEndDate = adjustedEndDate;

            switch (compareType)
            {
                case "PreviousPeriod":
                    compareEndDate = startDate.AddTicks(-1);
                    compareStartDate = compareEndDate.AddDays(-totalDays);
                    break;
                case "PreviousMonth":
                    compareStartDate = startDate.AddMonths(-1);
                    compareEndDate = adjustedEndDate.AddMonths(-1);
                    break;
                case "PreviousYear":
                    compareStartDate = startDate.AddYears(-1);
                    compareEndDate = adjustedEndDate.AddYears(-1);
                    break;
            }

            // ====================================================================
            // 2. TÍNH TOÁN KPI CHUNG (LUÔN CHẠY BẤT KỂ ĐANG XEM TAB BÁO CÁO NÀO)
            // ====================================================================
            var currentRevenueData = await _orderRepo.GetRevenueReportAsync(startDate, adjustedEndDate);
            var compareRevenueData = new List<ReportResultDTO>();

            if (compareType != "None")
            {
                compareRevenueData = await _orderRepo.GetRevenueReportAsync(compareStartDate, compareEndDate);
            }

            // Gán dữ liệu cho 3 thẻ KPI trên cùng
            response.TotalRevenue = currentRevenueData.Sum(x => x.Value);
            response.CompareTotalRevenue = compareRevenueData.Sum(x => x.Value);

            if (response.CompareTotalRevenue > 0)
                response.GrowthPercentage = (double)((response.TotalRevenue - response.CompareTotalRevenue) / response.CompareTotalRevenue * 100);
            else if (response.TotalRevenue > 0)
                response.GrowthPercentage = 100;

            // ====================================================================
            // 3. ĐIỀU HƯỚNG TABLE & CHART TÙY THEO LOẠI BÁO CÁO
            // ====================================================================
            switch (reportType.ToLower())
            {
                case "revenue":
                    var currentDict = currentRevenueData.ToDictionary(x => DateTime.ParseExact(x.Label, "dd/MM/yyyy", null).Date, x => x);
                    var compareDict = compareRevenueData.ToDictionary(x => DateTime.ParseExact(x.Label, "dd/MM/yyyy", null).Date, x => x);

                    for (int i = 0; i <= totalDays; i++)
                    {
                        var currentDay = startDate.AddDays(i);
                        var compareDay = compareStartDate.AddDays(i);

                        response.ChartData.Add(new CompareChartPointDTO
                        {
                            Label = currentDay.ToString("dd/MM"),
                            CompareLabel = compareType != "None" ? compareDay.ToString("dd/MM") : "",
                            CurrentValue = currentDict.ContainsKey(currentDay.Date) ? currentDict[currentDay.Date].Value : 0,
                            CompareValue = compareDict.ContainsKey(compareDay.Date) ? compareDict[compareDay.Date].Value : 0
                        });
                    }

                    // Gán dữ liệu cho Bảng KỲ HIỆN TẠI
                    response.TableData = currentRevenueData.OrderByDescending(x => DateTime.ParseExact(x.Label, "dd/MM/yyyy", null)).ToList();

                    // Gán dữ liệu cho Bảng KỲ SO SÁNH (Sắp xếp ngày mới nhất lên đầu)
                    if (compareType != "None")
                    {
                        response.CompareTableData = compareRevenueData.OrderByDescending(x => DateTime.ParseExact(x.Label, "dd/MM/yyyy", null)).ToList();
                    }
                    break;

                case "products":
                    response.TableData = await _orderRepo.GetTopSellingProductsAsync(startDate, adjustedEndDate, 10);
                    if (compareType != "None")
                        response.CompareTableData = await _orderRepo.GetTopSellingProductsAsync(compareStartDate, compareEndDate, 10);
                    break;

                case "categories":
                    response.TableData = await _orderRepo.GetCategoryRevenueReportAsync(startDate, adjustedEndDate);
                    if (compareType != "None")
                        response.CompareTableData = await _orderRepo.GetCategoryRevenueReportAsync(compareStartDate, compareEndDate);
                    break;

                case "payment_methods":
                    response.TableData = await _orderRepo.GetRevenueByPaymentMethodAsync(startDate, adjustedEndDate);
                    if (compareType != "None")
                        response.CompareTableData = await _orderRepo.GetRevenueByPaymentMethodAsync(compareStartDate, compareEndDate);
                    break;
            }

            return response;
        }
    }
}
