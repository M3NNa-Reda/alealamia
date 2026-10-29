using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class SubmissionFile
    {
        public int TaskSubmissionId { get; set; } // pk , fk
        public int FileId { get; set; } // pk , fk
        public DateTime UploadedAt { get; set; } 
    }
}
