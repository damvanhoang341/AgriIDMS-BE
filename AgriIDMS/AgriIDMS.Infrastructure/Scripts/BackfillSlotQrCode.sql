-- Gán mã QR mặc định cho slot cũ (khớp payload ảnh QR: SLOT-{Id})
-- Chạy một lần nếu còn slot NULL QrCode trước khi dùng "Đồng bộ ảnh QR" trên FE.

UPDATE dbo.Slots
SET QrCode = CONCAT('SLOT-', Id)
WHERE QrCode IS NULL OR LTRIM(RTRIM(QrCode)) = N'';
