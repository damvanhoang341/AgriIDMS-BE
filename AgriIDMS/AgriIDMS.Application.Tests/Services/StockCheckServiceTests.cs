using AgriIDMS.Application.DTOs.StockCheck;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AgriIDMS.Application.Tests.Services
{
    public class StockCheckServiceTests
    {
        private readonly Mock<IStockCheckRepository> _stockCheckRepo;
        private readonly Mock<IStockCheckDetailRepository> _detailRepo;
        private readonly Mock<IBoxRepository> _boxRepo;
        private readonly Mock<IWarehouseRepository> _warehouseRepo;
        private readonly Mock<IInventoryRequestRepository> _inventoryRequestRepo;
        private readonly Mock<IInventoryTransactionRepository> _inventoryTranRepo;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<ILogger<StockCheckService>> _logger;
        private readonly StockCheckService _sut;

        public StockCheckServiceTests()
        {
            _stockCheckRepo = new Mock<IStockCheckRepository>();
            _detailRepo = new Mock<IStockCheckDetailRepository>();
            _boxRepo = new Mock<IBoxRepository>();
            _warehouseRepo = new Mock<IWarehouseRepository>();
            _inventoryRequestRepo = new Mock<IInventoryRequestRepository>();
            _inventoryTranRepo = new Mock<IInventoryTransactionRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _logger = new Mock<ILogger<StockCheckService>>();

            _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _sut = new StockCheckService(
                _stockCheckRepo.Object,
                _detailRepo.Object,
                _boxRepo.Object,
                _warehouseRepo.Object,
                _inventoryRequestRepo.Object,
                _inventoryTranRepo.Object,
                _unitOfWork.Object,
                _logger.Object);
        }

        [Fact]
        public async Task CreateAsync_WhenWarehouseNotFound_ThrowsNotFoundException()
        {
            _warehouseRepo.Setup(r => r.GetWarehouseByIdAsync(It.IsAny<int>())).ReturnsAsync(default(Warehouse));

            var request = new CreateStockCheckRequest { WarehouseId = 1, CheckType = StockCheckType.Spot, BoxIds = new List<int> { 1 } };

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request, "user1"));

            Assert.Contains("Kho không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenSpotAndBoxIdsEmpty_ThrowsInvalidBusinessRuleException()
        {
            _warehouseRepo.Setup(r => r.GetWarehouseByIdAsync(1)).ReturnsAsync(new Warehouse { Id = 1 });

            var request = new CreateStockCheckRequest { WarehouseId = 1, CheckType = StockCheckType.Spot, BoxIds = new List<int>() };

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.CreateAsync(request, "user1"));

            Assert.Contains("BoxIds", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenSpotAndBoxIdsNull_ThrowsInvalidBusinessRuleException()
        {
            _warehouseRepo.Setup(r => r.GetWarehouseByIdAsync(1)).ReturnsAsync(new Warehouse { Id = 1 });

            var request = new CreateStockCheckRequest { WarehouseId = 1, CheckType = StockCheckType.Spot, BoxIds = null };

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.CreateAsync(request, "user1"));

            Assert.Contains("BoxIds", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenFullAndNoBoxesInWarehouse_ThrowsInvalidBusinessRuleException()
        {
            _warehouseRepo.Setup(r => r.GetWarehouseByIdAsync(1)).ReturnsAsync(new Warehouse { Id = 1 });
            _stockCheckRepo.Setup(r => r.GetBoxIdsInWarehouseAsync(1)).ReturnsAsync(new List<int>());

            var request = new CreateStockCheckRequest { WarehouseId = 1, CheckType = StockCheckType.Full };

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.CreateAsync(request, "user1"));

            Assert.Contains("không có box", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenSpotAndBoxNotFound_ThrowsNotFoundException()
        {
            _warehouseRepo.Setup(r => r.GetWarehouseByIdAsync(1)).ReturnsAsync(new Warehouse { Id = 1 });
            var boxIds = new List<int> { 1, 2 };
            _boxRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Dictionary<int, Box> { { 1, new Box { Id = 1, Weight = 10 } } });

            var request = new CreateStockCheckRequest { WarehouseId = 1, CheckType = StockCheckType.Spot, BoxIds = boxIds };

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request, "user1"));

            Assert.Contains("Box Id=", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenSpotWithValidBoxIds_ReturnsNewId()
        {
            _warehouseRepo.Setup(r => r.GetWarehouseByIdAsync(1)).ReturnsAsync(new Warehouse { Id = 1 });
            var boxIds = new List<int> { 1, 2 };
            _boxRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Box> { { 1, new Box { Id = 1, Weight = 10 } }, { 2, new Box { Id = 2, Weight = 20 } } });

            var newId = 99;
            _stockCheckRepo.Setup(r => r.AddAsync(It.IsAny<StockCheck>()))
                .Callback<StockCheck>(sc => sc.Id = newId)
                .Returns(Task.CompletedTask);
            _detailRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<StockCheckDetail>>())).Returns(Task.CompletedTask);

            var request = new CreateStockCheckRequest { WarehouseId = 1, CheckType = StockCheckType.Spot, BoxIds = boxIds };

            var id = await _sut.CreateAsync(request, "user1");

            Assert.Equal(newId, id);
            _stockCheckRepo.Verify(r => r.AddAsync(It.Is<StockCheck>(sc => sc.WarehouseId == 1 && sc.CheckType == StockCheckType.Spot && sc.Status == StockCheckStatus.Draft)), Times.Once);
            _detailRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<StockCheckDetail>>(d => System.Linq.Enumerable.Count(d) == 2)), Times.Once);
        }

        [Fact]
        public async Task StartCheckAsync_WhenStockCheckNotFound_ThrowsNotFoundException()
        {
            _stockCheckRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(default(StockCheck));

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.StartCheckAsync(1));

            Assert.Contains("Phiếu kiểm kê không tồn tại", ex.Message);
        }

        [Fact]
        public async Task StartCheckAsync_WhenStatusNotDraft_ThrowsInvalidBusinessRuleException()
        {
            _stockCheckRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new StockCheck { Id = 1, Status = StockCheckStatus.InProgress });

            var ex = await Assert.ThrowsAsync<InvalidBusinessRuleException>(() => _sut.StartCheckAsync(1));

            Assert.Contains("Draft", ex.Message);
        }

        [Fact]
        public async Task StartCheckAsync_WhenDraft_UpdatesToInProgress()
        {
            var stockCheck = new StockCheck { Id = 1, Status = StockCheckStatus.Draft };
            _stockCheckRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(stockCheck);
            _stockCheckRepo.Setup(r => r.UpdateAsync(It.IsAny<StockCheck>())).Returns(Task.CompletedTask);

            await _sut.StartCheckAsync(1);

            _stockCheckRepo.Verify(r => r.UpdateAsync(It.Is<StockCheck>(sc => sc.Status == StockCheckStatus.InProgress && sc.IsLockedSnapshot)), Times.Once);
        }
    }
}
