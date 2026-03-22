using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Order
{
    [Authorize]
    public class ReturnRequestModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IReturnRequestService _returnService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReturnRequestModel> _logger;

        public ReturnRequestModel(
            IOrderService orderService,
            IReturnRequestService returnService,
            IWebHostEnvironment env,
            ILogger<ReturnRequestModel> logger)
        {
            _orderService = orderService;
            _returnService = returnService;
            _env = env;
            _logger = logger;
        }

        public OrderDto? Order { get; set; }

        [BindProperty]
        public string Reason { get; set; } = "";

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public List<IFormFile>? EvidenceFiles { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = GetCurrentUserId();
            Order = await _orderService.GetOrderByIdAsync(id, userId);

            if (Order == null)
                return RedirectToPage("./Index");

            var validStatuses = new[] { "Paid", "Shipped", "Delivered", "Hoàn thành" };
            if (!validStatuses.Contains(Order.Status))
            {
                TempData["Error"] = "Chỉ có thể yêu cầu Trả hàng / Hoàn tiền đối với đơn hàng đã thanh toán hoặc đã giao.";
                return RedirectToPage("./Details", new { id });
            }

            // Kiểm tra đã có yêu cầu trả hàng chưa
            var hasRequest = await _returnService.HasReturnRequestAsync(id);
            if (hasRequest)
            {
                TempData["Error"] = "Đơn hàng này đã có yêu cầu trả hàng/hoàn tiền đang xử lý.";
                return RedirectToPage("./Details", new { id });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = GetCurrentUserId();

            // Upload ảnh bằng chứng
            var imageUrls = new List<string>();
            if (EvidenceFiles != null && EvidenceFiles.Any())
            {
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "returns");
                Directory.CreateDirectory(uploadPath);

                foreach (var file in EvidenceFiles)
                {
                    if (file.Length > 0 && file.Length <= 5 * 1024 * 1024) // Max 5MB mỗi file
                    {
                        // Validate file type
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var ext = Path.GetExtension(file.FileName).ToLower();
                        if (!allowedExtensions.Contains(ext))
                            continue;

                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadPath, fileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        imageUrls.Add($"/uploads/returns/{fileName}");
                    }
                }
            }

            var dto = new CreateReturnRequestDto
            {
                OrderId = id,
                UserId = userId,
                Reason = Reason,
                Description = Description,
                EvidenceImages = imageUrls
            };

            var (success, message) = await _returnService.CreateReturnRequestAsync(dto);

            if (success)
            {
                TempData["Success"] = message;
                return RedirectToPage("./Details", new { id });
            }
            else
            {
                TempData["Error"] = message;
                // Reload order data cho page
                Order = await _orderService.GetOrderByIdAsync(id, userId);
                return Page();
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
