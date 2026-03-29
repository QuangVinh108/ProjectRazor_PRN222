using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Voucher
{
    public int VoucherId { get; set; }

    /// <summary>Mã giảm giá (VD: GIAM50K, SALE20)</summary>
    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>"Fixed" = giảm tiền mặt, "Percent" = giảm theo %</summary>
    public string DiscountType { get; set; } = "Fixed";

    /// <summary>Giá trị giảm (bao nhiêu đồng hoặc bao nhiêu %)</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Giá trị đơn hàng tối thiểu để áp dụng mã</summary>
    public decimal MinOrderValue { get; set; }

    /// <summary>Giảm tối đa (chỉ dùng cho loại Percent)</summary>
    public decimal? MaxDiscount { get; set; }

    /// <summary>Số lần dùng tối đa</summary>
    public int UsageLimit { get; set; }

    /// <summary>Số lần đã dùng</summary>
    public int UsedCount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
}
