using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Payment
{
    [Authorize]
    public class PayModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PayModel> _logger;

        public PayModel(IOrderService orderService, IPaymentService paymentService, ILogger<PayModel> logger)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _logger = logger;
        }

        // POST /Payment/Pay  (id = OrderId)
        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Lấy thông tin đơn hàng
                var order = await _orderService.GetOrderByIdAsync(id, userId);
                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToPage("/Order/Index");
                }

                // Nếu đã thanh toán rồi thì không cho thanh toán lại
                if (order.Payment != null && order.Payment.Status == "Paid")
                {
                    TempData["Error"] = "Đơn hàng này đã được thanh toán.";
                    return RedirectToPage("/Order/Details", new { id });
                }

                // Tạo hoặc cập nhật bản ghi Payment về trạng thái Pending
                await _paymentService.CreatePendingPaymentAsync(id, order.TotalAmount);

                // Tạo URL thanh toán VNPay
                var paymentDto = new PaymentDto
                {
                    OrderId = id,
                    Amount = order.TotalAmount
                };

                var vnpayUrl = _paymentService.CreateVnPayUrl(paymentDto, HttpContext);
                _logger.LogInformation("Redirecting order {OrderId} to VNPay: {Url}", id, vnpayUrl);

                return Redirect(vnpayUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment/Pay error for order {OrderId}", id);
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại.";
                return RedirectToPage("/Order/Details", new { id });
            }
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
