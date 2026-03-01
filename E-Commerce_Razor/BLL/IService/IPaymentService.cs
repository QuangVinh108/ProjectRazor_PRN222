using BLL.DTOs;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IPaymentService
    {
        string CreateVnPayUrl(PaymentDto payment, HttpContext context);
        Task<(bool Success, string Message, int OrderId)> ProcessVnPayReturnAsync(HttpRequest request);
        Task CreatePendingPaymentAsync(int orderId, decimal amount);
    }
}
