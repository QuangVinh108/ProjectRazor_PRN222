using BLL.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace E_Commerce_Razor.Pages.Payment
{
    public class VnPayReturnModel : PageModel
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<VnPayReturnModel> _logger;

        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int OrderId { get; set; }

        public VnPayReturnModel(IPaymentService paymentService, ILogger<VnPayReturnModel> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = await _paymentService.ProcessVnPayReturnAsync(Request);
            IsSuccess = result.Success;
            Message = result.Message;
            OrderId = result.OrderId;

            _logger.LogInformation("VNPay Return: Success={Success}, OrderId={OrderId}, Message={Message}",
                result.Success, result.OrderId, result.Message);

            return Page();
        }
    }
}
