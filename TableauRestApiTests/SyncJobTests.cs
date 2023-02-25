using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TableauRestApiLib;
using TableauRestApiLib.Models;
using TableauSyncWebJob;
using TableauSyncWebJob.Models;

namespace TableauRestApiTests
{
    [TestClass]
    public class SyncJobTests
    {
        private static Mock<ILogger<RestApiService>> _mockLogger;
        private static Mock<ILogger<TableauSyncJob>> _mockSyncLogger;
        private static Mock<IRestApiService> _restApiService;
        private static Mock<IDisDataProvider> _disDataProvider;
        private static Mock<TableauCredentialsType> _credential;
        private string _siteId = "SiteId";

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _restApiService = new Mock<IRestApiService>();
            _disDataProvider = new Mock<IDisDataProvider>();
            _credential = new Mock<TableauCredentialsType>();
            _mockSyncLogger = new Mock<ILogger<TableauSyncJob>>();
        }

        [TestMethod]
        public async Task Can_Sync_Users_Groups()
        {
            _credential.Object.Token = "123456";
            _restApiService.Setup(m => m.SignInAsync("")).Returns(Task.FromResult(_credential.Object));
            _restApiService.Setup(m => m.CreateGroupAsync(_credential.Object, _siteId, It.IsAny<string>())).Returns(Task.FromResult(new GroupType() { Id = "Id", Name = "Group" }));
            _restApiService.Setup(m => m.CreateUserAsync(_credential.Object, _siteId, It.IsAny<string>())).Returns(Task.FromResult(new UserType() { Id = "Id", Name = "User" }));
            _restApiService.Setup(m => m.DeleteGroupAsync(_credential.Object, _siteId, It.IsAny<string>())).Returns(Task.FromResult(true));
            _restApiService.Setup(m => m.RemoveUserFromSiteAsync(_credential.Object, _siteId, It.IsAny<string>())).Returns(Task.FromResult(true));
            _restApiService.Setup(m => m.AddUserToGroupAsync(_credential.Object, _siteId, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(new UserType() { Id = "Id", Name = "User" }));
            _restApiService.Setup(m => m.RemoveUserFromGroupAsync(_credential.Object, _siteId, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(true));
            _restApiService.Setup(m => m.QueryUsersInSiteAsync(_credential.Object, _siteId)).Returns(GetTableauUsers());
            _restApiService.Setup(m => m.QueryGroupsInSiteAsync(_credential.Object, _siteId)).Returns(GetTableauGroups());
            _restApiService.Setup(m => m.QueryUsersInGroupAsync(_credential.Object, _siteId, It.IsAny<string>())).Returns(Task.FromResult(new UserListType()
            {
                User = new List<UserType>()
                                                                                                {
                                                                                                             new UserType {Id="1", Name= "user1@myapp.com" } ,
                                                                                                             new UserType {Id="3", Name = "user3@myapp.com" }
                                                                                                }
            }));

            _disDataProvider.Setup(d => d.GetUsersAsync()).Returns(Task.FromResult(GetDisUsers()));
            _disDataProvider.Setup(d => d.GetGroupsAsync()).Returns(Task.FromResult(GetGroups()));
            _disDataProvider.Setup(d => d.GetUsersByGroupAsync()).Returns(Task.FromResult(GetUsersByGroup()));


            var tableauSyncJob = new TableauSyncJob(_restApiService.Object, TableauConfigSettings,
                    SyncSettings, _mockSyncLogger.Object, _disDataProvider.Object);
            var syncStats = tableauSyncJob.RunAsync().Result;

            Assert.IsTrue(syncStats.UsersAddedToTableau == 3);
            Assert.IsTrue(syncStats.UsersRemovedFromTableau == 1);
            Assert.IsTrue(syncStats.GroupsAddedToTableau == 3);
            Assert.IsTrue(syncStats.GroupsRemovedFromTableau == 1);

            Assert.IsTrue(syncStats.UsersAddedToGroup["group1"]==1);
            Assert.IsTrue(syncStats.UsersRemovedFromGroup["group1"]== 1) ;
        }

        private List<User> GetDisUsers()
        {
            var userList = new List<User>()
            {
                new User { Id="1",Name="user1@myapp.com"},
                new User { Id="2", Name="user2@myapp.com"},
                new User { Id="3", Name="user3@myapp.com"},
                new User { Id="4", Name="user4@myapp.com"},
                new User { Id="5", Name="user5@myapp.com"},
            };
            return userList;
        }

        private List<Group> GetGroups()
        {
            var groupList = new List<Group>()
            {
                new Group { Id="1",Name="group1"},
                new Group { Id="2", Name="group2"},
                new Group { Id="3", Name="group3"},
                new Group { Id="4", Name="group4"},
                new Group { Id="5", Name="group5"},
            };
            return groupList;
        }

        private Dictionary<string, List<User>> GetUsersByGroup()
        {
            var dict = new Dictionary<string, List<User>>();
            dict.Add("group1", new List<User>() { new User { Id="1",Name="user1@myapp.com"},
                new User { Id="6", Name="user6@myapp.com"}});

            return dict;
        }

        private async Task<UserListType> GetTableauUsers()
        {
            var list = new List<UserType>()
            {
                new UserType {Id="1", Name = "user1@myapp.com", SiteRole=SiteRoleType.Viewer },
                new UserType {Id="3", Name = "user3@myapp.com", SiteRole=SiteRoleType.Viewer },
                new UserType {Id="6", Name = "user6@myapp.com", SiteRole=SiteRoleType.Viewer },
                new UserType {Id="7", Name = "user7@myapp.com", SiteRole=SiteRoleType.SiteAdministrator }
            };
            return new UserListType() { User = list };

        }

        private async Task<GroupListType> GetTableauGroups()
        {
            var list = new List<GroupType>()
            {
                new GroupType {Id="1", Name = "group1"},
                new GroupType {Id="3", Name = "Group3"},
                new GroupType {Id="7", Name = "Group7"}
            };
            return new GroupListType() { Group = list };
        }

        private IOptions<TableauConfigSettings> TableauConfigSettings
        {

            get
            {
                return Options.Create<TableauConfigSettings>(
                new TableauConfigSettings
                {
                    Username = "Username",
                    Password = "Password",
                    BaseUrl = "BaseUrl",
                    SiteId = "SiteId",
                    RemoveInactiveUsers = true
                }); ;
            }
        }

        private IOptions<SyncSettings> SyncSettings
        {
            get
            {
                return Options.Create<SyncSettings>(
                new SyncSettings
                {
                    ConnectionString = "ConnectionString",
                    SyncEnabled = true
                }); ;
            }
        }
    }
}
