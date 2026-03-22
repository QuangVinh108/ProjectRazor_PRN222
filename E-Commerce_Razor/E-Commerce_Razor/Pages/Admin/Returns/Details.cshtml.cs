using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace E_Commerce_Razor.Pages.Admin.Returns
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly IReturnRequestService _returnService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IReturnRequestService returnService, ILogger<DetailsModel> logger)
        {
            _returnService = returnService;
            _logger = logger;
        }

        public ReturnRequestDto? ReturnRequest { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ReturnRequest = await _returnService.GetByIdAsync(id);

            if (ReturnRequest == null)
                return RedirectToPage("./Index");

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var dto = new ProcessReturnRequestDto
                {
                    ReturnRequestId = id,
                    ProcessedByUserId = userId,
                    IsApproved = true
                };

                var (success, msg) = await _returnService.ProcessReturnRequestAsync(dto);

                if (success)
                    TempData["Success"] = msg;
                else
                    TempData["Error"] = msg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Approve Error");
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostRejectAsync(int id, string adminNote)
        {
            try
            {
                var userId = GetCurrentUserId();
                var dto = new ProcessReturnRequestDto
                {
                    ReturnRequestId = id,
                    ProcessedByUserId = userId,
                    IsApproved = false,
                    AdminNote = adminNote
                };

                var (success, msg) = await _returnService.ProcessReturnRequestAsync(dto);

                if (success)
                    TempData["Success"] = msg;
                else
                    TempData["Error"] = msg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reject Error");
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return RedirectToPage("./Details", new { id });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new Exception("Vui lòng đăng nhập lại");
            return int.Parse(userIdClaim);
        }
    }
}
