using System;
using System.Collections.Generic;
using System.Text;

namespace TableauSyncWebJob.Models
{
    public class SyncStats
    {
        public int UsersAddedToTableau { get; set; }
        public int UsersRemovedFromTableau { get; set; }
        public int GroupsAddedToTableau { get; set; }
        public int GroupsRemovedFromTableau { get; set; }
        public Dictionary<string,int> UsersAddedToGroup { get; set; }
        public Dictionary<string, int> UsersRemovedFromGroup { get; set; }
    }
}
