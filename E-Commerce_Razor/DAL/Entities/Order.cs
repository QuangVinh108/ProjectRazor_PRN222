using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; }

    /// <summary>FK tới Voucher đã áp dụng (null nếu không dùng mã)</summary>
    public int? VoucherId { get; set; }

    /// <summary>Số tiền được giảm từ voucher</summary>
    public decimal DiscountAmount { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Payment? Payment { get; set; }

    public virtual Shipping? Shipping { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }
}
