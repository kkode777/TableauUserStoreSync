using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using TableauRestApiLib;
using TableauRestApiLib.Models;

namespace TableauRestApiTests
{
    [TestClass]
    public class RestApiTests
    {
        private  static Mock<ILogger<RestApiService>> _mockLogger;
        private  static IRestApiService _restApiService;
        public  static string _siteId;
        public static TableauCredentialsType _credentials;
        public  static string _timeStampSuffix = DateTime.UtcNow.Ticks.ToString();
        public  static string _newGroupId;
        public  static string _newUserId;
        public static IConfigurationRoot _configurationRoot;
        public static  IConfigurationSection _tableauConfigSettings;
        [ClassInitialize]
        public  static void ClassInitialize(TestContext context)
        {
            _configurationRoot = TestsHelper.GetIConfigurationRoot();
            _tableauConfigSettings = _configurationRoot.GetSection("TableauConfigSettings");
            _mockLogger = new Mock<ILogger<RestApiService>>();
            _restApiService = new RestApiService(TableauConfigSettings, _mockLogger.Object);
            _credentials = _restApiService.SignInAsync("").Result;
            _siteId = _tableauConfigSettings["SiteId"];
        }

        private  async Task SeedData()
        {
            var newGroup = await _restApiService.CreateGroupAsync(_credentials, _siteId, $"testgroup-{_timeStampSuffix}");
            var newUser = await _restApiService.CreateUserAsync(_credentials, _siteId, $"testuser-{_timeStampSuffix}");
            _newGroupId = newGroup.Id;
            _newUserId = newUser.Id;
        }

        [TestMethod]
        public async Task A_Can_Get_Users()
        {
            var response = await _restApiService.QueryUsersInSiteAsync(_credentials, _siteId);
            Assert.IsTrue(response.User.Count > 0);
        }

        [TestMethod]
        public async Task B_Can_Get_Groups()
        {
            var response = await _restApiService.QueryGroupsInSiteAsync(_credentials, _siteId);
            Assert.IsTrue(response.Group.Count > 0);
        }

        [TestMethod]
        public async Task C_Can_Add_Group_To_Site()
        {
            var newGroup = await _restApiService.CreateGroupAsync(_credentials, _siteId, $"testgroup-{_timeStampSuffix}");
            Assert.IsTrue(newGroup != null);
            _newGroupId = newGroup.Id;
        }

        [TestMethod]
        public async Task D_Can_Add_User_To_Site()
        {
            var newUser = await _restApiService.CreateUserAsync(_credentials, _siteId, $"testuser-{_timeStampSuffix}");
            Assert.IsTrue(newUser != null);
            _newUserId = newUser.Id;
        }

        [TestMethod]
        public async Task E_Can_Add_User_To_Group()
        {
            var newUser = await _restApiService.AddUserToGroupAsync(_credentials, _siteId, _newGroupId, _newUserId);
            Assert.IsTrue(newUser != null);
        }

        [TestMethod]
        public async Task F_Can_Remove_User_From_Group()
        {
            var result = await _restApiService.RemoveUserFromGroupAsync(_credentials, _siteId, _newGroupId, _newUserId);
            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public async Task G_Can_Remove_User_From_Site()
        {
            var result = await _restApiService.RemoveUserFromSiteAsync(_credentials, _siteId, _newUserId);
            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public async Task H_Can_Remove_Group_From_Site()
        {
            var result = await _restApiService.DeleteGroupAsync(_credentials, _siteId, _newGroupId);
            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public async Task Z_Can_Signout()
        {
            var response = await _restApiService.SignOutAsync(_credentials);
            Assert.IsTrue(response == true);
        }
        private static IOptions<TableauConfigSettings> TableauConfigSettings
        {
            
            get
            {
                return Options.Create<TableauConfigSettings>(
                new TableauConfigSettings
                {
                    Username = _tableauConfigSettings["Username"],
                    Password = _tableauConfigSettings["Password"],
                    BaseUrl = _tableauConfigSettings["BaseUrl"],
                    SiteId= _tableauConfigSettings["SiteId"],
                    RemoveInactiveUsers=bool.Parse(_tableauConfigSettings["RemoveInactiveUsers"])
                });
            }
        }
    }
}
