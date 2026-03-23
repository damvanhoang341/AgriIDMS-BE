-- Chạy thủ công trên SQL Server nếu chưa dùng EF migrate (thêm cột URL ảnh QR do FE upload Cloudinary).

IF COL_LENGTH('dbo.Lots', 'QrImageUrl') IS NULL
    ALTER TABLE dbo.Lots ADD QrImageUrl NVARCHAR(500) NULL;

IF COL_LENGTH('dbo.Boxes', 'QrImageUrl') IS NULL
    ALTER TABLE dbo.Boxes ADD QrImageUrl NVARCHAR(500) NULL;

IF COL_LENGTH('dbo.Slots', 'QrImageUrl') IS NULL
    ALTER TABLE dbo.Slots ADD QrImageUrl NVARCHAR(500) NULL;
