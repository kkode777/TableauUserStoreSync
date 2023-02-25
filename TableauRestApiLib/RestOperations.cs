using System;
using System.Collections.Generic;
using System.Text;

namespace TableauRestApiLib
{
    public class RestOperations
    {
        public readonly string _basePath = "";
        public RestOperations(string basePath)
        {
            _basePath = basePath;
        }
        public string GetSignInUri()
        {
            return String.Concat(_basePath, "/auth/signin");
        }
        public string GetSignOutUri()
        {
            return String.Concat(_basePath, "/auth/signout");
        }
        public string GetCreateGroupUri(string siteId)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/groups");
        }
        public string GetDeleteGroupUri(string siteId, string groupId)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/groups/{groupId}");
        }
        public string GetGroupsUri(string siteId, int pageSize=0, int pageNumber=0)
        {
            if (pageSize > 0 && pageNumber > 0)
            {
                return String.Concat(_basePath, $"/sites/{siteId}/groups?pageSize={pageSize}&pageNumber={pageNumber}");
            }
            return String.Concat(_basePath, $"/sites/{siteId}/groups");
        }
        public string GetCreateUserUri(string siteId)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/users");
        }
        public string GetQueryUsersInSiteUri(string siteId, int pageSize=0, int pageNumber=0)
        {
            if (pageSize > 0 && pageNumber > 0)
            {
                return String.Concat(_basePath, $"/sites/{siteId}/users?pageSize={pageSize}&pageNumber={pageNumber}");
            }
            return String.Concat(_basePath, $"/sites/{siteId}/users");
        }
        public string GetQueryUsersInSiteByFilterUri(string siteId, string filter)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/users/?{filter}");
        }
        public string GetQueryUsersInGroupUri(string siteId, string groupId, int pageSize=0, int pageNumber=0)
        {
            if (pageSize > 0 && pageNumber > 0)
            {
                return String.Concat(_basePath, $"/sites/{siteId}/groups/{groupId}/users?pageSize={pageSize}&pageNumber={pageNumber}");
            }
            return String.Concat(_basePath, $"/sites/{siteId}/groups/{groupId}/users");
        }
        public string GetQueryUsersInGroupByFilterUri(string siteId, string groupId, string filter)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/groups/{groupId}/users/?{filter}");
        }
        public string GetQueryUserByIdUri(string siteId, string userId)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/users/{userId}");
        }

        public string GetAddUserToGroupUri(string siteId, string groupId)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/groups/{groupId}/users");
        }

        public string GetRemoveUserFromGroupUri(string siteId, string groupId, string userId)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/groups/{groupId}/users/{userId}");
        }

        public string GetRemoveUserFromSiteUri(string siteId, string userId)
        {
            return String.Concat(_basePath, $"/sites/{siteId}/users/{userId}");
        }

    }
}
