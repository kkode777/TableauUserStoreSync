using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TableauSyncWebJob.Models
{
    public interface IDisDataProvider
    {
        /// <summary>Gets DIS active users asynchronous.</summary>
        /// <returns>List of Users</returns>
        Task<List<User>> GetUsersAsync();

        /// <summary>Gets list of groups asynchronous.</summary>
        /// <returns>List of Groups</returns>
        Task<List<Group>> GetGroupsAsync();


        /// <summary>Gets users by group asynchronous.</summary>
        /// <returns>A dictionary of groups with users for each group</returns>
        Task<Dictionary<string, List<User>>> GetUsersByGroupAsync();
    }
}
