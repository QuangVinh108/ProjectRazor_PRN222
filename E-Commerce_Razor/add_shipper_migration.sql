-- =============================================
-- Migration: Add Shipper Role & ShipperId to Shipping
-- Date: 2026-03-08
-- =============================================

-- 1. Thêm Role "Shipper" (chỉ chạy nếu chưa tồn tại)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Shipper')
BEGIN
    INSERT INTO Roles (RoleName, Description, IsActive)
    VALUES ('Shipper', N'Nhân viên giao hàng', 1);
    PRINT 'Role Shipper inserted.';
END
ELSE
    PRINT 'Role Shipper already exists, skipped.';

-- 2. Thêm cột ShipperId vào bảng Shipping (chỉ nếu chưa có)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Shipping' AND COLUMN_NAME = 'ShipperId'
)
BEGIN
    ALTER TABLE Shipping
    ADD ShipperId INT NULL;
    PRINT 'Column ShipperId added to Shipping.';
END
ELSE
    PRINT 'Column ShipperId already exists, skipped.';

-- 3. Thêm Foreign Key constraint
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Shipping_Shipper'
)
BEGIN
    ALTER TABLE Shipping
    ADD CONSTRAINT FK_Shipping_Shipper
    FOREIGN KEY (ShipperId) REFERENCES Users(UserId)
    ON DELETE SET NULL;
    PRINT 'FK_Shipping_Shipper constraint added.';
END
ELSE
    PRINT 'FK_Shipping_Shipper already exists, skipped.';
