using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class SlotService : ISlotService
    {
        private static decimal BoxVolumeM3(Box box)
        {
            if (box.VolumeM3 > 0) return box.VolumeM3;
            var density = box.Lot?.GoodsReceiptDetail?.ProductVariant?.DensityKgPerM3 ?? 0m;
            if (density > 0 && box.Weight > 0) return box.Weight / density;
            return 0m;
        }

        private readonly ISlotRepository _slotRepository;
        private readonly IRackRepository _rackRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SlotService(
            ISlotRepository slotRepository,
            IRackRepository rackRepository,
            IUnitOfWork unitOfWork)
        {
            _slotRepository = slotRepository;
            _rackRepository = rackRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<SlotDto>> GetByRackAsync(int rackId)
        {
            var slots = await _slotRepository.GetByRackAsync(rackId);

            return slots
                .Select(s =>
                {
                    var activeBoxes = (s.Boxes ?? new List<Box>())
                        .Where(b => BoxVolumeM3(b) > 0 && b.Status != BoxStatus.Exported)
                        .ToList();
                    var usedVolume = activeBoxes.Sum(BoxVolumeM3);

                    return new SlotDto
                    {
                        ProductVariantId = activeBoxes
                        .Select(b => b.Lot?.GoodsReceiptDetail?.ProductVariantId)
                        .FirstOrDefault(v => v.HasValue && v.Value > 0),
                        ProductVariantName = activeBoxes
                        .Select(b => b.Lot?.GoodsReceiptDetail?.ProductVariant?.Name)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                        ProductName = activeBoxes
                        .Select(b => b.Lot?.GoodsReceiptDetail?.ProductVariant?.Product?.Name)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                        Id = s.Id,
                        Code = s.Code,
                        QrCode = s.QrCode,
                        QrImageUrl = s.QrImageUrl,
                        Capacity = s.Capacity,
                        CurrentCapacity = usedVolume > 0 ? usedVolume : s.CurrentCapacity,
                        LengthCm = s.LengthCm,
                        WidthCm = s.WidthCm,
                        HeightCm = s.HeightCm,
                        VolumeM3 = s.VolumeM3,
                        RackId = s.RackId,
                        RackName = null // GetByRackAsync không include navigation Rack
                    };
                })
                .ToList();
        }

        public async Task<int> CreateAsync(int rackId, CreateSlotRequest request)
        {
            var rack = await _rackRepository.GetByIdAsync(rackId);
            if (rack == null)
            {
                throw new NotFoundException("Rack không tồn tại");
            }
            if (!request.LengthCm.HasValue || !request.WidthCm.HasValue || !request.HeightCm.HasValue ||
                request.LengthCm.Value <= 0 || request.WidthCm.Value <= 0 || request.HeightCm.Value <= 0)
            {
                throw new InvalidBusinessRuleException("Slot phải có dài, rộng, cao lớn hơn 0.");
            }
            var slotBaseAreaM2 = (request.LengthCm.Value * request.WidthCm.Value) / 10000m;
            if (rack.FloorAreaM2.HasValue && slotBaseAreaM2 > rack.FloorAreaM2.Value)
            {
                throw new InvalidBusinessRuleException("Diện tích đáy slot không được lớn hơn diện tích rack.");
            }
            if (rack.FloorAreaM2.HasValue)
            {
                var existingSlots = await _slotRepository.GetByRackAsync(rackId);
                var totalOtherSlotsBaseArea = existingSlots.Sum(s =>
                {
                    if (s.LengthCm.HasValue && s.WidthCm.HasValue)
                    {
                        return (s.LengthCm.Value * s.WidthCm.Value) / 10000m;
                    }
                    return 0m;
                });
                if (totalOtherSlotsBaseArea + slotBaseAreaM2 > rack.FloorAreaM2.Value)
                {
                    throw new InvalidBusinessRuleException("Tổng diện tích đáy các slot không được lớn hơn diện tích rack.");
                }
            }
            var slotVolumeM3 = request.VolumeM3 ??
                (request.LengthCm.Value * request.WidthCm.Value * request.HeightCm.Value) / 1000000m;

            var slot = new Slot
            {
                Code = request.Code.Trim(),
                Capacity = request.Capacity,
                CurrentCapacity = 0,
                RackId = rackId,
                QrCode = request.QrCode?.Trim(),
                LengthCm = request.LengthCm,
                WidthCm = request.WidthCm,
                HeightCm = request.HeightCm,
                VolumeM3 = slotVolumeM3
            };

            await _slotRepository.AddAsync(slot);
            await _unitOfWork.SaveChangesAsync();

            // QR payload mặc định để quét slot (đồng bộ với FE tạo ảnh QR)
            if (string.IsNullOrWhiteSpace(slot.QrCode))
            {
                slot.QrCode = $"SLOT-{slot.Id}";
                await _slotRepository.UpdateAsync(slot);
                await _unitOfWork.SaveChangesAsync();
            }

            return slot.Id;
        }

        public async Task UpdateAsync(int id, CreateSlotRequest request)
        {
            var slot = await _slotRepository.GetByIdAsync(id);
            if (slot == null)
            {
                throw new NotFoundException("Slot không tồn tại");
            }
            if (!request.LengthCm.HasValue || !request.WidthCm.HasValue || !request.HeightCm.HasValue ||
                request.LengthCm.Value <= 0 || request.WidthCm.Value <= 0 || request.HeightCm.Value <= 0)
            {
                throw new InvalidBusinessRuleException("Slot phải có dài, rộng, cao lớn hơn 0.");
            }
            var rack = await _rackRepository.GetByIdAsync(slot.RackId);
            if (rack == null)
            {
                throw new NotFoundException("Rack không tồn tại");
            }
            var slotBaseAreaM2 = (request.LengthCm.Value * request.WidthCm.Value) / 10000m;
            if (rack.FloorAreaM2.HasValue && slotBaseAreaM2 > rack.FloorAreaM2.Value)
            {
                throw new InvalidBusinessRuleException("Diện tích đáy slot không được lớn hơn diện tích rack.");
            }
            if (rack.FloorAreaM2.HasValue)
            {
                var existingSlots = await _slotRepository.GetByRackAsync(slot.RackId);
                var totalOtherSlotsBaseArea = existingSlots
                    .Where(s => s.Id != slot.Id)
                    .Sum(s =>
                    {
                        if (s.LengthCm.HasValue && s.WidthCm.HasValue)
                        {
                            return (s.LengthCm.Value * s.WidthCm.Value) / 10000m;
                        }
                        return 0m;
                    });
                if (totalOtherSlotsBaseArea + slotBaseAreaM2 > rack.FloorAreaM2.Value)
                {
                    throw new InvalidBusinessRuleException("Tổng diện tích đáy các slot không được lớn hơn diện tích rack.");
                }
            }
            var slotVolumeM3 = request.VolumeM3 ??
                (request.LengthCm.Value * request.WidthCm.Value * request.HeightCm.Value) / 1000000m;

            slot.Code = request.Code.Trim();
            slot.Capacity = request.Capacity;
            slot.LengthCm = request.LengthCm;
            slot.WidthCm = request.WidthCm;
            slot.HeightCm = request.HeightCm;
            slot.VolumeM3 = slotVolumeM3;
            if (string.IsNullOrWhiteSpace(request.QrCode))
                slot.QrCode = $"SLOT-{slot.Id}";
            else
                slot.QrCode = request.QrCode.Trim();

            await _slotRepository.UpdateAsync(slot);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var slot = await _slotRepository.GetByIdAsync(id);
            if (slot == null)
            {
                throw new NotFoundException("Slot không tồn tại");
            }

            await _slotRepository.DeleteAsync(slot);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<SlotDto?> GetByQrCodeAsync(string qrCode)
        {
            var normalized = qrCode?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            // Lookup theo QrCode (chuẩn nhất nếu DB đã backfill)
            var slot = await _slotRepository.GetByQrCodeAsync(normalized);

            // Fallback: nhiều trường hợp đã có QR ảnh nhưng QrCode trong DB NULL/empty.
            // Payload FE/BE dùng mặc định: "SLOT-{id}" => parse để lấy Slot theo Id.
            if (slot == null &&
                normalized.StartsWith("SLOT-", StringComparison.OrdinalIgnoreCase))
            {
                var idPart = normalized.Substring("SLOT-".Length);
                if (int.TryParse(idPart, out var slotId))
                {
                    slot = await _slotRepository.GetByIdAsync(slotId);
                }
            }

            // Fallback theo slot.Code (trường hợp payload QR cũ là Code thay vì QrCode)
            if (slot == null)
            {
                slot = await _slotRepository.GetByCodeAsync(normalized);
            }
            if (slot == null) return null;

            return new SlotDto
            {
                Id = slot.Id,
                Code = slot.Code,
                QrCode = slot.QrCode,
                QrImageUrl = slot.QrImageUrl,
                Capacity = slot.Capacity,
                CurrentCapacity = slot.CurrentCapacity,
                LengthCm = slot.LengthCm,
                WidthCm = slot.WidthCm,
                HeightCm = slot.HeightCm,
                VolumeM3 = slot.VolumeM3,
                RackId = slot.RackId,
                RackName = slot.Rack?.Name
            };
        }

        public async Task UpdateQrImageUrlAsync(int slotId, string qrImageUrl)
        {
            var slot = await _slotRepository.GetByIdAsync(slotId);
            if (slot == null)
                throw new NotFoundException("Slot không tồn tại");

            slot.QrImageUrl = qrImageUrl.Trim();
            await _slotRepository.UpdateAsync(slot);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<SlotContentsDto> GetContentsAsync(int slotId)
        {
            var slot = await _slotRepository.GetByIdWithContentsAsync(slotId);
            if (slot == null)
                throw new NotFoundException("Slot không tồn tại");

            var boxes = (slot.Boxes ?? new List<Box>())
                .Where(b => BoxVolumeM3(b) > 0 && b.Status != BoxStatus.Exported)
                .ToList();
            var first = boxes.FirstOrDefault();
            var detail = first?.Lot?.GoodsReceiptDetail;
            var pv = detail?.ProductVariant;
            var currentVolume = boxes.Sum(BoxVolumeM3);
            var effectiveCurrentCapacity = currentVolume > 0 ? currentVolume : slot.CurrentCapacity;

            var dto = new SlotContentsDto
            {
                SlotId = slot.Id,
                SlotCode = slot.Code,
                SlotQrCode = slot.QrCode,
                SlotQrImageUrl = slot.QrImageUrl,
                Capacity = slot.Capacity,
                CurrentCapacity = effectiveCurrentCapacity,
                RemainingCapacity = Math.Max(0, slot.Capacity - effectiveCurrentCapacity),
                ProductVariantId = detail?.ProductVariantId,
                ProductName = pv?.Product?.Name,
                VariantName = pv?.Name,
                BoxCount = boxes.Count(),
                TotalBoxWeight = boxes.Sum(b => b.Weight),
                TotalBoxVolumeM3 = boxes.Sum(BoxVolumeM3),
                Boxes = boxes
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new SlotBoxItemDto
                    {
                        Id = b.Id,
                        BoxCode = b.BoxCode,
                        QrCode = b.QRCode,
                        QrImageUrl = b.QrImageUrl,
                        Weight = b.Weight,
                        VolumeM3 = BoxVolumeM3(b),
                        Status = b.Status.ToString(),
                        LotId = b.LotId,
                        LotCode = b.Lot?.LotCode ?? string.Empty,
                        ReceivedDate = b.Lot?.ReceivedDate ?? default,
                        ExpiryDate = b.Lot?.ExpiryDate ?? default
                    })
                    .ToList()
            };

            return dto;
        }

        public async Task<int> SyncSlotCapacitiesByWarehouseAsync(int warehouseId)
        {
            if (warehouseId <= 0)
                throw new InvalidBusinessRuleException("WarehouseId không hợp lệ");

            var affected = await _slotRepository.RecalculateCurrentCapacityByWarehouseAsync(warehouseId);
            await _unitOfWork.SaveChangesAsync();
            return affected;
        }
    }
}

