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
        private readonly IVoucherService _voucherService;
        private readonly ICartService _cartService;

        
        private readonly IUserService _userService; // Thêm dòng này để gọi UserService
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(IOrderService orderService, IPaymentService paymentService,
                             IVoucherService voucherService, ICartService cartService, ILogger<CheckoutModel> logger, IUserService userService)
        {
            _orderService   = orderService;
            _paymentService = paymentService;
            _voucherService = voucherService;
            _cartService    = cartService;
            _userService = userService; // Gán qua DI
            _logger = logger;
        }

        [BindProperty]
        public CreateOrderDto Input { get; set; } = new CreateOrderDto
        {
            PaymentMethod = "COD",
            Country       = "Vietnam"
        };

        public List<VoucherDto> AvailableVouchers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = GetCurrentUserId();
            var cart = _cartService.GetCart(userId);
            if (cart != null && cart.CartItems.Any())
            {
                var totalAmount = cart.CartItems.Sum(c => c.Quantity * c.UnitPrice);
                AvailableVouchers = await _voucherService.GetApplicableVouchersAsync(userId, totalAmount);
            }
        }

        /// <summary>
        /// AJAX handler: Preview số tiền giảm khi người dùng nhập/chọn mã voucher.
        /// POST /Order/Checkout?handler=ValidateVoucher  (body: "MÃCODE")
        /// </summary>
        public async Task<IActionResult> OnPostValidateVoucherAsync([FromBody] string code)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = _cartService.GetCart(userId);
                
                if (cart == null || !cart.CartItems.Any()) 
                    return new JsonResult(new { isSuccess = false, errorMessage = "Giỏ hàng trống" });

                var totalAmount = cart.CartItems.Sum(c => c.Quantity * c.UnitPrice);
                var result = await _voucherService.ApplyVoucherAsync(userId, code, totalAmount);
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { isSuccess = false, errorMessage = "Lỗi xác thực: " + ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Load lại dropdown nếu lỗi
                return Page();
            }

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

                if (string.IsNullOrWhiteSpace(Input.ShippingAddress))
                {
                    ModelState.AddModelError("Input.ShippingAddress", "Vui lòng nhập địa chỉ giao hàng");
                    await OnGetAsync();
                    return Page();
                }

                var order = await _orderService.CreateOrderAsync(Input);
                _logger.LogInformation("Order #{OrderId} created, Method={Method}, Voucher={Voucher}",
                    order.OrderId, Input.PaymentMethod, Input.VoucherCode ?? "none");

                if (Input.PaymentMethod == "VNPAY")
                {
                    var paymentDto = new PaymentDto { OrderId = order.OrderId, Amount = order.TotalAmount };
                    var vnpayUrl   = _paymentService.CreateVnPayUrl(paymentDto, HttpContext);
                    return Redirect(vnpayUrl);
                }

                return RedirectToPage("./Details", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout error");
                TempData["Error"] = ex.Message;
                await OnGetAsync();
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
