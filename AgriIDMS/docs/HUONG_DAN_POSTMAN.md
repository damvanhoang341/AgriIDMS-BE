# Hướng dẫn test API bằng Postman

## 1. Chuẩn bị

### 1.1 Chạy API

```bash
cd AgriIDMS
dotnet run --project AgriIDMS.API
```

- API chạy tại: **http://localhost:5132**
- Có thể mở Swagger: http://localhost:5132/swagger

### 1.2 Cài Postman

- Tải Postman: https://www.postman.com/downloads/
- Hoặc dùng Postman for Web (đăng nhập tài khoản Postman)

### 1.3 Tạo Environment trong Postman

1. Vào **Environments** (biểu tượng bánh răng hoặc tab Environments).
2. **Create Environment** → đặt tên ví dụ: `AgriIDMS Local`.
3. Thêm biến:

| Variable   | Initial Value       | Current Value       |
|-----------|---------------------|---------------------|
| `base_url` | `http://localhost:5132` | `http://localhost:5132` |
| `token`   | (để trống)          | (sẽ gán sau khi Login) |

4. **Save** và chọn environment này (dropdown góc trên phải).

---

## 2. Lấy token (Đăng nhập)

Mọi API (trừ Login) đều cần header: `Authorization: Bearer <token>`.

### Request

- **Method:** `POST`
- **URL:** `{{base_url}}/api/Auth/Login`
- **Headers:**  
  `Content-Type: application/json`
- **Body (raw, JSON):**

```json
{
  "userNameOrEmail": "admin",
  "password": "Admin@123"
}
```

### Lưu token tự động (khuyến nghị)

1. Mở request **Login** → tab **Tests**.
2. Thêm script:

```javascript
if (pm.response.code === 200) {
    var json = pm.response.json();
    if (json.accessToken) {
        pm.environment.set("token", json.accessToken);
    }
}
```

3. Sau khi **Send**, token sẽ được gán vào biến `token`. Các request khác dùng header:  
   **Authorization:** `Bearer {{token}}`

### Response mẫu

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "...",
  "userId": "...",
  "userName": "admin",
  "roles": ["Admin"]
}
```

---

## 3. Danh sách request theo luồng

### 3.1 Auth

| # | Mô tả        | Method | URL | Role |
|---|--------------|--------|-----|------|
| 1 | Đăng nhập    | POST   | `{{base_url}}/api/Auth/Login` | - |

---

### 3.2 Purchase Order (Đơn mua)

| # | Mô tả           | Method | URL | Role |
|---|-----------------|--------|-----|------|
| 1 | Tạo đơn mua     | POST   | `{{base_url}}/api/PurchaseOrder` | Admin, Manager, PurchasingStaff |
| 2 | Xem đơn mua     | GET    | `{{base_url}}/api/PurchaseOrder/{{id}}` | Đã login |
| 3 | Cập nhật đơn mua | PUT   | `{{base_url}}/api/PurchaseOrder/{{id}}` | Admin, Manager, PurchasingStaff |
| 4 | Duyệt đơn mua   | POST   | `{{base_url}}/api/PurchaseOrder/{{id}}/approve` | Admin, Manager |
| 5 | Xóa đơn mua     | DELETE | `{{base_url}}/api/PurchaseOrder/{{id}}` | Admin, Manager, PurchasingStaff |

**Lưu ý:** Cập nhật và Xóa chỉ được khi PO trạng thái **Pending** và (với Xóa) chưa có phiếu nhập kho.

---

### 3.3 Phiếu nhập kho (GoodsReceipts)

| # | Mô tả                | Method | URL | Role |
|---|----------------------|--------|-----|------|
| 1 | Tạo phiếu nhập       | POST   | `{{base_url}}/api/GoodsReceipts` | Admin, Manager, WarehouseStaff |
| 2 | Thêm dòng phiếu      | POST   | `{{base_url}}/api/GoodsReceipts/detail` | Admin, Manager, WarehouseStaff |
| 3 | Cập nhật cân xe      | PUT    | `{{base_url}}/api/GoodsReceipts/truck-weight` | Admin, Manager, WarehouseStaff |
| 4 | QC kiểm tra          | POST   | `{{base_url}}/api/GoodsReceipts/qc` | Admin, Manager, WarehouseStaff |
| 5 | Duyệt phiếu nhập     | POST   | `{{base_url}}/api/GoodsReceipts/{{receiptId}}/approve` | Admin, Manager |
| 6 | Manager duyệt (vượt dung sai) | POST | `{{base_url}}/api/GoodsReceipts/{{receiptId}}/manager-approve` | Admin, Manager |
| 7 | Manager từ chối     | POST   | `{{base_url}}/api/GoodsReceipts/{{receiptId}}/manager-reject` | Admin, Manager |
| 8 | Tạo box (sau khi Approved) | POST | `{{base_url}}/api/GoodsReceipts/boxes` | Admin, Manager, WarehouseStaff |

---

## 4. Body mẫu từng request

### 4.1 POST – Tạo đơn mua (Purchase Order)

**URL:** `{{base_url}}/api/PurchaseOrder`  
**Headers:** `Authorization: Bearer {{token}}`, `Content-Type: application/json`

```json
{
  "supplierId": 1,
  "details": [
    {
      "productVariantId": 1,
      "orderedWeight": 1000,
      "unitPrice": 25000,
      "tolerancePercent": 2
    }
  ]
}
```

Response: `{ "message": "Tạo đơn mua thành công", "purchaseOrderId": 1 }`  
→ Ghi **purchaseOrderId** (ví dụ lưu vào biến `po_id` = 1).

---

### 4.2 GET – Xem đơn mua

**URL:** `{{base_url}}/api/PurchaseOrder/1`  
**Headers:** `Authorization: Bearer {{token}}`

(Thay `1` bằng `purchaseOrderId`.)

→ Từ response lấy **details[].id** (PurchaseOrderDetailId) dùng khi thêm chi tiết phiếu nhập. Ví dụ: `detail_id` = 1.

---

### 4.3 PUT – Cập nhật đơn mua

**URL:** `{{base_url}}/api/PurchaseOrder/1`  
**Headers:** `Authorization: Bearer {{token}}`, `Content-Type: application/json`

Chỉ khi PO đang **Pending**. Có thể chỉ gửi `supplierId`, hoặc chỉ `details`, hoặc cả hai.

```json
{
  "supplierId": 1,
  "details": [
    {
      "id": 1,
      "productVariantId": 1,
      "orderedWeight": 1200,
      "unitPrice": 26000,
      "tolerancePercent": 2
    }
  ]
}
```

- **id** có giá trị: cập nhật dòng đó (chỉ dòng chưa nhập kho).
- **id** = 0 hoặc null: thêm dòng mới.

---

### 4.4 POST – Duyệt đơn mua

**URL:** `{{base_url}}/api/PurchaseOrder/1/approve`  
**Headers:** `Authorization: Bearer {{token}}`

Không body. Thay `1` bằng `purchaseOrderId`.

---

### 4.5 DELETE – Xóa đơn mua

**URL:** `{{base_url}}/api/PurchaseOrder/1`  
**Headers:** `Authorization: Bearer {{token}}`

Chỉ khi PO **Pending** và chưa có phiếu nhập kho. Thay `1` bằng `purchaseOrderId`.

---

### 4.6 POST – Tạo phiếu nhập kho

**URL:** `{{base_url}}/api/GoodsReceipts`  
**Headers:** `Authorization: Bearer {{token}}`, `Content-Type: application/json`

**supplierId** phải trùng với PO.

```json
{
  "supplierId": 1,
  "warehouseId": 1,
  "vehicleNumber": "51C-12345",
  "driverName": "Nguyễn Văn A",
  "transportCompany": "Công ty TNHH VT",
  "purchaseOrderId": 1
}
```

`purchaseOrderId` là tùy chọn (để gắn phiếu với đơn mua).

Response: `{ "message": "Tạo phiếu nhập thành công", "receiptId": 1 }`  
→ Ghi **receiptId** (ví dụ `receipt_id` = 1).

---

### 4.7 POST – Thêm chi tiết phiếu nhập

**URL:** `{{base_url}}/api/GoodsReceipts/detail`  
**Headers:** `Authorization: Bearer {{token}}`, `Content-Type: application/json`

**purchaseOrderDetailId** lấy từ GET đơn mua (details[].id). **productVariantId** phải trùng với dòng PO đó.

```json
{
  "goodsReceiptId": 1,
  "purchaseOrderDetailId": 1,
  "productVariantId": 1,
  "receivedWeight": 980
}
```

---

### 4.8 PUT – Cập nhật cân xe

**URL:** `{{base_url}}/api/GoodsReceipts/truck-weight`  
**Headers:** `Authorization: Bearer {{token}}`, `Content-Type: application/json`

**grossWeight** phải lớn hơn **tareWeight**.

```json
{
  "goodsReceiptId": 1,
  "grossWeight": 12000,
  "tareWeight": 3000
}
```

---

### 4.9 POST – QC kiểm tra

**URL:** `{{base_url}}/api/GoodsReceipts/qc`  
**Headers:** `Authorization: Bearer {{token}}`, `Content-Type: application/json`

**detailId** = Id của dòng phiếu nhập (GoodsReceiptDetail). **usableWeight** ≤ **receivedWeight** của dòng đó.  
**qcResult:** `Passed` | `Failed` | `Partial` | `Pending`.

```json
{
  "detailId": 1,
  "usableWeight": 950,
  "qcResult": "Passed",
  "qcNote": "Đạt chuẩn"
}
```

Lặp cho từng dòng phiếu (mỗi dòng một request).

---

### 4.10 POST – Duyệt phiếu nhập

**URL:** `{{base_url}}/api/GoodsReceipts/1/approve`  
**Headers:** `Authorization: Bearer {{token}}`

Không body. Thay `1` bằng `receiptId`. Gọi sau khi đã QC hết các dòng.

- Trong dung sai (theo TolerancePercent từng dòng PO) → phiếu **Approved**, tạo Lot.
- Vượt dung sai → phiếu **PendingManagerApproval** → gọi manager-approve hoặc manager-reject.

---

### 4.11 POST – Manager duyệt / từ chối (khi PendingManagerApproval)

**Manager duyệt:**  
**URL:** `{{base_url}}/api/GoodsReceipts/1/manager-approve`  
**Headers:** `Authorization: Bearer {{token}}`

**Manager từ chối:**  
**URL:** `{{base_url}}/api/GoodsReceipts/1/manager-reject`  
**Headers:** `Authorization: Bearer {{token}}`

Thay `1` bằng `receiptId`.

---

### 4.12 POST – Tạo box (sau khi phiếu Approved)

**URL:** `{{base_url}}/api/GoodsReceipts/boxes`  
**Headers:** `Authorization: Bearer {{token}}`, `Content-Type: application/json`

**lotId** lấy từ Lot đã tạo khi duyệt phiếu (có thể xem DB bảng Lots hoặc API GET phiếu kèm Lots nếu có).

```json
{
  "lotId": 1,
  "boxSize": 100
}
```

---

## 5. Thứ tự chạy luồng đầy đủ (Postman)

1. **Login** → lấy token (hoặc dùng script Tests để gán `token`).
2. **POST PurchaseOrder** → ghi `purchaseOrderId`.
3. **POST PurchaseOrder/{id}/approve**.
4. **GET PurchaseOrder/{id}** → ghi `details[].id` (PurchaseOrderDetailId) và `productVariantId`.
5. **POST GoodsReceipts** → ghi `receiptId`.
6. **POST GoodsReceipts/detail** (có thể nhiều lần cho nhiều dòng).
7. **PUT GoodsReceipts/truck-weight**.
8. **POST GoodsReceipts/qc** (từng dòng, mỗi dòng một request với `detailId` tương ứng).
9. **POST GoodsReceipts/{receiptId}/approve**.
10. (Nếu PendingManagerApproval) **POST GoodsReceipts/{receiptId}/manager-approve** hoặc **manager-reject**.
11. **POST GoodsReceipts/boxes** với `lotId` (sau khi phiếu Approved).

---

## 6. Thiết lập Authorization chung trong Postman

Để không phải gõ header từng request:

1. **Settings** (biểu tượng bánh răng) → **Collections** (hoặc mở một Collection).
2. Chọn Collection của bạn → tab **Authorization**.
3. **Type:** Bearer Token.
4. **Token:** `{{token}}`.
5. Mọi request trong Collection sẽ kế thừa. Request **Login** nên đặt **Authorization** = No Auth (override).

Hoặc với từng request: tab **Authorization** → Type **Bearer Token** → Token: `{{token}}`.

---

## 7. Lỗi thường gặp

| Mã / Triệu chứng | Nguyên nhân | Cách xử lý |
|------------------|-------------|------------|
| 401 Unauthorized | Thiếu token hoặc token hết hạn | Gửi header `Authorization: Bearer {{token}}`; Login lại lấy token mới. |
| 403 Forbidden | User không đủ role | Đăng nhập user có role tương ứng (Admin, Manager, PurchasingStaff, WarehouseStaff). |
| "Đơn mua chưa được duyệt" | Thêm chi tiết phiếu khi PO chưa approve | Duyệt PO trước (POST .../approve). |
| "Phiếu nhập phải cùng nhà cung cấp với đơn mua" | supplierId phiếu ≠ supplierId PO | Đặt supplierId trùng với PO. |
| "Sản phẩm không khớp với dòng đơn mua" | productVariantId không đúng với dòng PO | Dùng productVariantId từ GET PurchaseOrder/{id} (details). |
| "Có sản phẩm chưa QC" | Duyệt phiếu khi còn dòng Pending | QC hết các dòng (POST .../qc) rồi mới duyệt phiếu. |
| "Không được chỉnh sửa đơn mua sau khi đã duyệt" | Update/Delete PO đã Approved | Chỉ sửa/xóa PO khi trạng thái Pending. |
| "Không thể xóa đơn mua đã có phiếu nhập kho" | Delete PO đã có phiếu nhập | Chỉ xóa PO khi chưa có phiếu nhập. |

---

## 8. Export Collection (tùy chọn)

Có thể tạo một **Collection** trong Postman, thêm toàn bộ request trên vào đó, rồi **Export** (File → Export) để chia sẻ hoặc backup. Khi import lại, nhớ chọn đúng **Environment** (có `base_url` và `token`).
