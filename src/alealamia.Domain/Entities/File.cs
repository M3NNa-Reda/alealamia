using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class File
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty; // FK
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();
        public ICollection<SupportMessageFile> SupportMessageFiles { get; set; } = new List<SupportMessageFile>();
        public ICollection<TaskFile> TaskFiles { get; set; } = new List<TaskFile>();
        public ICollection<SubmissionFile> SubmissionFiles { get; set; } = new List<SubmissionFile>();
        public ICollection<PortfolioItemFile> PortfolioItemFiles { get; set; } = new List<PortfolioItemFile>();
    }
}

