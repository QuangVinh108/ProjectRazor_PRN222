using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Order
{
    [Authorize]
    public class CheckoutModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService; // Thêm dòng này để gọi UserService
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(IOrderService orderService, IPaymentService paymentService, IUserService userService, ILogger<CheckoutModel> logger)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _userService = userService; // Gán qua DI
            _logger = logger;
        }

        [BindProperty]
        public CreateOrderDto Input { get; set; } = new CreateOrderDto
        {
            PaymentMethod = "COD",
            Country = "Vietnam"
        };

        public void OnGet() { }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                int userId = GetCurrentUserId();

                // 1. KIỂM TRA TRẠNG THÁI EKYC
                var currentUser = _userService.GetUserById(userId);
                if (currentUser == null || !currentUser.IsIdentityVerified)
                {
                    TempData["ErrorMessage"] = "Bạn cần xác thực danh tính (eKYC) trước khi tiến hành thanh toán mua hàng.";
                    return RedirectToPage("/Account/Ekyc"); // Chuyển hướng nếu chưa eKYC
                }

                // Gán UserId để tạo đơn
                Input.UserId = userId;

                // Validate địa chỉ giao hàng
                if (string.IsNullOrWhiteSpace(Input.ShippingAddress))
                {
                    ModelState.AddModelError("Input.ShippingAddress", "Vui lòng nhập địa chỉ giao hàng");
                    return Page();
                }

                // Tạo đơn hàng + Payment(Pending) + Shipping + xóa giỏ → đều vào DB
                var order = await _orderService.CreateOrderAsync(Input);
                _logger.LogInformation("Order #{OrderId} created, Method={Method}", order.OrderId, Input.PaymentMethod);

                if (Input.PaymentMethod == "VNPAY")
                {
                    // OrderService đã tạo Payment(Pending). Chỉ cần tạo URL và redirect.
                    var paymentDto = new PaymentDto { OrderId = order.OrderId, Amount = order.TotalAmount };
                    var vnpayUrl = _paymentService.CreateVnPayUrl(paymentDto, HttpContext);
                    return Redirect(vnpayUrl);
                }

                // COD / BankTransfer → xem chi tiết đơn hàng
                return RedirectToPage("./Details", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout error");
                TempData["Error"] = ex.Message;
                return Page();
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) throw new Exception("Vui lòng đăng nhập");
            return int.Parse(userIdClaim);
        }
    }
}
