using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableauRestApiLib;
using TableauRestApiLib.Models;

namespace TableauSyncWebJob.Models
{
    public class TableauSyncJob
    {
        private readonly ILogger _logger;
        private readonly IRestApiService _restApiService;
        public readonly TableauConfigSettings _tableauConfigSettings;
        public readonly SyncSettings _syncSettings;
        private TableauCredentialsType _token;
        private string _siteId;
        private readonly IDisDataProvider _disDataProvider;
        private SyncStats _syncStats = new SyncStats();
        public TableauSyncJob(IRestApiService restApiService,
            IOptions<TableauConfigSettings> tableauSettings,
            IOptions<SyncSettings> syncSettings,
            ILogger<TableauSyncJob> logger,
            IDisDataProvider disDataProvider)
        {
            _logger = logger;
            _restApiService = restApiService;
            _tableauConfigSettings = tableauSettings.Value;
            _syncSettings = syncSettings.Value;
            _disDataProvider = disDataProvider;
            _logger.LogInformation("Tableau Sync Job called.");
        }

        /// <summary>Entry Point for the Web Job</summary>
        /// <returns>SyncStats</returns>
        public async Task<SyncStats> RunAsync()
        {
            try
            {
                _logger.LogInformation($"Tableau Sync Job triggered.");

                //Validate settings
                if (!ValidateTableauSettings())
                {
                    _logger.LogError("One ore more Tableau settings are not configured correctly. Quitting the job.");
                    return _syncStats;
                }
                if (_restApiService == null)
                {
                    _logger.LogError("REST Api service is null. Quitting the job.");
                    return _syncStats;
                }

                _logger.LogInformation($"Settings validated. Tableau Sync Job started for server: {_tableauConfigSettings.BaseUrl} and site id: {_tableauConfigSettings.SiteId}");
                //Initialize siteId and token
                _siteId = _tableauConfigSettings.SiteId;

                _token = await _restApiService.SignInAsync("");

                if (_token?.Token != null)
                {
                    _logger.LogInformation("Tableau signin success. Retrieved token");
                }
                else
                {
                    _logger.LogError("Could not signin to Tableau.");
                    return _syncStats;
                }

                if (!_syncSettings.SyncEnabled)
                {
                    _logger.LogInformation("Sync option is disabled. Skipping sync");
                    return _syncStats;
                }
                await SyncUsersAndGroupsAsync();
                _logger.LogInformation("Tableau Sync Job completed.");

                _logger.LogInformation("Signing out of Tableau.");
                await _restApiService.SignOutAsync(_token);
                _logger.LogInformation("Signout of Tableau complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Tableau Sync Job encountered an error with stack trace: - {ex.StackTrace}.");
            }
            return _syncStats;

        }

        private bool ValidateTableauSettings()
        {
            if (string.IsNullOrEmpty(_tableauConfigSettings.BaseUrl) ||
                string.IsNullOrEmpty(_tableauConfigSettings.Username) ||
                string.IsNullOrEmpty(_tableauConfigSettings.Password) ||
                string.IsNullOrEmpty(_tableauConfigSettings.SiteId))
            {
                return false;
            }
            return true;
        }

        /// <summary>Synchronizes the users and groups asynchronous.</summary>
        /// Pulls users and groups from Tableau and DIS and determines the users and groups to add or remove from Tableau.
        private async Task SyncUsersAndGroupsAsync()
        {
            _logger.LogInformation($"Sync User and Groups Begin..");
            //Get users and groups from both sources.
            var tableauGroups = await GetTableauGroupsAsync();
            _logger.LogInformation($"Found-{tableauGroups.Count()} groups in Tableau");

            var tableauUsers = await GetTableauUsersAsync();
            _logger.LogInformation($"Found-{tableauUsers.Count()} users in Tableau");

            var disGroups = await _disDataProvider.GetGroupsAsync();
            _logger.LogInformation($"Found-{disGroups.Count()} groups in DIS");

            var disUsers = await _disDataProvider.GetUsersAsync();
            _logger.LogInformation($"Found-{disUsers.Count()} users in DIS");

            //Groups to add In Tableau
            var groupsToAdd = disGroups.Except(tableauGroups).ToList();
            _logger.LogInformation($"Groups to add in Tableau: {groupsToAdd.Count()}");

            //Groups to remove in Tableau  --Exclude All Users group
            var groupsToRemove = tableauGroups.Except(disGroups).Where(g => g.Name != "All Users").ToList();
            _logger.LogInformation($"Groups to remove in Tableau: {groupsToRemove.Count()}");

            //Users to add in Tableau
            var usersToAdd = disUsers.Except(tableauUsers).ToList();
            _logger.LogInformation($"Users to add in Tableau: {usersToAdd.Count()}");

            //Users to remove in Tableau --Exclude system and site administrators
            var excludedRolesFromDelete = new List<SiteRoleType>() {SiteRoleType.ServerAdministrator,
                                                              SiteRoleType.SiteAdministratorCreator,
                                                              SiteRoleType.SiteAdministratorExplorer,
                                                              SiteRoleType.SiteAdministrator};

            var excludedUsers = new List<User>() { new User { Name = "guest" } };

            var usersToRemove = tableauUsers.Except(disUsers).Except(excludedUsers).
                Where(u => (!excludedRolesFromDelete.Contains(u.SiteRole.Value))).ToList();
            _logger.LogInformation($"Users to remove in Tableau: {usersToRemove.Count()}");

            //Sync users and groups
            //Inactive users will not be authenticated by ID server and so will not be able to access Tableau. Removing users in Tableau is optional .
            if (_tableauConfigSettings.RemoveInactiveUsers)
            {
                var usersRemoved = await RemoveUsersFromSiteAsync(usersToRemove);
                _syncStats.UsersRemovedFromTableau = usersRemoved;
                _logger.LogInformation("Complete - Remove users from Tableau Site");
            }
            else
            {
                _logger.LogInformation("Skipping the step - remove users from Tableau Site");
            }

            if (groupsToRemove.Any())
            {
                var groupsRemovedCount = await RemoveGroupsFromSiteAsync(groupsToRemove);
                _syncStats.GroupsRemovedFromTableau = groupsRemovedCount;
                _logger.LogInformation($"Removed {groupsRemovedCount} groups from Tableau Site");
            }

            if (usersToAdd.Any())
            {
                var usersAddedCount = await AddUsersToSiteAsync(usersToAdd);
                _syncStats.UsersAddedToTableau = usersAddedCount;
                _logger.LogInformation($"Added {usersAddedCount} users to Tableau Site");
            }

            if (groupsToAdd.Any())
            {
                var groupsAddedCount = await AddGroupsToSiteAsync(groupsToAdd);
                _syncStats.GroupsAddedToTableau = groupsAddedCount;
                _logger.LogInformation($"Added {groupsAddedCount} groups to Tableau Site");
            }

            await SyncUsersToGroupsAsync(disGroups, disUsers);

            _logger.LogInformation($"Sync User and Groups completed..");
        }

        /// <summary>Synchronizes users for each group asynchronous.</summary>
        /// <param name="disGroups">The dis groups.</param>
        /// <param name="disUsers">The dis users.</param>
        private async Task SyncUsersToGroupsAsync(List<Group> disGroups, List<User> disUsers)
        {
            _logger.LogInformation($"Sync User by each group begin..");

            _syncStats.UsersAddedToGroup = new Dictionary<string, int>();
            _syncStats.UsersRemovedFromGroup = new Dictionary<string, int>();

            //Pull the current list of groups from Tableau.
            var tableauGroups = await GetTableauGroupsAsync();

            //Create a dictionary to quickly pull group id by name. All Tableau operations are done using Id.
            var tableauGroupsDict = tableauGroups.ToDictionary(g => g.Name.ToLower(), g => g.Id);

            //Pull users for each group and add them to a dictionary with groupname as key.
            var tableauUsersByGroupDict = await GetTableauUsersByGroupAsync(tableauGroups);

            //Pull DIS users by group (OU)
            var disUsersByGroupDict = await _disDataProvider.GetUsersByGroupAsync();

            //Pulling the list of all users as we need the ID to add to a group.
            var tableauUsers = await GetTableauUsersAsync();
            var tableauUsersDict = tableauUsers.GroupBy(u => u.Name)
                                             .ToDictionary(g => g.Key.ToLower(), g => g.Select(u => u.Id).FirstOrDefault());

            _logger.LogInformation($"Number of DIS Groups to sync - {disUsersByGroupDict.Keys.Count}");
            var groupCounter = 0;
            foreach (var key in disUsersByGroupDict.Keys)
            {
                _logger.LogInformation($"Counter: {++groupCounter} - Syncing group  {key}");
                if (tableauUsersByGroupDict.ContainsKey(key))
                {
                    var usersInDisGroup = disUsersByGroupDict[key];
                    var usersInTableauGroup = tableauUsersByGroupDict[key];

                    //users to add to group
                    var toAdd = usersInDisGroup.Except(usersInTableauGroup).ToList();

                    //users to remove from gropup
                    var toRemove = usersInTableauGroup.Except(usersInDisGroup).ToList();

                    if (!toAdd.Any() && !toRemove.Any())
                    {
                        _logger.LogInformation($"Users in group -{key} are already in sync. Skipping. ");
                        continue;
                    }

                    var groupId = tableauGroupsDict[key];
                    if (toAdd.Any())
                    {
                        var addedCount = await AddUsersToTableauGroupAsync(key, groupId, toAdd, tableauUsersDict);
                        _logger.LogInformation($"Added {addedCount} users to group {key} ");
                        _syncStats.UsersAddedToGroup.Add(key, addedCount);
                    }
                    if (toRemove.Any())
                    {
                        var removedCount = await RemoveUsersFromTableauGroupAsync(key, groupId, toRemove);
                        _logger.LogInformation($"Removed {removedCount} users from group {key} ");
                        _syncStats.UsersRemovedFromGroup.Add(key, removedCount);
                    }
                }
                else
                {
                    _logger.LogInformation($"Group: {key} not found in Tableau. Sync skipped");
                }
            }
            _logger.LogInformation($"Sync User by each group end..");
        }
       
        private async Task<Dictionary<string, List<User>>> GetTableauUsersByGroupAsync(List<Group> tableauGroups)
        {
            var tableauGroupsDict = new Dictionary<string, List<User>>();

            //for each Tableau group, get users from Tableau
            foreach (var group in tableauGroups)
            {
                var userListType = await _restApiService.QueryUsersInGroupAsync(_token, _siteId, group.Id);
                var usersByGroup = userListType.User.Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email
                }).ToList();
                tableauGroupsDict.Add(group.Name.ToLower(), usersByGroup);
            }
            return tableauGroupsDict;
        }

        private async Task<int> AddUsersToSiteAsync(List<User> users)
        {
            _logger.LogInformation($"Attempting to add {users.Count} users to site");
            var added = 0;
            foreach (var user in users)
            {
                var response = await _restApiService.CreateUserAsync(_token, _siteId, user.Name);
                if (response?.Id != null)
                {
                    _logger.LogInformation($"Added user - {user.Name} to site.");
                    added++;
                }
                else
                {
                    _logger.LogError($"Failed to added user - {user.Name} to site.");
                }
            }
            return added;
        }

        private async Task<int> RemoveUsersFromSiteAsync(List<User> usersToRemove)
        {
            var removed = 0;
            foreach (var user in usersToRemove)
            {
                _logger.LogDebug($"Attempting to remove user -{user.Name} from site");
                if (await _restApiService.RemoveUserFromSiteAsync(_token, _siteId, user.Id))
                {
                    _logger.LogInformation($"Successfully removed user -{user.Name} from site");
                    removed++;
                }
                else
                {
                    _logger.LogError($"Failed to remove user -{user.Name} from site");
                }
            }
            return removed;
        }

        private async Task<int> AddGroupsToSiteAsync(List<Group> groupsToAdd)
        {
            var added = 0;
            foreach (var group in groupsToAdd)
            {
                var response = await _restApiService.CreateGroupAsync(_token, _siteId, group.Name);
                if (response?.Id != null)
                {
                    _logger.LogInformation($"Successfully added group -{group.Name} to site.");
                    added++;
                }
                else
                {
                    _logger.LogError($"Failed to add group -{group.Name} to site.");
                }
            }
            return added;
        }

        private async Task<int> RemoveGroupsFromSiteAsync(List<Group> groupsToRemove)
        {
            var removed = 0;
            foreach (var group in groupsToRemove)
            {
                if (await _restApiService.DeleteGroupAsync(_token, _siteId, group.Id))
                {
                    _logger.LogInformation($"Successfully removed group -{group.Name} from site.");
                    removed++;
                }
                else
                {
                    _logger.LogError($"Failed to remove group -{group.Name} from site.");
                }
            }
            return removed;
        }

        private async Task<int> AddUsersToTableauGroupAsync(string groupName, string groupId, List<User> users, Dictionary<string, string> tableauUsersDict)
        {
            _logger.LogInformation($"Attempting to add {users.Count} users to group - {groupName}");
            var addedCount = 0;
            foreach (var user in users)
            {
                if (tableauUsersDict.ContainsKey(user.Name.ToLower()))
                {
                    var tableauUserId = tableauUsersDict[user.Name.ToLower()];
                    var response = await _restApiService.AddUserToGroupAsync(_token, _siteId, groupId, tableauUserId);
                    if (response?.Id != null)
                    {
                        addedCount++;
                        _logger.LogInformation($"Successfully added user - {user.Name} to group - {groupName}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to add user -{user.Name} to group -{groupName}");
                    }
                }
                else
                {
                    _logger.LogError($"Failed to add user -{user.Name} to group -{groupName} . User not found in Tableau");
                }

            }
            _logger.LogInformation($"Added {addedCount} users to group - {groupName}");
            return addedCount;
        }

        private async Task<int> RemoveUsersFromTableauGroupAsync(string groupName, string groupId, List<User> users)
        {
            _logger.LogInformation($"Attempting to remove {users.Count} users from group - {groupName}");
            var removedCount = 0;
            foreach (var user in users)
            {
                var removed = await _restApiService.RemoveUserFromGroupAsync(_token, _siteId, groupId, user.Id);
                if (removed)
                {
                    removedCount++;
                    _logger.LogInformation($"Successfully removed user - {user.Name} from group - {groupName}");
                }
                else
                {
                    _logger.LogError($"Failed to remove user -{user.Name} from group -{groupName}");
                }
            }
            _logger.LogInformation($"Removed {removedCount} users from group - {groupName}");
            return removedCount;
        }

        private async Task<List<Group>> GetTableauGroupsAsync()
        {
            _logger.LogDebug($"Retrieving groups from Tableau");

            var tableauGroupsListType = await _restApiService.QueryGroupsInSiteAsync(_token, _siteId);

            var tableauGroups = tableauGroupsListType.Group
                .Select(g => new Group
                {
                    Id = g.Id,
                    Name = g.Name?.Trim()
                }).ToList();
            _logger.LogDebug($"Retrieved {tableauGroups.Count} groups from Tableau");
            return tableauGroups;
        }

        private async Task<List<User>> GetTableauUsersAsync()
        {
            _logger.LogDebug($"Retrieving users from Tableau");
            var tableauUsersListType = await _restApiService.QueryUsersInSiteAsync(_token, _siteId);
            var tableauUsers = tableauUsersListType.User
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name?.Trim(),
                    Email = u.Email?.Trim(),
                    SiteRole = u.SiteRole
                }).ToList();

            _logger.LogDebug($"Retrieved {tableauUsers.Count} users from Tableau");
            return tableauUsers;
        }

    }
}

