using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class TaskFile
    {
        public int TaskId { get; set; } // pk , fk
        public int FileId { get; set; } // pk , fk
        public DateTime AddedAt { get; set; }
        public File File { get; set; } = null!;
    }
}
