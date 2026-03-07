# Đánh giá Entity luồng nhập kho (từ PurchaseOrder)

## 1. Tổng quan quan hệ

```
Supplier ──┬── PurchaseOrder (1-N)
           │       └── PurchaseOrderDetail (1-N) ── ProductVariant
           └── GoodsReceipt (1-N)
                   └── GoodsReceiptDetail (1-N) ──► PurchaseOrderDetail (N-1)
                           └── Lot (1-N)
                                   └── Box (1-N) ── Slot?
                                           └── InventoryTransaction (1-N)

Warehouse ──► GoodsReceipt (N-1)
```

---

## 2. Từng bảng – đúng / sát thực tế / thiếu

### 2.1 PurchaseOrder

| Field | Đánh giá | Ghi chú |
|-------|----------|---------|
| Id, OrderCode | ✅ | Chuẩn ERP |
| SupplierId, OrderDate | ✅ | Đúng nghiệp vụ |
| Status (Draft, Pending, Approved, PartiallyReceived, Completed, Cancelled) | ✅ | Đủ vòng đời đơn mua |
| CreatedBy, ApprovedBy, ApprovedAt | ✅ | Audit, duyệt đơn |
| Details (PurchaseOrderDetail) | ✅ | 1 PO nhiều dòng |

**Thiếu / gợi ý (không bắt buộc):**

- **ExpectedDeliveryDate**: ngày giao dự kiến (thực tế hay có).
- **PurchaseOrderId trên GoodsReceipt**: không bắt buộc vì đã liên kết qua GoodsReceiptDetail → PurchaseOrderDetail; nếu cần “1 phiếu nhập = 1 đơn” thì thêm FK optional từ GoodsReceipt → PurchaseOrder để tra cứu nhanh.

**Kết luận:** Đúng và sát thực tế cho luồng nhập kho từ PO.

---

### 2.2 PurchaseOrderDetail

| Field | Đánh giá | Ghi chú |
|-------|----------|---------|
| Id, PurchaseOrderId, ProductVariantId | ✅ | Chuẩn |
| OrderedWeight, UnitPrice | ✅ | Đúng: đặt bao nhiêu, giá bao nhiêu |
| GoodsReceiptDetails (collection) | ✅ | 1 dòng PO có thể nhận nhiều lần (nhiều phiếu) |

**Thiếu so với ERP thực tế:**

- **ReceivedQuantity / TotalReceivedWeight**: tổng khối lượng đã nhận cho dòng PO (tính từ các GoodsReceiptDetail cùng PurchaseOrderDetailId). Hiện có thể tính bằng query, nhưng không lưu sẵn → không tự cập nhật PartiallyReceived/Completed trên PO. Nếu muốn trạng thái PO “Đã nhận đủ” tự động thì nên có field này (hoặc computed) và cập nhật khi approve phiếu nhập.
- **RejectedQuantity** (số lượng từ chối/trả NCC): tùy nghiệp vụ, có thể thêm sau.

**Kết luận:** Đúng hướng; thiếu theo dõi “đã nhận bao nhiêu” nếu muốn PO tự chuyển PartiallyReceived/Completed.

---

### 2.3 GoodsReceipt

| Field | Đánh giá | Ghi chú |
|-------|----------|---------|
| Id, SupplierId, WarehouseId | ✅ | Đúng: nhập của ai, vào kho nào |
| Status (Draft → PendingManagerApproval / Approved / Rejected) | ✅ | Sát luồng ERP (dung sai, duyệt) |
| VehicleNumber, DriverName, TransportCompany | ✅ | Vận chuyển |
| GrossWeight, TareWeight, NetWeight (computed) | ✅ | Cân xe |
| TolerancePercent | ✅ | Dung sai hao hụt |
| TotalOrderedWeight, TotalUsableWeight, TransportLossWeight, QCLossWeight, TotalLossWeight, AllowedLossWeight, ClaimableWeight | ✅ | Tính từ Details, phục vụ báo cáo/đối soát |
| CreatedBy, ApprovedBy, ReceivedDate, CreatedAt, ApprovedAt | ✅ | Audit và duyệt |
| Details | ✅ | 1 phiếu nhiều dòng |

**Lưu ý:**

- **TotalLossWeight**: hiện là property có setter private + method `CalculateTotalLossWeight()`; nếu không gọi method thì giá có thể cũ. Có thể đổi thành computed: `TotalOrderedWeight - TotalUsableWeight` (hoặc tương đương) để luôn đồng bộ.
- **PurchaseOrderId (optional)**: thêm nếu muốn “phiếu nhập gắn 1 đơn” và tra cứu nhanh.

**Kết luận:** Đúng và sát thực tế; chỉ cần nhất quán cách tính TotalLossWeight (computed vs. gọi method).

---

### 2.4 GoodsReceiptDetail

| Field | Đánh giá | Ghi chú |
|-------|----------|---------|
| Id, GoodsReceiptId, ProductVariantId, PurchaseOrderDetailId | ✅ | Liên kết phiếu – dòng PO – sản phẩm |
| OrderedWeight (từ PO) | ✅ | Không do kho nhập |
| ReceivedWeight (kho nhập) | ✅ | Sát thực tế: kho chỉ khai thực nhận |
| UsableWeight, RejectWeight (tính từ Received − Usable) | ✅ | QC: dùng được / loại |
| QCResult, QCNote, InspectedBy, InspectedAt | ✅ | Audit QC |
| UnitPrice (từ PO) | ✅ | Không lộ cho kho; lưu để tính giá trị nhập |
| Lots | ✅ | 1 dòng nhiều lot (sau khi approve) |

**Kết luận:** Đúng và sát thực tế so với luồng đã refactor (Ordered/Received/Usable, QC, UnitPrice từ PO).

---

### 2.5 Lot

| Field | Đánh giá | Ghi chú |
|-------|----------|---------|
| Id, LotCode, GoodsReceiptDetailId | ✅ | Lot gắn dòng phiếu nhập |
| TotalQuantity, RemainingQuantity | ✅ | FEFO, xuất kho trừ dần |
| ExpiryDate, ReceivedDate | ✅ | Hạn dùng, ngày nhập |
| Status (Active, Blocked, Expired) | ✅ | Quản lý lot |
| Boxes | ✅ | 1 lot nhiều thùng |

**Gợi ý:**

- **ProductVariantId**: hiện suy ra qua GoodsReceiptDetail. Thêm redundant (denormalize) nếu hay query tồn theo Lot/Variant mà không cần load Detail.
- **WarehouseId / ZoneId**: nếu lot luôn thuộc 1 kho/khu vực cố định có thể thêm để filter nhanh; không bắt buộc vì có thể đi qua Receipt.

**Kết luận:** Đúng và sát thực tế; ProductVariantId/WarehouseId chỉ là tối ưu truy vấn.

---

### 2.6 Box

| Field | Đánh giá | Ghi chú |
|-------|----------|---------|
| Id, BoxCode, LotId, Weight | ✅ | Đơn vị tồn kho (thùng) |
| SlotId (nullable) | ✅ | Vị trí trong kho (nếu có sơ đồ slot) |
| QRCode, Status (Stored, Reserved, Picking, Exported, Damaged, Expired) | ✅ | Theo dõi trạng thái thùng |
| Transactions, Allocations | ✅ | Lịch sử nhập/xuất/di chuyển và phân bổ đơn hàng |

**Kết luận:** Đúng và sát thực tế (FEFO, slot, trạng thái, trace qua transaction).

---

### 2.7 InventoryTransaction

| Field | Đánh giá | Ghi chú |
|-------|----------|---------|
| BoxId, TransactionType (Import, Move, Export, Adjust) | ✅ | Ghi nhận theo từng box |
| Quantity, FromSlotId, ToSlotId | ✅ | Số lượng và di chuyển vị trí |
| ReferenceType, ReferenceRequestId | ✅ | Liên kết chứng từ (GoodsReceipt, Export, …) |
| CreatedBy, CreatedAt | ✅ | Audit |

**Thiếu nhỏ:**

- Khi tạo transaction **Import** từ GoodsReceipt, nên set **ReferenceType = GoodsReceipt** và **ReferenceRequestId = GoodsReceipt.Id** (nếu mapping cho phép) để truy vết “nhập từ phiếu nào”. Hiện logic tạo transaction có thể chưa set đủ 2 field này.

**Kết luận:** Đúng hướng; nên bổ sung gắn Reference theo phiếu nhập khi tạo transaction Import.

---

### 2.8 Supplier, Warehouse, ProductVariant

- **Supplier**: Id, Name, ContactPerson, Phone, Email, Address, Status, quan hệ GoodsReceipts – đủ cho luồng nhập.
- **Warehouse**: Id, Name, Location, TitleWarehouse, Zones, GoodsReceipts – đủ.
- **ProductVariant**: Id, ProductId, Grade, Price, IsActive, GoodsReceiptDetails – đủ; đơn vị (kg) đang thể hiện qua OrderedWeight/ReceivedWeight/UsableWeight ở chi tiết.

**Kết luận:** Các bảng này đúng và sát thực tế cho luồng từ PO đến nhập kho.

---

## 3. Tóm tắt

| Khía cạnh | Đánh giá |
|-----------|----------|
| Quan hệ PO → PO Detail → GoodsReceipt → Detail → Lot → Box → InventoryTransaction | ✅ Đúng, sát ERP |
| Phân tách Ordered / Received / Usable, QC, dung sai | ✅ Sát thực tế |
| Trạng thái PO (PartiallyReceived, Completed) | ⚠️ Enum có nhưng chưa có field/cập nhật “đã nhận bao nhiêu” trên PO Detail |
| Trace chứng từ (ReferenceType/ReferenceRequestId) trên InventoryTransaction | ⚠️ Nên set khi tạo Import từ phiếu nhập |
| TotalLossWeight trên GoodsReceipt | ⚠️ Nên dùng computed thay vì property + method để tránh quên gọi |

**Kết luận chung:** Các bảng entity trong luồng nhập kho từ PurchaseOrder **đã đúng và khá sát thực tế**. Chỉ cần bổ sung nhỏ: (1) theo dõi đã nhận trên PurchaseOrderDetail (và cập nhật status PO nếu cần), (2) gắn ReferenceType/ReferenceRequestId khi tạo InventoryTransaction Import, (3) có thể chuyển TotalLossWeight sang computed. Nếu bạn muốn, có thể triển khai luôn 3 điểm này trong code.
