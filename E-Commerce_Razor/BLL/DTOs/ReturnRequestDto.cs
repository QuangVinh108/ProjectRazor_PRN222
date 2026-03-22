using System;
using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs
{
    /// <summary>DTO hiển thị yêu cầu trả hàng</summary>
    public class ReturnRequestDto
    {
        public int ReturnRequestId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = "";
        public string Reason { get; set; } = "";
        public string ReasonDisplay => Reason switch
        {
            "Defective" => "Sản phẩm bị lỗi/hỏng",
            "WrongItem" => "Giao sai sản phẩm",
            "NotAsDescribed" => "Không đúng mô tả",
            "Other" => "Lý do khác",
            _ => Reason
        };
        public string? Description { get; set; }
        public List<string> EvidenceImageUrls { get; set; } = new();
        public string Status { get; set; } = "Pending";
        public string StatusDisplay => Status switch
        {
            "Pending" => "Chờ xử lý",
            "Approved" => "Đã duyệt",
            "Rejected" => "Đã từ chối",
            _ => Status
        };
        public string? AdminNote { get; set; }
        public decimal? RefundAmount { get; set; }
        public decimal OrderTotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedByName { get; set; }

        // Thông tin đơn hàng liên quan
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    /// <summary>DTO tạo yêu cầu trả hàng từ Customer</summary>
    public class CreateReturnRequestDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lý do trả hàng")]
        public string Reason { get; set; } = "";

        [MaxLength(1000, ErrorMessage = "Mô tả không quá 1000 ký tự")]
        public string? Description { get; set; }

        /// <summary>Danh sách ảnh bằng chứng (đường dẫn file)</summary>
        public List<string> EvidenceImages { get; set; } = new();
    }

    /// <summary>DTO Admin xử lý yêu cầu trả hàng</summary>
    public class ProcessReturnRequestDto
    {
        public int ReturnRequestId { get; set; }
        public int ProcessedByUserId { get; set; }

        /// <summary>true = Duyệt, false = Từ chối</summary>
        public bool IsApproved { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }
    }
}
