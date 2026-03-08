using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;
        private readonly IOrderRepository _orderRepository;

        public PaymentService(
            IInventoryService inventoryService,
            IConfiguration config,
            ILogger<PaymentService> logger,
            IOrderRepository orderRepository)
        {
            _inventoryService = inventoryService;
            _config = config;
            _logger = logger;
            _orderRepository = orderRepository;
        }

        public async Task CreatePendingPaymentAsync(int orderId, decimal amount)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, includeDetails: true);
            if (order == null) throw new Exception("Order not found");

            if (order.Payment == null)
            {
                order.Payment = new Payment
                {
                    OrderId = orderId,
                    PaymentMethod = "VNPAY",
                    Amount = amount,
                    Status = "Pending",
                    PaidAt = null
                };
                await _orderRepository.UpdateAsync(order);
            }
        }

        public string CreateVnPayUrl(PaymentDto payment, HttpContext context)
        {
            // Chuẩn hóa IP (::1 = IPv6 localhost → 127.0.0.1)
            var ipAddr = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddr == "::1") ipAddr = "127.0.0.1";

            var now = DateTime.Now;

            // ---- Tham số gửi lên VNPay (sorted theo key) ----
            var vnpParams = new SortedDictionary<string, string>
            {
                { "vnp_Amount",     ((long)(payment.Amount * 100)).ToString() },
                { "vnp_Command",    "pay" },
                { "vnp_CreateDate", now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode",   "VND" },
                { "vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss") },
                { "vnp_IpAddr",     ipAddr },
                { "vnp_Locale",     "vn" },
                { "vnp_OrderInfo",  $"Thanh toan don hang {payment.OrderId}" },
                { "vnp_OrderType",  "other" },
                { "vnp_ReturnUrl",  _config["VnPay:ReturnUrl"]! },
                { "vnp_TmnCode",    _config["VnPay:TmnCode"]! },
                { "vnp_TxnRef",     payment.OrderId.ToString() },
                { "vnp_Version",    "2.1.0" }
            };

            // ---- Build chuỗi hash: key=urlEncode(value) nối & (chuẩn VNPay) ----
            var hashBuilder = new StringBuilder();
            foreach (var kv in vnpParams)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    if (hashBuilder.Length > 0) hashBuilder.Append('&');
                    hashBuilder.Append(kv.Key).Append('=').Append(WebUtility.UrlEncode(kv.Value));
                }
            }
            var hashData   = hashBuilder.ToString();
            var secureHash = HmacSHA512(_config["VnPay:HashSecret"]!, hashData);

            // ---- Build query string (giống hashData + thêm vnp_SecureHash) ----
            var url = $"{_config["VnPay:BaseUrl"]}?{hashData}&vnp_SecureHash={secureHash}";

            _logger.LogInformation("VNPay HashData : {H}", hashData);
            _logger.LogInformation("VNPay SecureHash: {S}", secureHash);

            return url;
        }

        public async Task<(bool Success, string Message, int OrderId)> ProcessVnPayReturnAsync(HttpRequest request)
        {
            int orderId = 0;
            try
            {
                var rawQuery = request.QueryString.Value ?? string.Empty;
                if (string.IsNullOrEmpty(rawQuery)) return (false, "Invalid parameters", 0);

                // ---- Parse raw query string (KHÔNG decode) ----
                // Ví dụ: "vnp_Amount=5500000000&vnp_OrderInfo=Thanh+toan+don+hang+12&..."
                // Giữ nguyên encoding như VNPay gửi về → hash sẽ khớp với hash của VNPay.
                var rawPairs = rawQuery.TrimStart('?')
                    .Split('&')
                    .Select(p => p.Split('=', 2))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => p[0], p => p[1]);

                if (!rawPairs.TryGetValue("vnp_SecureHash", out var vnp_SecureHash))
                    return (false, "Thiếu vnp_SecureHash", 0);

                // Decoded values để xử lý logic
                var vnp_TxnRef       = Uri.UnescapeDataString(rawPairs.GetValueOrDefault("vnp_TxnRef", "").Replace("+", " "));
                var vnp_ResponseCode = Uri.UnescapeDataString(rawPairs.GetValueOrDefault("vnp_ResponseCode", "").Replace("+", " "));

                // Parse OrderId
                orderId = int.Parse(vnp_TxnRef.Contains('_') ? vnp_TxnRef.Split('_')[0] : vnp_TxnRef);

                // ---- Build chuỗi verify từ raw values (đúng như VNPay đã hash) ----
                var signData = string.Join("&", rawPairs
                    .Where(kv => kv.Key.StartsWith("vnp_")
                              && kv.Key != "vnp_SecureHash"
                              && kv.Key != "vnp_SecureHashType")
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv => $"{kv.Key}={kv.Value}")); // raw URL-encoded values

                var checkHash = HmacSHA512(_config["VnPay:HashSecret"]!, signData);

                _logger.LogInformation("RETURN signData  : {S}", signData);
                _logger.LogInformation("RETURN checkHash : {C}", checkHash);
                _logger.LogInformation("RETURN vnpayHash : {V}", vnp_SecureHash);

                if (!checkHash.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase))
                    return (false, "Lỗi bảo mật: Chữ ký không hợp lệ", orderId);

                // Lấy đơn hàng
                var order = await _orderRepository.GetByIdAsync(orderId, includeDetails: true);
                if (order == null) return (false, "Đơn hàng không tồn tại", orderId);

                if (vnp_ResponseCode == "00") // Thành công
                {
                    await _inventoryService.DeductInventoryAsync(orderId);

                    order.Status = "Paid";
                    if (order.Payment != null)
                    {
                        order.Payment.Status = "Paid";
                        order.Payment.PaidAt = DateTime.Now;
                    }
                    await _orderRepository.UpdateAsync(order);
                    return (true, "Thanh toán thành công!", orderId);
                }
                else
                {
                    if (order.Payment != null)
                    {
                        order.Payment.Status = "Failed";
                        await _orderRepository.UpdateAsync(order);
                    }
                    return (false, $"Thanh toán thất bại. Mã lỗi: {vnp_ResponseCode}", orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay Return Error");
                return (false, "Lỗi xử lý hệ thống", orderId);
            }
        }

        private static string HmacSHA512(string key, string input)
        {
            var keyBytes   = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(input);
            using var hmac = new HMACSHA512(keyBytes);
            var hashValue  = hmac.ComputeHash(inputBytes);
            return string.Concat(hashValue.Select(b => b.ToString("x2")));
        }
    }
}
