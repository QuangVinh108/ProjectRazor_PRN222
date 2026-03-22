-- Thêm cột IsDisputed, DisputeReportedAt cho bảng Shipping (cảnh báo shipper khi khách báo chưa nhận hàng)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shipping') AND name = 'IsDisputed')
BEGIN
    ALTER TABLE Shipping ADD IsDisputed BIT NOT NULL DEFAULT 0;
END
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shipping') AND name = 'DisputeReportedAt')
BEGIN
    ALTER TABLE Shipping ADD DisputeReportedAt DATETIME NULL;
END
