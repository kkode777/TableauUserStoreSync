using System;
using System.Collections.Generic;
using System.Text;

namespace TableauRestApiLib
{
    public class TableauConfigSettings
    {
        public string BaseUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SiteId { get; set; }
        public bool RemoveInactiveUsers { get; set; }
    }
}
