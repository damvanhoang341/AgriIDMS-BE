using AgriIDMS.Application.DTOs.BoxTypeSpec;

namespace AgriIDMS.Application.Interfaces
{
    public interface IBoxTypeSpecService
    {
        Task<List<BoxTypeSpecDto>> GetAllAsync();
        Task<List<BoxTypeSpecDto>> ReplaceAllAsync(List<UpsertBoxTypeSpecItemRequest> items);
    }
}

