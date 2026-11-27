namespace alealamia.Application.DTOs.Users
{
    public class UserListQueryDto
    {
        public string? Search { get; set; }   // name, email or phone
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}