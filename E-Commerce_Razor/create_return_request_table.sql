-- Tạo bảng ReturnRequest cho luồng Trả hàng/Hoàn tiền
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnRequest')
BEGIN
    CREATE TABLE [dbo].[ReturnRequest] (
        [ReturnRequestId] INT IDENTITY(1,1) NOT NULL,
        [OrderId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [Reason] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [EvidenceImages] NVARCHAR(2000) NULL,
        [Status] NVARCHAR(30) NOT NULL DEFAULT 'Pending',
        [AdminNote] NVARCHAR(500) NULL,
        [RefundAmount] DECIMAL(18,2) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        [ProcessedAt] DATETIME2 NULL,
        [ProcessedByUserId] INT NULL,
        CONSTRAINT [PK_ReturnRequest] PRIMARY KEY ([ReturnRequestId]),
        CONSTRAINT [FK_ReturnRequest_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([OrderId]),
        CONSTRAINT [FK_ReturnRequest_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]),
        CONSTRAINT [FK_ReturnRequest_ProcessedBy] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    PRINT 'Table ReturnRequest created successfully!';
END
ELSE
    PRINT 'Table ReturnRequest already exists.';
