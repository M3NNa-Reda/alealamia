using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Domain.Entities
{
    public class PortfolioItemFile
    {
        public int PortfolioItemId { get; set; } //pk,fk
        public int FileId { get; set; } //pk,fk
        public int DisplayOrder { get; set; }
        public PortfolioItem PortfolioItem { get; set; } = null!;
        //public File File { get; set; } nav prop
    }
}
