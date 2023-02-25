using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableauRestApiLib.Models;

namespace TableauSyncWebJob.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public SiteRoleType? SiteRole { get; set; }
        public List<Group> Groups { get; set; }

        public override bool Equals(object obj)
        {
            var user=obj as User;
            return this.Name?.Trim().ToLower() == user?.Name?.Trim().ToLower();
        }
        public override int GetHashCode()
        {
            return Name.Trim().ToLower().GetHashCode();
        }

        

    }
}
