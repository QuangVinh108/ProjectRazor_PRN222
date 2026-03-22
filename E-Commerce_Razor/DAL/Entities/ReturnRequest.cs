using System;

namespace DAL.Entities;

public partial class ReturnRequest
{
    public int ReturnRequestId { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    /// <summary>Lý do trả hàng: Defective, WrongItem, NotAsDescribed, Other</summary>
    public string Reason { get; set; } = null!;

    /// <summary>Mô tả chi tiết từ khách hàng</summary>
    public string? Description { get; set; }

    /// <summary>Ảnh bằng chứng (lưu đường dẫn, phân cách bởi ;)</summary>
    public string? EvidenceImages { get; set; }

    /// <summary>Trạng thái: Pending, Approved, Rejected</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Lý do từ chối (Admin điền khi reject)</summary>
    public string? AdminNote { get; set; }

    /// <summary>Số tiền hoàn (khi được duyệt)</summary>
    public decimal? RefundAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ProcessedAt { get; set; }

    /// <summary>Admin xử lý</summary>
    public int? ProcessedByUserId { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? ProcessedByUser { get; set; }
}
