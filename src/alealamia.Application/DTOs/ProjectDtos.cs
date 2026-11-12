using alealamia.Domain.Enums;

namespace alealamia.Application.DTOs.Projects
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public int ClientProfileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasCustomization { get; set; }
        public ProjectStatus Status { get; set; }
        public decimal? FinalPrice { get; set; }  
        public string Currency { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProjectDetailsDto : ProjectDto
    {
        public List<ProjectServiceDto> Services { get; set; } = new();
        public List<ProjectStatusHistoryDto> StatusHistory { get; set; } = new();
    }

    public class CreateProjectDto
    {
        public int ClientProfileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasCustomization { get; set; }
        public string Currency { get; set; } = string.Empty;
        public List<AddProjectServiceDto> Services { get; set; } = new();
    }

    public class UpdateProjectDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasCustomization { get; set; }
    }

    public class UpdateProjectStatusDto
    {
        public ProjectStatus NewStatus { get; set; }
        public decimal? FinalPrice { get; set; }
        public string? Notes { get; set; }
        public string ChangedByUserId { get; set; } = string.Empty;
    }

    public class ProjectStatusHistoryDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public ProjectStatus? OldStatus { get; set; }
        public ProjectStatus NewStatus { get; set; }
        public string ChangedByUserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class ProjectServiceDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? CustomizationDetails { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddProjectServiceDto
    {
        public int ServiceId { get; set; }
        public string? CustomizationDetails { get; set; }
    }

    public class UpdateProjectServiceDto
    {
        public string? CustomizationDetails { get; set; }
    }
}