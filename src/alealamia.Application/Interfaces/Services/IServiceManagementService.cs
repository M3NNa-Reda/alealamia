using alealamia.Application.DTOs.Services;

namespace alealamia.Application.Interfaces.Services
{
    public interface IServiceManagementService
    {
        Task<IEnumerable<ServiceDto>> GetAllAsync(bool activeOnly = false);
        Task<ServiceDto?> GetByIdAsync(int id);
        Task<ServiceDto> CreateAsync(CreateServiceDto dto);
        Task<ServiceDto> UpdateAsync(int id, UpdateServiceDto dto);
        Task DeactivateAsync(int id);
    }
}