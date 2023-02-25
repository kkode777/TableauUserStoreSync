using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TableauRestApiLib.Models;

namespace TableauRestApiLib
{
    /// <summary>API Service client for the Tableau REST API to manage users and groups.</summary>
    /// <seealso cref="TableauRestApiLib.IRestApiService" />
    public class RestApiService : IRestApiService
    {
        private const string TABLEAU_AUTH_HEADER = "X-Tableau-Auth";
        private readonly RestOperations _restOperations = null;
        private readonly ILogger<RestApiService> _logger;
        private readonly TableauConfigSettings _settings;
        private const int MaxRetries = 3;
        private readonly TimeSpan delay = TimeSpan.FromSeconds(5); //Deley for retries
        public RestApiService(IOptions<TableauConfigSettings> configOptions, ILogger<RestApiService> logger)
        {
            _settings = configOptions.Value;
            _logger = logger;
            _restOperations = new RestOperations(_settings.BaseUrl);
        }
      
        /// <summary>Sign in asynchronous.</summary>
        /// <param name="contentUrl">The content URL.</param>
        /// <returns>Tableau Credentials</returns>
        /// <exception cref="Exception"></exception>
        public async Task<TableauCredentialsType> SignInAsync(string contentUrl)
        {
            _logger.LogDebug($"InvokeSignIn called for username:{_settings.Username}");

            var url = _restOperations.GetSignInUri();

            var site = new SiteType
            {
                ContentUrl = contentUrl
            };

            var signInCredentials = new TableauCredentialsType
            {
                Name = _settings.Username,
                Password = _settings.Password,
                Site = site
            };

            var payload = new TsRequest
            {
                Credentials = signInCredentials
            };

            // Makes a POST request with no credential
            var response = await PostAsync(url, null, payload);
            if (response?.Credentials == null)
            {
                var errorMessage = $"Exception occurred while signing in with userid {_settings.Username}. " +
                    $"Exception Code: {response?.Error?.Code} , Exception Details: {response?.Error?.Detail}";
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }

            return response.Credentials;
        }

        /// <summary>Sign out asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <returns>True or False</returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> SignOutAsync(TableauCredentialsType credential)
        {
            _logger.LogDebug("Signing out of Tableau Server");

            var url = _restOperations.GetSignOutUri();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, credential.Token);

                var response = await httpClient.PostAsync(url, null);

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogDebug("Successfully signed out of Tableau Server");
                    return true;
                }
                else
                {
                    var message = $"Failed to sign out of Tableau Server with response -{response?.ToString()}";
                    _logger.LogError(message);
                    throw new Exception(message);
                }
            }

        }

        /// <summary>Creates a group in site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns>Newly crete Group</returns>
        /// <exception cref="Exception"></exception>
        public async Task<GroupType> CreateGroupAsync(TableauCredentialsType credential, string siteId, string groupName)
        {
            var url = _restOperations.GetCreateGroupUri(siteId);

            var group = new GroupType()
            {
                Name = groupName,
                Import = null

            };

            var payload = new TsRequest()
            {
                Group = group
            };

            var response = await PostAsync(url, credential.Token, payload);

            if (response.Group == null)
            {
                var errorMessage = $"Exception occurred while creating group {groupName}. " +
                    $"Exception Code: {response?.Error?.Code} , Exception Details: {response?.Error?.Detail}";
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
            _logger.LogDebug($"Successfully created group {groupName} in site {siteId}");
            return response.Group;
        }

        /// <summary>Deletes a group from site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>True or False</returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> DeleteGroupAsync(TableauCredentialsType credential, string siteId, string groupId)
        {
            var url = _restOperations.GetDeleteGroupUri(siteId, groupId);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, credential.Token);

                var response = await httpClient.DeleteAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogDebug($"Deleted group-{groupId} successfully");
                    return true;
                }
                else
                {
                    var message = $"Failed to delete group -{groupId} with message {response?.ToString()}";
                    _logger.LogError(message);
                    throw new Exception(message);
                }
            }
        }

        /// <summary>Queries the groups in site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <returns>GroupLisType with a list of Groups</returns>
        /// <exception cref="Exception">Error while retrieving groups for site: {siteId}</exception>
        public async Task<GroupListType> QueryGroupsInSiteAsync(TableauCredentialsType credential, string siteId)
        {
            var url = _restOperations.GetGroupsUri(siteId);

            var response = await GetAsync(url, credential.Token);

            if (response.GroupList != null)
            {
                var groupList = response.GroupList;
                var pageInfo = GetPaginationInfo(response);
                if (pageInfo.PageCount > 1)   
                {
                    for (int pageNum = 2; pageNum <= pageInfo.PageCount; pageNum++) //first page already retrieved, so start from 2
                    {
                        var pageUrl= _restOperations.GetGroupsUri(siteId,pageInfo.PageSize,pageNum);
                        var pageResponse = await GetAsync(pageUrl, credential.Token);
                        if (pageResponse?.GroupList?.Group?.Count > 0)
                        {
                            groupList.Group.AddRange(pageResponse?.GroupList?.Group);
                        }
                    }
                }
                return groupList;
            }
            else
            {
                throw new Exception($"Error while retrieving groups for site: {siteId}");
            }
        }

        /// <summary>Adds user to a site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="username">The username.</param>
        /// <returns>Newly created User</returns>
        /// <exception cref="Exception"></exception>
        public async Task<UserType> CreateUserAsync(TableauCredentialsType credential, string siteId, string username)
        {
            var url = _restOperations.GetCreateUserUri(siteId);

            var user = new UserType()
            {
                Name = username,
                SiteRole=SiteRoleType.Viewer
            };

            var payload = new TsRequest()
            {
                User = user
            };

            var response = await PostAsync(url, credential.Token, payload);

            if (response.User == null)
            {
                var errorMessage = $"Exception occurred while creating user {username}. " +
                    $"Exception Code: {response?.Error?.Code} , Exception Details: {response?.Error?.Detail}";
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
            _logger.LogDebug($"Successfully created user {username} in site {siteId}");
            return response.User;
        }

        /// <summary>Adds a user to a group asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>User added to the Group</returns>
        /// <exception cref="Exception"></exception>
        public async Task<UserType> AddUserToGroupAsync(TableauCredentialsType credential, string siteId, string groupId, string userId)
        {
            var url = _restOperations.GetAddUserToGroupUri(siteId, groupId);

            var user = new UserType()
            {
                Id = userId
            };

            var payload = new TsRequest()
            {
                User = user
            };

            var response = await PostAsync(url, credential.Token, payload);

            if (response.User == null)
            {
                var errorMessage = $"Exception occurred while creating user {userId}. " +
                    $"Exception Code: {response?.Error?.Code} , Exception Details: {response?.Error?.Detail}";
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
            _logger.LogDebug($"Successfully added user {userId} to group {groupId}");
            return response.User;
        }

        /// <summary>Removes a user from group asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>True or False</returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> RemoveUserFromGroupAsync(TableauCredentialsType credential, string siteId, string groupId, string userId)
        {
            var url = _restOperations.GetRemoveUserFromGroupUri(siteId, groupId, userId);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, credential.Token);

                var response = await httpClient.DeleteAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogDebug($"Removed user-{userId} from group-{groupId} successfully");
                    return true;
                }
                else
                {
                    var message = $"Failed to remove user-{userId} from group -{groupId} with message {response?.ToString()}";
                    _logger.LogError(message);
                    throw new Exception(message);
                }
            }
        }

        /// <summary>Removes a user from site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>True or False</returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> RemoveUserFromSiteAsync(TableauCredentialsType credential, string siteId, string userId)
        {
            var url = _restOperations.GetRemoveUserFromSiteUri(siteId, userId);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, credential.Token);

                var response = await httpClient.DeleteAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogDebug($"Removed user-{userId} from site-{siteId} successfully");
                    return true;
                }
                else
                {
                    var message = $"Failed to remove user-{userId} from site -{siteId} with message {response?.ToString()}";
                    _logger.LogError(message);
                    throw new Exception(message);
                }
            }
        }

        /// <summary>Queries the users in site asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <returns>UserListType with a list of Users</returns>
        /// <exception cref="Exception">Error while retrieving users</exception>
        public async Task<UserListType> QueryUsersInSiteAsync(TableauCredentialsType credential, string siteId)
        {
            var url = _restOperations.GetQueryUsersInSiteUri(siteId);
            var response = await GetAsync(url,credential.Token);
            if (response.UserList != null)
            {
                var userList = response.UserList;
                var pageInfo=GetPaginationInfo(response);
                if (pageInfo.PageCount > 1)
                {
                    for (int pageNum = 2; pageNum <= pageInfo.PageCount; pageNum++)
                    {
                        var pageUrl = _restOperations.GetQueryUsersInSiteUri(siteId, pageInfo.PageSize, pageNum);
                        var pageResponse = await GetAsync(pageUrl, credential.Token);
                        if (pageResponse?.UserList?.User.Count > 0)
                        {
                            userList.User.AddRange(pageResponse?.UserList?.User);
                        }
                    }
                }
                return userList;
            }
            else 
            {
                throw new Exception("Error while retrieving users");
            }
        }

        /// <summary>Queries the users in site by filter asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Error while retrieving users in site -{siteId} by filter -{filter}</exception>
        public async Task<UserListType> QueryUsersInSiteByFilterAsync(TableauCredentialsType credential, string siteId,string filter)
        {
            var url = _restOperations.GetQueryUsersInSiteByFilterUri(siteId,filter);

            var response = await GetAsync(url, credential.Token);

            if (response.UserList != null)
            {
                return response.UserList;
            }
            else
            {
                throw new Exception($"Error while retrieving users in site -{siteId} by filter -{filter} ");
            }
        }
        /// <summary>Queries the users in group asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <returns>UserListType with a list of Users</returns>
        /// <exception cref="Exception">Error while retrieving users</exception>
        public async Task<UserListType> QueryUsersInGroupAsync(TableauCredentialsType credential, string siteId,string groupId)
        {
            var url = _restOperations.GetQueryUsersInGroupUri(siteId,groupId);

            var response = await GetAsync(url, credential.Token);

            if (response.UserList != null)
            {
                var userList = response.UserList;
                var pageInfo = GetPaginationInfo(response);
                if (pageInfo.PageCount > 1)
                {
                    for (int pageNum = 2; pageNum <= pageInfo.PageCount; pageNum++)
                    {
                        var pageUrl = _restOperations.GetQueryUsersInGroupUri(siteId, groupId,pageInfo.PageSize, pageNum);
                        var pageResponse = await GetAsync(pageUrl, credential.Token);
                        if (pageResponse?.UserList?.User.Count > 0)
                        {
                            userList.User.AddRange(pageResponse?.UserList?.User);
                        }
                    }
                }
                return userList;
            }
            else
            {
                throw new Exception("Error while retrieving users");
            }
        }

        /// <summary>Queries the users in group by filter asynchronous.</summary>
        /// <param name="credential">The credential.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Error while retrieving users</exception>
        public async Task<UserListType> QueryUsersInGroupByFilterAsync(TableauCredentialsType credential, string siteId, string groupId,string filter)
        {
            var url = _restOperations.GetQueryUsersInGroupByFilterUri(siteId, groupId,filter);

            var response = await GetAsync(url, credential.Token);

            if (response.UserList != null)
            {
                return response.UserList;
            }
            else
            {
                throw new Exception("Error while retrieving users");
            }
        }
        /// <summary>Posts the asynchronous.</summary>
        /// <param name="url">The URL.</param>
        /// <param name="token">The token.</param>
        /// <param name="payLoad">The pay load.</param>
        /// <returns></returns>
        private async Task<TsResponse> PostAsync(string url, string token, TsRequest payLoad)
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, token);
            }

            var httpContent = new StringContent(XmlExtensions.Serialize<TsRequest>(payLoad), Encoding.UTF8, "text/xml");

            //_logger.LogDebug($"Post Url: {url}");


            var response = await client.PostAsync(url, httpContent);

            string responseBody = await response.Content.ReadAsStringAsync();

           // _logger.LogDebug($"Response from tableau:{responseBody}");
            var tsResponse = XmlExtensions.Deserialize<TsResponse>(responseBody);

            return tsResponse;
        }
        /// <summary>Gets the asynchronous.</summary>
        /// <param name="url">The URL.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        private async Task<TsResponse> GetAsync(string url, string token)
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, token);
            }
           
            var response = await client.GetAsync(url);

            string responseBody = await response.Content.ReadAsStringAsync();

            var tsResponse = XmlExtensions.Deserialize<TsResponse>(responseBody);

            return tsResponse;
        }
        private PaginationInfo GetPaginationInfo(TsResponse response)
        {
            var paginationType = response.PaginationType;
            var totalCount = Int32.Parse(paginationType.TotalAvailable);
            var pageSize = Int32.Parse(paginationType.PageSize);
            var pageCount = (int)Math.Ceiling((double)totalCount / (double)pageSize);

            return new PaginationInfo { TotalAvailable = totalCount, PageSize = pageSize, PageCount = pageCount };
        }

        private async Task<TsResponse> PostAsyncWithReTry(string url, string token, TsRequest payLoad)
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, token);
            }

            var httpContent = new StringContent(XmlExtensions.Serialize<TsRequest>(payLoad), Encoding.UTF8, "text/xml");

            //_logger.LogDebug($"Post Url: {url}");

            HttpResponseMessage response = null;

            for (int i = 0; i < MaxRetries; i++)
            {
                response = await client.PostAsync(url, httpContent);
                if (response.IsSuccessStatusCode) 
                {
                    break;
                }
                await Task.Delay(delay);
            }
            string responseBody = await response.Content.ReadAsStringAsync();

            // _logger.LogDebug($"Response from tableau:{responseBody}");
            var tsResponse = XmlExtensions.Deserialize<TsResponse>(responseBody);

            return tsResponse;
        }

        private async Task<TsResponse> GetAsyncWithReTry(string url, string token)
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add(TABLEAU_AUTH_HEADER, token);
            }

            HttpResponseMessage response = null;

            for (int i = 0; i < MaxRetries; i++)
            {
                response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    break;
                }
                await Task.Delay(delay);
            }

            string responseBody = await response.Content.ReadAsStringAsync();

            var tsResponse = XmlExtensions.Deserialize<TsResponse>(responseBody);

            return tsResponse;
        }
    }
}
