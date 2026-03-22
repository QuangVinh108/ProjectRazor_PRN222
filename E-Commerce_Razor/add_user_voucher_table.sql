-- Tạo bảng trung gian UserVouchers để lưu trữ Voucher mà mỗi User đã "Lưu" (Claim)
CREATE TABLE UserVouchers (
    UserId INT NOT NULL,
    VoucherId INT NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    SavedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UsedAt DATETIME2 NULL,
    CONSTRAINT PK_UserVouchers PRIMARY KEY (UserId, VoucherId),
    CONSTRAINT FK_UserVouchers_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserVouchers_Vouchers FOREIGN KEY (VoucherId) REFERENCES Vouchers(VoucherId) ON DELETE CASCADE
);