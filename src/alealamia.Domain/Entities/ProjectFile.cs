using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class ProjectFile
    {
        public int ProjectId { get; set; } // PK, FK
        public int FileId { get; set; } // PK, FK
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        //public Project Project { get; set; } = null!;
        public File File { get; set; } = null!;
    }
}