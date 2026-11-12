using alealamia.Application.DTOs.Services;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Services
{
    public class ServiceManagementService : IServiceManagementService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ServiceManagementService(IServiceRepository serviceRepository, IUnitOfWork unitOfWork)
        {
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ServiceDto>> GetAllAsync(bool activeOnly = false)
        {
            var services = activeOnly
                ? await _serviceRepository.GetActiveServicesAsync()
                : await _serviceRepository.GetAllAsync();

            return services.Select(MapToDto);
        }

        public async Task<ServiceDto?> GetByIdAsync(int id)
        {
            var service = await _serviceRepository.GetByIdAsync(id);
            return service is null ? null : MapToDto(service);
        }

        public async Task<ServiceDto> CreateAsync(CreateServiceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Service name is required.");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Service description is required.");

            var name = dto.Name.Trim();
            if (await _serviceRepository.ExistsByNameAsync(name))
                throw new InvalidOperationException("A service with the same name already exists.");

            var service = new Service
            {
                Name = name,
                Description = dto.Description.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _serviceRepository.AddAsync(service);
            await _unitOfWork.CompleteAsync();
            return MapToDto(service);
        }

        public async Task<ServiceDto> UpdateAsync(int id, UpdateServiceDto dto)
        {
            var service = await _serviceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Service {id} was not found.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Service name is required.");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Service description is required.");

            var name = dto.Name.Trim();
            if (!string.Equals(service.Name, name, StringComparison.OrdinalIgnoreCase)
                && await _serviceRepository.ExistsByNameAsync(name))
                throw new InvalidOperationException("A service with the same name already exists.");

            service.Name = name;
            service.Description = dto.Description.Trim();
            service.IsActive = dto.IsActive;
            service.UpdatedAt = DateTime.UtcNow;

            _serviceRepository.Update(service);
            await _unitOfWork.CompleteAsync();
            return MapToDto(service);
        }

        public async Task DeactivateAsync(int id)
        {
            var service = await _serviceRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Service {id} was not found.");

            service.IsActive = false;
            service.UpdatedAt = DateTime.UtcNow;

            _serviceRepository.Update(service);
            await _unitOfWork.CompleteAsync();
        }

        private static ServiceDto MapToDto(Service s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}