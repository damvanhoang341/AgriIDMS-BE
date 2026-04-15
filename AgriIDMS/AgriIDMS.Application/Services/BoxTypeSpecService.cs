using AgriIDMS.Application.DTOs.BoxTypeSpec;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;

namespace AgriIDMS.Application.Services
{
    public class BoxTypeSpecService : IBoxTypeSpecService
    {
        private readonly IBoxTypeSpecRepository _boxTypeSpecRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BoxTypeSpecService(
            IBoxTypeSpecRepository boxTypeSpecRepository,
            IUnitOfWork unitOfWork)
        {
            _boxTypeSpecRepository = boxTypeSpecRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<BoxTypeSpecDto>> GetAllAsync()
        {
            var data = await _boxTypeSpecRepository.GetAllActiveAsync();
            return data.Select(Map).ToList();
        }

        public async Task<List<BoxTypeSpecDto>> ReplaceAllAsync(List<UpsertBoxTypeSpecItemRequest> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new InvalidBusinessRuleException("Danh sách kích cỡ không được để trống.");
            }

            EnsureHasRequiredTypes(items);

            var now = DateTime.UtcNow;
            var newEntities = items.Select(x => new BoxTypeSpec
            {
                BoxType = x.BoxType,
                DisplayName = (x.DisplayName ?? string.Empty).Trim(),
                LengthCm = x.LengthCm,
                WidthCm = x.WidthCm,
                HeightCm = x.HeightCm,
                IsActive = true,
                CreatedAt = now
            }).ToList();

            if (newEntities.Any(x => string.IsNullOrWhiteSpace(x.DisplayName)))
            {
                throw new InvalidBusinessRuleException("Tên kích cỡ không được để trống.");
            }

            var allOld = await _boxTypeSpecRepository.GetAllAsync();
            if (allOld.Count > 0)
            {
                await _boxTypeSpecRepository.RemoveRangeAsync(allOld);
            }

            await _boxTypeSpecRepository.AddRangeAsync(newEntities);
            await _unitOfWork.SaveChangesAsync();

            return newEntities.Select(Map).ToList();
        }

        private static void EnsureHasRequiredTypes(IEnumerable<UpsertBoxTypeSpecItemRequest> items)
        {
            var types = items.Select(x => x.BoxType).ToHashSet();
            var required = new[] { BoxType.StyrofoamBox, BoxType.Carton, BoxType.MeshBag };
            var missing = required.Where(x => !types.Contains(x)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidBusinessRuleException(
                    $"Thiếu cấu hình cho loại box: {string.Join(", ", missing)}");
            }
        }

        private static BoxTypeSpecDto Map(BoxTypeSpec e)
        {
            return new BoxTypeSpecDto
            {
                Id = e.Id,
                BoxType = e.BoxType,
                DisplayName = e.DisplayName,
                LengthCm = e.LengthCm,
                WidthCm = e.WidthCm,
                HeightCm = e.HeightCm,
                VolumeM3 = (e.LengthCm * e.WidthCm * e.HeightCm) / 1_000_000m
            };
        }
    }
}

