using System;
using System.Collections.Generic;
using System.Text;

namespace TableauRestApiLib.Models
{
    public class PaginationInfo
    {
        public int PageSize { get; set; }
        public int TotalAvailable { get; set; }
        public int PageCount { get; set; }
    }
}
