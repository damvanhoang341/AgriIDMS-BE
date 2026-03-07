# Role theo luồng Purchase → Nhập kho

## Roles đã seed (IdentitySeeder)

| Role           | Mô tả ngắn                |
|----------------|----------------------------|
| **Admin**      | Quản trị hệ thống, user, cấu hình |
| **Manager**    | Quản lý: duyệt đơn mua, duyệt phiếu nhập |
| **PurchasingStaff** | Nhân viên mua hàng: tạo PO (Manager duyệt) |
| **WarehouseStaff** | Nhân viên kho: tạo phiếu nhập, cân xe, QC, tạo box |
| **SalesStaff** | Nhân viên bán hàng (luồng khác) |
| **Customer**   | Khách hàng (luồng khác)    |

---

## Luồng Purchase → Nhập kho và role tương ứng

| Bước | Hành động | API | Role đề xuất | Ghi chú |
|------|-----------|-----|----------------|---------|
| 1 | Tạo đơn mua (PO) | `POST /api/PurchaseOrder` | **PurchasingStaff** (hoặc Admin/Manager) | Nhân viên mua hàng tạo đơn |
| 2 | Xem PO | `GET /api/PurchaseOrder/{id}` | **Manager**, **WarehouseStaff** | Cần xem để chọn dòng khi nhập |
| 3 | **Duyệt đơn mua** | `POST /api/PurchaseOrder/{id}/approve` | **Manager** | Chỉ quản lý duyệt PO |
| 4 | Tạo phiếu nhập | `POST /api/GoodsReceipts` | **WarehouseStaff** | Thủ kho lập phiếu khi xe về |
| 5 | Thêm dòng phiếu nhập | `POST /api/GoodsReceipts/detail` | **WarehouseStaff** | Khai báo từng dòng theo PO |
| 6 | Cập nhật cân xe | `PUT /api/GoodsReceipts/truck-weight` | **WarehouseStaff** | Cân bruto/tare |
| 7 | QC kiểm tra | `POST /api/GoodsReceipts/qc` | **WarehouseStaff** (hoặc **QCStaff** nếu tách) | Kiểm tra chất lượng, UsableWeight |
| 8 | Tạo box | `POST /api/GoodsReceipts/boxes` | **WarehouseStaff** | Chia lot thành box |
| 9 | **Duyệt phiếu nhập** | `POST /api/GoodsReceipts/{id}/approve` | **Manager** | Chốt nhập kho, ghi InventoryTransaction |

---

## Tóm tắt role cần cho luồng này

- **Manager**: duyệt PO, duyệt phiếu nhập (có thể xem PO / phiếu).
- **WarehouseStaff**: toàn bộ thao tác tại kho (tạo phiếu, thêm dòng, cân xe, QC, tạo box); không duyệt PO và không duyệt phiếu nhập.
- **Admin**: không bắt buộc trong luồng; dùng cho quản trị user/role.

**PurchasingStaff** chỉ tạo PO; **Manager** (hoặc Admin) duyệt PO và duyệt phiếu nhập.
