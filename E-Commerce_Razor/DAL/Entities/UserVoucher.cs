using System;

namespace DAL.Entities
{
    public class UserVoucher
    {
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int VoucherId { get; set; }
        public virtual Voucher Voucher { get; set; } = null!;

        public bool IsUsed { get; set; } = false;
        public DateTime SavedAt { get; set; } = DateTime.Now;
        public DateTime? UsedAt { get; set; }
    }
}
