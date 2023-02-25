using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TableauRestApiLib.Models;

namespace TableauRestApiLib
{
    public interface IRestApiService
    {
        /// <summary>Sign in asynchronous.</summary>
        /// <param name="contentUrl">The content URL.</param>
        /// <returns>Tableau Credentials</returns>
        Task<TableauCredentialsType> SignInAsync(string contentUrl);

        /// <summary>Sign out asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <returns>True or False</returns>
        Task<bool> SignOutAsync(TableauCredentialsType credential);

        /// <summary>Creates a group in site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns>Newly crete Group</returns>
        Task<GroupType> CreateGroupAsync(TableauCredentialsType credential, string siteId, string groupName);
        
        /// <summary>Deletes a group from site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>True or False</returns>
        Task<bool> DeleteGroupAsync(TableauCredentialsType credential, string siteId, string groupId);

        /// <summary>Adds user to a site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="username">The username.</param>
        /// <returns>Newly created User</returns>
        Task<UserType> CreateUserAsync(TableauCredentialsType credential, string siteId, string username);

        //Add user to group
        /// <summary>Adds a user to a group asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>User added to the Group</returns>
        Task<UserType> AddUserToGroupAsync(TableauCredentialsType credential, string siteId, string groupId, string userId);

        /// <summary>Removes a user from group asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>True or False</returns>
        Task<bool> RemoveUserFromGroupAsync(TableauCredentialsType credential, string siteId, string groupId, string userId);

        /// <summary>Removes a user from site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>True or False</returns>
        Task<bool> RemoveUserFromSiteAsync(TableauCredentialsType credential, string siteId, string userId);

        //Get list of users
        /// <summary>Queries the users in site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <returns>UserListType with a list of Users</returns>
        Task<UserListType> QueryUsersInSiteAsync(TableauCredentialsType credential, string siteId);
        //Get list of users by filter

        /// <summary>Queries the users in group asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>UserListType with a list of Users</returns>
        Task<UserListType> QueryUsersInGroupAsync(TableauCredentialsType credential, string siteId, string groupId);
        //Get user by userid

        //Get list of groups
        /// <summary>Queries the groups in site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <returns>GroupLisType with a list of Groups</returns>
        Task<GroupListType> QueryGroupsInSiteAsync(TableauCredentialsType credential, string siteId);
    }
}
