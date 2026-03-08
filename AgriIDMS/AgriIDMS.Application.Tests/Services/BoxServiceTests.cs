using AgriIDMS.Application.DTOs.Box;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AgriIDMS.Application.Tests.Services
{
    public class BoxServiceTests
    {
        private readonly Mock<IBoxRepository> _boxRepo;
        private readonly Mock<ISlotRepository> _slotRepo;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly BoxService _sut;

        public BoxServiceTests()
        {
            _boxRepo = new Mock<IBoxRepository>();
            _slotRepo = new Mock<ISlotRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _sut = new BoxService(_boxRepo.Object, _slotRepo.Object, _unitOfWork.Object);
        }

        [Fact]
        public async Task AssignBoxToSlotAsync_WhenBoxNotFound_ThrowsNotFoundException()
        {
            _boxRepo.Setup(r => r.GetByIdWithLotAndReceiptAsync(It.IsAny<int>())).ReturnsAsync(default(Box));

            var request = new AssignBoxToSlotRequest { BoxId = 1, SlotId = 2 };

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignBoxToSlotAsync(request));

            Assert.Contains("Box không tồn tại", ex.Message);
        }

        [Fact]
        public async Task AssignBoxToSlotAsync_WhenSlotNotFound_ThrowsNotFoundException()
        {
            var box = CreateBoxWithWarehouse(warehouseId: 1, weight: 10);
            _boxRepo.Setup(r => r.GetByIdWithLotAndReceiptAsync(1)).ReturnsAsync(box);
            _slotRepo.Setup(r => r.GetByIdWithWarehouseAsync(It.IsAny<int>())).ReturnsAsync(default(Slot));

            var request = new AssignBoxToSlotRequest { BoxId = 1, SlotId = 2 };

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.AssignBoxToSlotAsync(request));

            Assert.Contains("Slot không tồn tại", ex.Message);
        }

        [Fact]
        public async Task AssignBoxToSlotAsync_WhenBoxHasNoWarehouse_ThrowsInvalidBusinessRuleException()
        {
            var box = new Box { Id = 1, Weight = 10, SlotId = null, Lot = null! };
            _boxRepo.Setup(r => r.GetByIdWithLotAndReceiptAsync(1)).ReturnsAsync(box);

            var slot = CreateSlotWithWarehouse(warehouseId: 1, capacity: 100, currentCapacity: 0);
            _slotRepo.Setup(r => r.GetByIdWithWarehouseAsync(2)).ReturnsAsync(slot);

            var request = new AssignBoxToSlotRequest { BoxId = 1, SlotId = 2 };

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.AssignBoxToSlotAsync(request));

            Assert.Contains("không xác định được kho", ex.Message);
        }

        [Fact]
        public async Task AssignBoxToSlotAsync_WhenSlotHasNoWarehouse_ThrowsInvalidBusinessRuleException()
        {
            var box = CreateBoxWithWarehouse(warehouseId: 1, weight: 10);
            _boxRepo.Setup(r => r.GetByIdWithLotAndReceiptAsync(1)).ReturnsAsync(box);

            var slot = new Slot { Id = 2, Capacity = 100, CurrentCapacity = 0, Rack = null! };
            _slotRepo.Setup(r => r.GetByIdWithWarehouseAsync(2)).ReturnsAsync(slot);

            var request = new AssignBoxToSlotRequest { BoxId = 1, SlotId = 2 };

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.AssignBoxToSlotAsync(request));

            Assert.Contains("Slot không thuộc kho hợp lệ", ex.Message);
        }

        [Fact]
        public async Task AssignBoxToSlotAsync_WhenDifferentWarehouse_ThrowsInvalidBusinessRuleException()
        {
            var box = CreateBoxWithWarehouse(warehouseId: 1, weight: 10);
            _boxRepo.Setup(r => r.GetByIdWithLotAndReceiptAsync(1)).ReturnsAsync(box);

            var slot = CreateSlotWithWarehouse(warehouseId: 2, capacity: 100, currentCapacity: 0);
            _slotRepo.Setup(r => r.GetByIdWithWarehouseAsync(2)).ReturnsAsync(slot);

            var request = new AssignBoxToSlotRequest { BoxId = 1, SlotId = 2 };

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.AssignBoxToSlotAsync(request));

            Assert.Contains("cùng một kho", ex.Message);
        }

        [Fact]
        public async Task AssignBoxToSlotAsync_WhenSlotCapacityExceeded_ThrowsInvalidBusinessRuleException()
        {
            var box = CreateBoxWithWarehouse(warehouseId: 1, weight: 50);
            _boxRepo.Setup(r => r.GetByIdWithLotAndReceiptAsync(1)).ReturnsAsync(box);

            var slot = CreateSlotWithWarehouse(warehouseId: 1, capacity: 100, currentCapacity: 60);
            _slotRepo.Setup(r => r.GetByIdWithWarehouseAsync(2)).ReturnsAsync(slot);

            var request = new AssignBoxToSlotRequest { BoxId = 1, SlotId = 2 };

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.AssignBoxToSlotAsync(request));

            Assert.Contains("không đủ dung lượng", ex.Message);
        }

        [Fact]
        public async Task AssignBoxToSlotAsync_WhenSameWarehouseAndEnoughCapacity_CompletesSuccessfully()
        {
            var box = CreateBoxWithWarehouse(warehouseId: 1, weight: 10);
            box.SlotId = null;
            _boxRepo.Setup(r => r.GetByIdWithLotAndReceiptAsync(1)).ReturnsAsync(box);
            _boxRepo.Setup(r => r.UpdateAsync(It.IsAny<Box>())).Returns(Task.CompletedTask);

            var slot = CreateSlotWithWarehouse(warehouseId: 1, capacity: 100, currentCapacity: 0);
            slot.Rack!.Zone!.Warehouse!.TitleWarehouse = TitleWarehouse.Normal;
            _slotRepo.Setup(r => r.GetByIdWithWarehouseAsync(2)).ReturnsAsync(slot);
            _slotRepo.Setup(r => r.UpdateAsync(It.IsAny<Slot>())).Returns(Task.CompletedTask);

            var request = new AssignBoxToSlotRequest { BoxId = 1, SlotId = 2 };

            await _sut.AssignBoxToSlotAsync(request);

            _boxRepo.Verify(r => r.UpdateAsync(It.Is<Box>(b => b.SlotId == 2)), Times.Once);
            _slotRepo.Verify(r => r.UpdateAsync(It.Is<Slot>(s => s.CurrentCapacity == 10)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
        }

        private static Box CreateBoxWithWarehouse(int warehouseId, decimal weight)
        {
            var warehouse = new Warehouse { Id = warehouseId };
            var receipt = new GoodsReceipt { WarehouseId = warehouseId, Warehouse = warehouse };
            var detail = new GoodsReceiptDetail { GoodsReceipt = receipt };
            var lot = new Lot { GoodsReceiptDetail = detail };
            return new Box { Id = 1, Weight = weight, SlotId = null, Lot = lot };
        }

        private static Slot CreateSlotWithWarehouse(int warehouseId, decimal capacity, decimal currentCapacity)
        {
            var warehouse = new Warehouse { Id = warehouseId, TitleWarehouse = TitleWarehouse.Normal };
            var zone = new Zone { Warehouse = warehouse };
            var rack = new Rack { Zone = zone };
            return new Slot { Id = 2, Capacity = capacity, CurrentCapacity = currentCapacity, Rack = rack };
        }
    }
}
