using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableauSyncWebJob.Models
{
    public class Group
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public override bool Equals(object obj) 
        {
            var group = obj as Group;
            return Name?.Trim().ToLower() == group?.Name?.Trim().ToLower();
        }

        public override int GetHashCode()
        {
            return this.Name.Trim().ToLower().GetHashCode();
        }
    }
}
