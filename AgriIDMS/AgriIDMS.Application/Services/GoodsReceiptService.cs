using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class GoodsReceiptService
    {
        private readonly IGoodsReceiptRepository _goodsReceiptRepository;
        public GoodsReceiptService(IGoodsReceiptRepository goodsReceiptRepository)
        {
            _goodsReceiptRepository = goodsReceiptRepository;
        }

    }
}
