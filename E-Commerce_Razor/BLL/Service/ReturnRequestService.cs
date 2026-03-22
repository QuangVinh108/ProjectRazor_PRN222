using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.Extensions.Logging;

namespace BLL.Service
{
    public class ReturnRequestService : IReturnRequestService
    {
        private readonly IReturnRequestRepository _returnRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IInventoryService _inventoryService;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<ReturnRequestService> _logger;

        // Thời hạn cho phép trả hàng (7 ngày kể từ ngày giao)
        private const int RETURN_DEADLINE_DAYS = 7;

        public ReturnRequestService(
            IReturnRequestRepository returnRepo,
            IOrderRepository orderRepo,
            IInventoryService inventoryService,
            IEmailService emailService,
            IUserRepository userRepo,
            ILogger<ReturnRequestService> logger)
        {
            _returnRepo = returnRepo;
            _orderRepo = orderRepo;
            _inventoryService = inventoryService;
            _emailService = emailService;
            _userRepo = userRepo;
            _logger = logger;
        }

        /// <summary>
        /// LUỒNG TẠO YÊU CẦU TRẢ HÀNG (Customer)
        /// Bước 1: Validate - Đơn phải ở trạng thái "Delivered" hoặc "Hoàn thành"
        /// Bước 2: Validate - Kiểm tra thời hạn trả hàng (7 ngày)
        /// Bước 3: Validate - Chưa có yêu cầu trả hàng trước đó
        /// Bước 4: Validate - Kiểm tra quyền sở hữu đơn hàng
        /// Bước 5: Validate - Phải có lý do + ảnh bằng chứng
        /// Bước 6: Tạo yêu cầu trả hàng → Trạng thái "Pending"
        /// Bước 7: Cập nhật trạng thái đơn hàng → "ReturnRequested"
        /// </summary>
        public async Task<(bool Success, string Message)> CreateReturnRequestAsync(CreateReturnRequestDto dto)
        {
            try
            {
                // ── Bước 1: Lấy đơn hàng và validate trạng thái ──
                var order = await _orderRepo.GetByIdAsync(dto.OrderId, includeDetails: true);
                if (order == null)
                    return (false, "Đơn hàng không tồn tại.");

                // ── Bước 2: Kiểm tra quyền sở hữu ──
                if (order.UserId != dto.UserId)
                    return (false, "Bạn không có quyền thao tác trên đơn hàng này.");

                // ── Bước 3: Validate trạng thái đơn hàng ──
                var validStatuses = new[] { "Paid", "Shipped", "Delivered", "Hoàn thành" };
                if (!validStatuses.Contains(order.Status))
                    return (false, "Chỉ có thể yêu cầu Hoàn tiền/Trả hàng với đơn đã thanh toán hoặc đã giao.");

                // ── Bước 4: Kiểm tra thời hạn trả hàng ──
                var deliveryDate = order.Shipping?.DeliveryDate;
                if (deliveryDate.HasValue)
                {
                    var daysSinceDelivery = (DateTime.Now - deliveryDate.Value).TotalDays;
                    if (daysSinceDelivery > RETURN_DEADLINE_DAYS)
                        return (false, $"Đã quá thời hạn trả hàng ({RETURN_DEADLINE_DAYS} ngày kể từ ngày nhận).");
                }

                // ── Bước 5: Kiểm tra đã có yêu cầu trả hàng trước đó chưa ──
                var existingRequest = await _returnRepo.ExistsByOrderIdAsync(dto.OrderId);
                if (existingRequest)
                    return (false, "Đơn hàng này đã có yêu cầu trả hàng/hoàn tiền đang xử lý.");

                // ── Bước 6: Validate lý do ──
                var validReasons = new[] { "Defective", "WrongItem", "NotAsDescribed", "Other" };
                if (!validReasons.Contains(dto.Reason))
                    return (false, "Lý do trả hàng không hợp lệ.");

                // ── Bước 7: Validate ảnh bằng chứng ──
                if (dto.EvidenceImages == null || !dto.EvidenceImages.Any())
                    return (false, "Vui lòng upload ít nhất 1 ảnh bằng chứng.");

                // ── Bước 8: Tạo yêu cầu trả hàng ──
                var returnRequest = new ReturnRequest
                {
                    OrderId = dto.OrderId,
                    UserId = dto.UserId,
                    Reason = dto.Reason,
                    Description = dto.Description,
                    EvidenceImages = string.Join(";", dto.EvidenceImages),
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    RefundAmount = order.TotalAmount // Mặc định hoàn toàn bộ
                };

                await _returnRepo.CreateAsync(returnRequest);

                // ── Bước 9: Cập nhật trạng thái đơn hàng ──
                order.Status = "ReturnRequested";
                await _orderRepo.UpdateAsync(order);

                _logger.LogInformation("Return request created for Order #{OrderId} by User #{UserId}", dto.OrderId, dto.UserId);

                return (true, "Yêu cầu trả hàng/hoàn tiền đã được gửi thành công. Vui lòng chờ Admin xử lý.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating return request for Order #{OrderId}", dto.OrderId);
                return (false, "Có lỗi xảy ra khi tạo yêu cầu: " + ex.Message);
            }
        }

        /// <summary>
        /// LUỒNG ADMIN XỬ LÝ YÊU CẦU TRẢ HÀNG
        /// Bước 1: Validate yêu cầu tồn tại + đang ở trạng thái "Pending"
        /// Bước 2: Nếu DUYỆT:
        ///    - Hoàn kho (Inventory restore)
        ///    - Cập nhật Payment → "Refunded"
        ///    - Cập nhật Order → "Returned"
        ///    - Gửi email thông báo cho Customer
        /// Bước 3: Nếu TỪ CHỐI:
        ///    - Cập nhật trạng thái → "Rejected"
        ///    - Gửi email kèm lý do từ chối
        ///    - Khôi phục trạng thái đơn hàng
        /// </summary>
        public async Task<(bool Success, string Message)> ProcessReturnRequestAsync(ProcessReturnRequestDto dto)
        {
            try
            {
                // ── Bước 1: Validate ──
                var returnRequest = await _returnRepo.GetByIdAsync(dto.ReturnRequestId);
                if (returnRequest == null)
                    return (false, "Yêu cầu trả hàng không tồn tại.");

                if (returnRequest.Status != "Pending")
                    return (false, "Yêu cầu này đã được xử lý trước đó.");

                var order = returnRequest.Order;
                if (order == null)
                    return (false, "Đơn hàng liên quan không tồn tại.");

                // Lấy thông tin customer để gửi email
                var customer = returnRequest.User;
                var customerEmail = customer?.Email;

                if (dto.IsApproved)
                {
                    // ══════════════════════════════════════
                    // DUYỆT YÊU CẦU TRẢ HÀNG
                    // ══════════════════════════════════════

                    // Bước 2a: Hoàn kho
                    await _inventoryService.RestoreInventoryAsync(order);
                    _logger.LogInformation("Inventory restored for Order #{OrderId}", order.OrderId);

                    // Bước 2b: Cập nhật Payment → Refunded
                    if (order.Payment != null)
                    {
                        order.Payment.Status = "Refunded";
                    }

                    // Bước 2c: Cập nhật Order status
                    order.Status = "Returned";
                    await _orderRepo.UpdateAsync(order);

                    // Bước 2d: Cập nhật ReturnRequest
                    returnRequest.Status = "Approved";
                    returnRequest.AdminNote = dto.AdminNote ?? "Yêu cầu trả hàng đã được chấp nhận.";
                    returnRequest.ProcessedAt = DateTime.Now;
                    returnRequest.ProcessedByUserId = dto.ProcessedByUserId;
                    await _returnRepo.UpdateAsync(returnRequest);

                    // Bước 2e: Gửi email thông báo cho Customer
                    if (!string.IsNullOrEmpty(customerEmail))
                    {
                        try
                        {
                            var refundAmount = returnRequest.RefundAmount ?? order.TotalAmount;
                            await _emailService.SendOtpEmailAsync(customerEmail,
                                $"YÊU CẦU TRẢ HÀNG ĐÃ ĐƯỢC DUYỆT - Đơn #{order.OrderId}. " +
                                $"Số tiền {refundAmount:N0} VNĐ sẽ được hoàn lại trong 3-5 ngày làm việc.");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "Failed to send approval email to {Email}", customerEmail);
                            // Không throw - vẫn duyệt thành công dù gửi mail lỗi
                        }
                    }

                    _logger.LogInformation("Return request #{ReturnId} APPROVED for Order #{OrderId}",
                        dto.ReturnRequestId, order.OrderId);

                    return (true, $"Đã duyệt yêu cầu trả hàng. Số tiền {(returnRequest.RefundAmount ?? order.TotalAmount):N0} ₫ sẽ được hoàn cho khách hàng.");
                }
                else
                {
                    // ══════════════════════════════════════
                    // TỪ CHỐI YÊU CẦU TRẢ HÀNG
                    // ══════════════════════════════════════

                    if (string.IsNullOrWhiteSpace(dto.AdminNote))
                        return (false, "Vui lòng nhập lý do từ chối.");

                    // Bước 3a: Cập nhật ReturnRequest
                    returnRequest.Status = "Rejected";
                    returnRequest.AdminNote = dto.AdminNote;
                    returnRequest.ProcessedAt = DateTime.Now;
                    returnRequest.ProcessedByUserId = dto.ProcessedByUserId;
                    await _returnRepo.UpdateAsync(returnRequest);

                    // Bước 3b: Khôi phục trạng thái đơn hàng về "Delivered"
                    order.Status = "Delivered";
                    await _orderRepo.UpdateAsync(order);

                    // Bước 3c: Gửi email từ chối
                    if (!string.IsNullOrEmpty(customerEmail))
                    {
                        try
                        {
                            await _emailService.SendOtpEmailAsync(customerEmail,
                                $"YÊU CẦU TRẢ HÀNG BỊ TỪ CHỐI - Đơn #{order.OrderId}. " +
                                $"Lý do: {dto.AdminNote}");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "Failed to send rejection email to {Email}", customerEmail);
                        }
                    }

                    _logger.LogInformation("Return request #{ReturnId} REJECTED for Order #{OrderId}",
                        dto.ReturnRequestId, order.OrderId);

                    return (true, "Đã từ chối yêu cầu trả hàng.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing return request #{ReturnId}", dto.ReturnRequestId);
                return (false, "Có lỗi xảy ra khi xử lý: " + ex.Message);
            }
        }

        public async Task<List<ReturnRequestDto>> GetByUserIdAsync(int userId)
        {
            var requests = await _returnRepo.GetByUserIdAsync(userId);
            return requests.Select(MapToDto).ToList();
        }

        public async Task<List<ReturnRequestDto>> GetAllPendingAsync()
        {
            var requests = await _returnRepo.GetAllPendingAsync();
            return requests.Select(MapToDto).ToList();
        }

        public async Task<List<ReturnRequestDto>> GetAllAsync()
        {
            var requests = await _returnRepo.GetAllAsync();
            return requests.Select(MapToDto).ToList();
        }

        public async Task<ReturnRequestDto?> GetByIdAsync(int id)
        {
            var request = await _returnRepo.GetByIdAsync(id);
            return request == null ? null : MapToDto(request);
        }

        public async Task<bool> HasReturnRequestAsync(int orderId)
        {
            return await _returnRepo.ExistsByOrderIdAsync(orderId);
        }

        public async Task<ReturnRequestDto?> GetByOrderIdAsync(int orderId)
        {
            var request = await _returnRepo.GetByOrderIdAsync(orderId);
            return request == null ? null : MapToDto(request);
        }

        // ── Helper Map ──
        private ReturnRequestDto MapToDto(ReturnRequest r)
        {
            return new ReturnRequestDto
            {
                ReturnRequestId = r.ReturnRequestId,
                OrderId = r.OrderId,
                UserId = r.UserId,
                CustomerName = r.User?.FullName ?? r.User?.UserName ?? "",
                Reason = r.Reason,
                Description = r.Description,
                EvidenceImageUrls = string.IsNullOrEmpty(r.EvidenceImages)
                    ? new List<string>()
                    : r.EvidenceImages.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Status = r.Status,
                AdminNote = r.AdminNote,
                RefundAmount = r.RefundAmount,
                OrderTotalAmount = r.Order?.TotalAmount ?? 0,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt,
                ProcessedByName = r.ProcessedByUser?.FullName ?? r.ProcessedByUser?.UserName,
                OrderItems = r.Order?.OrderItems?.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.ProductName ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList() ?? new List<OrderItemDto>()
            };
        }
    }
}
