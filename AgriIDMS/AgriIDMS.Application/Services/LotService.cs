using AgriIDMS.Application.DTOs.Lot;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class LotService : ILotService
    {
        private readonly ILotRepository _lotRepository;
        private readonly IBoxRepository _boxRepo;
        public LotService(ILotRepository lotRepository,IBoxRepository boxRepository)
        {
            _lotRepository = lotRepository;
            _boxRepo = boxRepository;
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
            var now = DateTime.UtcNow;

            var lots = await _lotRepository.GetAllExpiryDateAsync();
            if (lots == null || !lots.Any()) throw new NotFoundException("Không có sản phầm nào sắp hết hạn");
            var result = new List<NearExpiryLotDto>();
            foreach(var l in lots)
            {
                var boxCount = await _boxRepo.GetAvailableBoxCountByVariantIdAsync(l.GoodsReceiptDetail.ProductVariantId);
                result.Add(new NearExpiryLotDto
                {
                    LotId = l.Id,
                    LotCode = l.LotCode,

                    ProductVariantId = l.GoodsReceiptDetail.ProductVariant.Id,
                    ProductName = l.GoodsReceiptDetail.ProductVariant.Product.Name,
                    Grade = l.GoodsReceiptDetail.ProductVariant.Grade.ToString(),

                    RemainingQuantity = l.RemainingQuantity,

                    ExpiryDate = l.ExpiryDate,
                    DaysLeft = (l.ExpiryDate - now).Days,

                    Status = l.ExpiryDate < now ? "Expired" : "NearExpiry"
                });
            }
            return result;
        }
    }
}
