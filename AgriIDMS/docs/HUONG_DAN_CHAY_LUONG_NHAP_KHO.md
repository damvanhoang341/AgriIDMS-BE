# Hướng dẫn chạy luồng Purchase Order → Nhập kho

## 0. Chuẩn bị

### 0.1 Chạy API

```bash
cd AgriIDMS
dotnet run --project AgriIDMS.API
```

- API chạy tại: **http://localhost:5132**
- Swagger: http://localhost:5132/swagger

### 0.2 Database

- Đảm bảo đã cấu hình connection string trong `appsettings.Development.json`.
- Chạy migration (nếu chưa): `dotnet ef database update --project AgriIDMS.Infrastructure --startup-project AgriIDMS.API`
- Lần đầu chạy app sẽ **seed role** (Admin, Manager, PurchasingStaff, WarehouseStaff, …) và **tài khoản Admin**: `admin` / `Admin@123` (đổi mật khẩu sau khi deploy).

### 0.3 Dữ liệu cần có sẵn

Để chạy luồng cần có ít nhất:

- **1 Supplier** (nhà cung cấp)
- **1 Warehouse** (kho)
- **1 Product** có **ProductVariant** (để đặt hàng và nhập)

Nếu chưa có, tạo qua Swagger hoặc seed:

- `GET /api/Suppliers` → nếu trống thì `POST` tạo Supplier.
- `GET /api/Warehouses` → nếu trống thì tạo Warehouse.
- `GET /api/ProductVariant` → lấy một `id` (ProductVariantId) để dùng trong PO và phiếu nhập.

---

## 1. Lấy token (đăng nhập)

Mọi request có `[Authorize]` cần gửi header:

```http
Authorization: Bearer <AccessToken>
```

**Login:**

```http
POST http://localhost:5132/api/Auth/Login
Content-Type: application/json

{
  "userNameOrEmail": "admin",
  "password": "Admin@123"
}
```

Response mẫu:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "...",
  "userId": "...",
  "userName": "admin",
  "roles": ["Admin"]
}
```

Copy `accessToken` và dùng cho các bước sau (trong Swagger: nút **Authorize** → nhập `Bearer <accessToken>`).

---

## 2. Luồng từng bước

### Bước 1: Tạo đơn mua (PO)

**Role:** PurchasingStaff (hoặc Admin / Manager).

```http
POST http://localhost:5132/api/PurchaseOrder
Authorization: Bearer <token>
Content-Type: application/json

{
  "supplierId": 1,
  "details": [
    {
      "productVariantId": 1,
      "orderedWeight": 1000,
      "unitPrice": 25000
    }
  ]
}
```

Response: `{ "message": "Tạo đơn mua thành công", "purchaseOrderId": 1 }`  
→ Ghi lại **purchaseOrderId** (ví dụ `1`).

---

### Bước 2: Duyệt đơn mua (PO)

**Role:** Manager hoặc Admin.

```http
POST http://localhost:5132/api/PurchaseOrder/1/approve
Authorization: Bearer <token>
```

(Thay `1` bằng `purchaseOrderId` của bước 1.)

---

### Bước 3: Lấy chi tiết PO (để lấy Id dòng đơn)

**Role:** User đã login (Manager / WarehouseStaff / …).

```http
GET http://localhost:5132/api/PurchaseOrder/1
Authorization: Bearer <token>
```

Response có dạng:

```json
{
  "id": 1,
  "orderCode": "PO-20250307-0001",
  "supplierId": 1,
  "supplierName": "...",
  "status": "Approved",
  "orderDate": "...",
  "details": [
    {
      "id": 1,
      "productVariantId": 1,
      "productName": "...",
      "orderedWeight": 1000,
      "unitPrice": 25000
    }
  ]
}
```

→ Ghi lại **details[].id** (đây là **PurchaseOrderDetailId** dùng ở bước 5). Ví dụ: `1`.

---

### Bước 4: Tạo phiếu nhập kho

**Role:** WarehouseStaff (hoặc Manager / Admin).

**Lưu ý:** `supplierId` phải trùng với PO (ở đây là `1`).

```http
POST http://localhost:5132/api/GoodsReceipts
Authorization: Bearer <token>
Content-Type: application/json

{
  "supplierId": 1,
  "warehouseId": 1,
  "vehicleNumber": "51C-12345",
  "driverName": "Nguyễn Văn A",
  "transportCompany": "Công ty TNHH VT",
  "tolerancePercent": 2
}
```

Response: `{ "message": "Tạo phiếu nhập thành công", "receiptId": 1 }`  
→ Ghi lại **receiptId** (ví dụ `1`).

---

### Bước 5: Thêm chi tiết phiếu nhập

**Role:** WarehouseStaff (hoặc Manager / Admin).

Dùng **PurchaseOrderDetailId** = `details[].id` lấy ở bước 3 (ví dụ `1`), **productVariantId** và **orderedWeight** khớp với dòng PO. **GoodsReceiptId** = phiếu vừa tạo (bước 4).  
**Không gửi đơn giá**: backend tự lấy từ PO để tránh lộ giá nhập cho thủ kho.

```http
POST http://localhost:5132/api/GoodsReceipts/detail
Authorization: Bearer <token>
Content-Type: application/json

{
  "goodsReceiptId": 1,
  "purchaseOrderDetailId": 1,
  "productVariantId": 1,
  "orderedWeight": 980
}
```

---

### Bước 6: Cập nhật cân xe

**Role:** WarehouseStaff (hoặc Manager / Admin).

```http
PUT http://localhost:5132/api/GoodsReceipts/truck-weight
Authorization: Bearer <token>
Content-Type: application/json

{
  "goodsReceiptId": 1,
  "grossWeight": 12000,
  "tareWeight": 3000
}
```

---

### Bước 7: QC kiểm tra (từng dòng phiếu nhập)

**Role:** WarehouseStaff (hoặc Manager / Admin).

**DetailId** = id của **GoodsReceiptDetail** (sau khi thêm dòng ở bước 5). Nếu chỉ có 1 dòng, thường là `1`. **QCResult**: `Passed` | `Failed` | `Partial` | `Pending`.

```http
POST http://localhost:5132/api/GoodsReceipts/qc
Authorization: Bearer <token>
Content-Type: application/json

{
  "detailId": 1,
  "usableWeight": 950,
  "qcResult": "Passed",
  "qcNote": "Đạt chuẩn"
}
```

Sau bước này hệ thống tự tạo **Lot** cho dòng đó.

---

### Bước 8: Tạo Box từ Lot

**Role:** WarehouseStaff (hoặc Manager / Admin).

Cần biết **LotId**. Lot vừa được tạo ở bước 7 (có thể xem trong DB bảng `Lots`, hoặc thêm API get receipt by id kèm details/lots). Ví dụ `lotId = 1`, chia mỗi box 100 kg:

```http
POST http://localhost:5132/api/GoodsReceipts/boxes
Authorization: Bearer <token>
Content-Type: application/json

{
  "lotId": 1,
  "boxSize": 100
}
```

(Số box = `TotalQuantity / boxSize` lấy phần nguyên; ví dụ 950/100 = 9 box.)

---

### Bước 9: Duyệt phiếu nhập

**Role:** Manager hoặc Admin.

```http
POST http://localhost:5132/api/GoodsReceipts/1/approve
Authorization: Bearer <token>
```

(Thay `1` bằng **receiptId**.)

Sau bước này phiếu chuyển **Approved**, hệ thống tạo **InventoryTransaction** (Import) cho từng box → hàng chính thức nhập kho.

---

## 3. Test nhanh bằng một tài khoản (Admin)

- Đăng nhập **admin** / **Admin@123** (có role Admin → gọi được mọi API).
- Lần lượt gọi từ bước 1 đến bước 9 với cùng token.
- Đảm bảo đã có ít nhất 1 Supplier, 1 Warehouse, 1 ProductVariant (id dùng trong `supplierId`, `warehouseId`, `productVariantId`).

---

## 4. Test đúng role (nhiều user)

1. **Admin** đăng nhập → gọi `POST /api/Auth/admin/create-employee` tạo nhân viên (body có `email`, `role`). Role dùng: `PurchasingStaff`, `Manager`, `WarehouseStaff`. (User mới cần xác nhận email hoặc Admin set email confirmed + mật khẩu tạm tùy cách bạn cấu hình.)
2. **PurchasingStaff** login → tạo PO (bước 1).
3. **Manager** login → duyệt PO (bước 2), sau đó **WarehouseStaff** có thể gọi GET PO (bước 3).
4. **WarehouseStaff** login → bước 4 → 5 → 6 → 7 → 8.
5. **Manager** login → duyệt phiếu nhập (bước 9).

---

## 5. Thứ tự tóm tắt

| # | Hành động           | API                          | Role chính        |
|---|---------------------|------------------------------|-------------------|
| 1 | Tạo PO             | POST /api/PurchaseOrder      | PurchasingStaff   |
| 2 | Duyệt PO           | POST /api/PurchaseOrder/{id}/approve | Manager    |
| 3 | Xem PO (lấy detail id) | GET /api/PurchaseOrder/{id}  | Manager/WarehouseStaff |
| 4 | Tạo phiếu nhập     | POST /api/GoodsReceipts      | WarehouseStaff    |
| 5 | Thêm dòng phiếu    | POST /api/GoodsReceipts/detail | WarehouseStaff  |
| 6 | Cân xe             | PUT /api/GoodsReceipts/truck-weight | WarehouseStaff |
| 7 | QC                 | POST /api/GoodsReceipts/qc   | WarehouseStaff    |
| 8 | Tạo box            | POST /api/GoodsReceipts/boxes | WarehouseStaff   |
| 9 | Duyệt phiếu nhập   | POST /api/GoodsReceipts/{id}/approve | Manager  |

---

## 6. Lỗi thường gặp

- **401 Unauthorized**: Chưa gửi header `Authorization: Bearer <token>` hoặc token hết hạn. Login lại lấy token mới.
- **403 Forbidden**: User không có role đúng. Ví dụ PurchasingStaff gọi duyệt PO → 403. Đổi user có role Manager/Admin.
- **"Đơn mua chưa được duyệt"**: Thêm chi tiết phiếu nhập (bước 5) khi PO chưa approve. Cần gọi bước 2 trước.
- **"Phiếu nhập phải cùng nhà cung cấp với đơn mua"**: Tạo phiếu nhập với `supplierId` khác PO → sửa `supplierId` trùng với PO.
- **"Sản phẩm không khớp với dòng đơn mua"**: `productVariantId` trong body không trùng với dòng PO đã chọn (`purchaseOrderDetailId`) → dùng đúng `productVariantId` từ GET PO details.
- **"Có sản phẩm chưa QC"**: Duyệt phiếu nhập (bước 9) khi còn dòng có `QCResult = Pending`. Cần QC hết (bước 7) cho mọi dòng.
- **LotId khi tạo box**: Hiện API chưa trả về list lot theo receipt. Có thể xem trong DB (`Lots` table) hoặc bổ sung API GET phiếu nhập kèm Details/Lots để lấy `lotId`.
