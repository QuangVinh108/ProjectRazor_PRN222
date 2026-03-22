-- 1. Tạo bảng Vouchers
CREATE TABLE Vouchers (
    VoucherId       INT IDENTITY(1,1) PRIMARY KEY,
    Code            NVARCHAR(50)        NOT NULL,   -- Mã giảm giá (e.g. GIAM20K)
    Description     NVARCHAR(255)       NULL,       -- Mô tả
    DiscountType    NVARCHAR(20)        NOT NULL DEFAULT 'Fixed', -- 'Fixed' hoặc 'Percent'
    DiscountValue   DECIMAL(18, 2)      NOT NULL,   -- Số tiền hoặc % giảm
    MinOrderValue   DECIMAL(18, 2)      NOT NULL DEFAULT 0, -- Đơn tối thiểu để dùng
    MaxDiscount     DECIMAL(18, 2)      NULL,       -- Giảm tối đa (dành cho Percent)
    UsageLimit      INT                 NOT NULL DEFAULT 1, -- Số lần dùng tối đa
    UsedCount       INT                 NOT NULL DEFAULT 0, -- Số lần đã dùng
    StartDate       DATETIME            NOT NULL,
    EndDate         DATETIME            NOT NULL,
    IsActive        BIT                 NOT NULL DEFAULT 1,
    CreatedAt       DATETIME            NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT UQ_Voucher_Code UNIQUE (Code),
    CONSTRAINT CK_Voucher_DiscountType CHECK (DiscountType IN ('Fixed', 'Percent')),
    CONSTRAINT CK_Voucher_DiscountValue CHECK (DiscountValue > 0),
    CONSTRAINT CK_Voucher_Dates CHECK (EndDate > StartDate)
);

-- 2. Thêm cột VoucherId và DiscountAmount vào bảng Orders
ALTER TABLE Orders
    ADD VoucherId      INT             NULL,
        DiscountAmount DECIMAL(18, 2)  NOT NULL DEFAULT 0;

-- 3. Thêm Foreign Key từ Orders -> Vouchers
ALTER TABLE Orders
    ADD CONSTRAINT FK_Orders_Vouchers
    FOREIGN KEY (VoucherId) REFERENCES Vouchers(VoucherId);

-- 4. Seed 1 vài voucher mẫu để test
INSERT INTO Vouchers (Code, Description, DiscountType, DiscountValue, MinOrderValue, MaxDiscount, UsageLimit, StartDate, EndDate)
VALUES
    ('GIAM50K',  N'Giảm 50.000đ cho đơn từ 200k',     'Fixed',   50000, 200000, NULL,  100, '2026-01-01', '2026-12-31'),
    ('SALE20',   N'Giảm 20% tối đa 100k',              'Percent', 20,    100000, 100000, 50, '2026-01-01', '2026-12-31'),
    ('WELCOME',  N'Chào mừng thành viên mới - Giảm 30k','Fixed',  30000, 0,      NULL,  200, '2026-01-01', '2026-12-31'),
    -- 1. Giảm 5% cho đơn từ 500k, tối đa 150k
    ('TECHSALE', N'Giảm 5% tối đa 150k cho đơn hàng từ 500.000đ', 'Percent', 5, 500000, 150000, 100, SYSDATETIME(), DATEADD(day, 30, SYSDATETIME())),
    
    -- 2. Giảm 10% cho đơn từ 2 triệu, tối đa 300k
    ('PAYDAY10', N'Giảm 10% dịp lương về, áp dụng đơn từ 2 Triệu', 'Percent', 10, 2000000, 300000, 50, SYSDATETIME(), DATEADD(day, 15, SYSDATETIME())),
    
    -- 3. Giảm thẳng 100k cho đơn từ 1.5 triệu
    ('GIAM100K', N'Tặng 100.000đ tiền mặt cho đơn từ 1.5 Triệu', 'Fixed', 100000, 1500000, NULL, 200, SYSDATETIME(), DATEADD(day, 60, SYSDATETIME())),
    
    -- 4. Giảm thẳng 250k cho đơn từ 5 triệu (mua laptop/đồ điện tử)
    ('VIP250', N'Đặc quyền mua sắm đồ điện tử cao cấp, giảm thẳng 250k', 'Fixed', 250000, 5000000, NULL, 30, SYSDATETIME(), DATEADD(day, 60, SYSDATETIME())),
    
    -- 5. Deal sốc cuối tuần: Giảm 20% tối đa 50k không cần tối thiểu
    ('WEEKEND20', N'Flash Sale cuối tuần! Giảm giá chớp nhoáng 20%', 'Percent', 20, 0, 50000, 500, SYSDATETIME(), DATEADD(day, 3, SYSDATETIME())),
    
    -- 6. Khách hàng mới: Giảm 15% tối đa 100k cho đơn từ 200k
    ('NEWFRIEND15', N'Chào bạn mới tới E-Shop, giảm 15% cho đơn đầu tiên', 'Percent', 15, 200000, 100000, 1000, SYSDATETIME(), DATEADD(day, 90, SYSDATETIME())),
    
    -- 7. Giảm 400k cho đơn siêu khủng từ 10 triệu
    ('APPLEPRO', N'Dành riêng cho các tín đồ công nghệ, trợ giá tới 400k', 'Fixed', 400000, 10000000, NULL, 20, SYSDATETIME(), DATEADD(day, 120, SYSDATETIME())),
    
    -- 8. Sale đồng giá: Giảm 50% tối đa 100k
    ('HALFPRICE', N'Trợ giá siêu đỉnh 50% (tối đa 100k) cho đơn từ 150k', 'Percent', 50, 150000, 100000, 100, SYSDATETIME(), DATEADD(day, 7, SYSDATETIME())),
    
    -- 9. Mã tri ân: Giảm 50k không cần điều kiện
    ('THANKYOU50', N'Cảm ơn bạn đã đồng hành. Tặng ngay 50k', 'Fixed', 50000, 0, NULL, 300, SYSDATETIME(), DATEADD(day, 30, SYSDATETIME())),
    
    -- 10. Lễ hội mua sắm: Giảm 8% tối đa 800k cho đơn từ 5 triệu
    ('MEGASALE8', N'Bùng nổ mua sắm, siêu bão giảm 8% (Max 800k)', 'Percent', 8, 5000000, 800000, 150, SYSDATETIME(), DATEADD(day, 45, SYSDATETIME()));


