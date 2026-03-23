using AgriIDMS.Application.DTOs.Lot;
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
    public class LotService : ILotService
    {
        private readonly ILotRepository _lotRepository;
        public LotService(ILotRepository lotRepository)
        {
            _lotRepository = lotRepository;
        }


        public async Task<List<Lot>> GetLotsByGoodsReceiptIdAsync(int goodsReceiptId)
        {
            var lots = await _lotRepository.GetByGoodsReceiptIdAsync(goodsReceiptId);

            if (lots == null || !lots.Any())
                return new List<Lot>();

            return lots;
        }

        public async Task<IEnumerable<NearExpiryLotDto>> GetNearExpiryLotsAsync()
        {
            var dashboard = await GetNearExpiryDashboardAsync(3);
            return dashboard.Lots;
        }

        public async Task<NearExpiryDashboardDto> GetNearExpiryDashboardAsync(int days)
        {
            if (days <= 0)
                throw new InvalidBusinessRuleException("Số ngày lọc phải lớn hơn 0");

            var now = DateTime.UtcNow;
            var lots = await _lotRepository.GetNearExpiryLotsAsync(days);
            if (lots == null || !lots.Any())
                throw new NotFoundException("Không có lô nào sắp hết hạn trong ngưỡng ngày đã chọn");

            var mappedLots = lots.Select(l =>
            {
                var nearExpiryBoxes = l.Boxes
                    .Where(b => b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved)
                    .Select(b => new NearExpiryBoxDto
                    {
                        BoxId = b.Id,
                        BoxCode = b.BoxCode,
                        Weight = b.Weight,
                        IsPartial = b.IsPartial,
                        Status = b.Status.ToString(),
                        SlotId = b.SlotId,
                        SlotCode = b.Slot?.Code
                    })
                    .ToList();

                return new NearExpiryLotDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,
                    ProductVariantId = l.GoodsReceiptDetail.ProductVariant.Id,
                    ProductName = l.GoodsReceiptDetail.ProductVariant.Product.Name,
                    Grade = l.GoodsReceiptDetail.ProductVariant.Grade.ToString(),
                    RemainingQuantity = l.RemainingQuantity,
                    ExpiryDate = l.ExpiryDate,
                    DaysLeft = (int)Math.Ceiling((l.ExpiryDate - now).TotalDays),
                    NearExpiryBoxCount = nearExpiryBoxes.Count(),
                    Boxes = nearExpiryBoxes,
                    Status = l.ExpiryDate < now ? "Expired" : "NearExpiry"
                };
            }).ToList();

            return new NearExpiryDashboardDto
            {
                DaysThreshold = days,
                TotalLots = mappedLots.Count(),
                TotalBoxes = mappedLots.Sum(x => x.NearExpiryBoxCount),
                Lots = mappedLots
            };
        }
    }
}
