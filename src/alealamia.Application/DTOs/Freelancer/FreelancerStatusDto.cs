using alealamia.Domain.Enums;

namespace alealamia.Application.DTOs.Freelancer
{
    public class FreelancerStatusDto
    {
        public FreelancerApprovalStatus ApprovalStatus { get; set; }
        public bool IsAvailable { get; set; }
    }
}