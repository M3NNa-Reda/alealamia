using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class SupportMessageFile
    {
        public int SupportMessageId { get; set; } // PK, FK
        public int FileId { get; set; } // PK, FK

        public SupportMessage SupportMessage { get; set; } = null!;
        public File File { get; set; } = null!;
    }
}