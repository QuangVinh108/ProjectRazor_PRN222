using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs;

public class VoucherDto
{
    public int VoucherId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Fixed";
    public decimal DiscountValue { get; set; }
    public decimal MinOrderValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateVoucherDto
{
    [Required(ErrorMessage = "Mã voucher không được để trống")]
    [StringLength(50, ErrorMessage = "Mã không quá 50 ký tự")]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã chỉ chứa chữ IN HOA và số")]
    public string Code { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Loại giảm giá không được để trống")]
    public string DiscountType { get; set; } = "Fixed"; // "Fixed" hoặc "Percent"

    [Required(ErrorMessage = "Giá trị giảm không được để trống")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinOrderValue { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal? MaxDiscount { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượt dùng tối thiểu là 1")]
    public int UsageLimit { get; set; } = 1;

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Kết quả sau khi áp dụng voucher</summary>
public class ApplyVoucherResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public VoucherDto? Voucher { get; set; }
}
