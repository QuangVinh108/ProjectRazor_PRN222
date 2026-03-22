using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Order
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IOrderService orderService, ILogger<DetailsModel> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        public OrderDto? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = GetCurrentUserId();
            Order = await _orderService.GetOrderByIdAsync(id, userId);

            if (Order == null)
                return RedirectToPage("./Index");

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmReceivedAsync(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var ok = await _orderService.ConfirmReceivedByCustomerAsync(id, userId);
                if (ok)
                    TempData["Success"] = "Đã xác nhận nhận hàng. Đơn hàng đã hoàn thành.";
                else
                    TempData["Error"] = "Không thể xác nhận.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirm received error");
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostReportNotReceivedAsync(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var orderBefore = await _orderService.GetOrderByIdAsync(id, userId);
                var amount = orderBefore?.Payment?.Amount ?? orderBefore?.TotalAmount ?? 0m;

                var ok = await _orderService.ReportNotReceivedByCustomerAsync(id, userId);
                if (ok)
                {
                    TempData["Success"] = $"Đã ghi nhận. Số tiền {amount:N0} ₫ sẽ được hoàn lại trong vòng 3–5 ngày làm việc. Shipper đã được cảnh báo.";
                }
                else
                    TempData["Error"] = "Không thể xử lý.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report not received error");
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                // Lấy thông tin đơn trước khi hủy để biết trạng thái thanh toán
                var orderBefore = await _orderService.GetOrderByIdAsync(id, userId);
                var wasPaid = orderBefore?.Payment?.Status == "Paid";
                var paymentMethod = orderBefore?.Payment?.PaymentMethod ?? "phương thức thanh toán đã sử dụng";
                var amount = orderBefore?.Payment?.Amount ?? orderBefore?.TotalAmount ?? 0m;

                var result = await _orderService.CancelOrderAsync(id, userId);

                if (result)
                {
                    if (wasPaid)
                    {
                        TempData["Success"] =
                            $"Đơn hàng đã được hủy thành công. Số tiền {amount:N0} ₫ sẽ được hoàn lại về {paymentMethod} của bạn trong vòng 3–5 ngày làm việc.";
                    }
                    else
                    {
                        TempData["Success"] = "Hủy đơn hàng thành công.";
                    }
                }
                else
                {
                    TempData["Error"] = "Không thể hủy đơn hàng.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel order error");
                TempData["Error"] = ex.Message;
            }

            return RedirectToPage("./Details", new { id });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new Exception("Vui lòng đăng nhập");
            return int.Parse(userIdClaim);
        }
    }
}
