using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace TableauSyncWebJob.Models
{
    /// <summary>Data provider for DIS users and groups</summary>
    /// <seealso cref="TableauSyncWebJob.Models.IDisDataProvider" />
    public class DisDataProvider : IDisDataProvider
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly SyncSettings _configuration;
        private int _commandTimeout = 360;
        public DisDataProvider(ILogger<DisDataProvider> logger, IOptions<SyncSettings> configuration)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _connectionString = _configuration.ConnectionString;
            _commandTimeout = _configuration.CommandTimeout;
        }

        /// <summary>Gets list of groups asynchronous.</summary>
        /// <returns>List of Groups</returns>
        public async Task<List<Group>> GetGroupsAsync()
        {
            _logger.LogDebug("Getting groups from DIS");

            var groups = new List<Group>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(
                  "SELECT CONVERT(NVARCHAR(50), Id) ID, Code FROM dbo.OperatingUnit(NOLOCK) WHERE IsDelete=0",
                  connection);
                command.CommandTimeout = _commandTimeout;
                await connection.OpenAsync();

                SqlDataReader reader =await command.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        groups.Add(new Group()
                        {
                            Id = reader["ID"] as string,
                            Name = reader["Code"] as string
                        });
                    }
                }
            }

            _logger.LogDebug($"Found {groups.Count} groups in DIS");
            return groups;
        }

        /// <summary>Gets DIS active users asynchronous.</summary>
        /// <returns>List of Users</returns>
        public async Task<List<User>> GetUsersAsync()
        {
            _logger.LogDebug("Getting users from DIS");

            var users = new List<User>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(
                  "SELECT DISTINCT CONVERT(NVARCHAR(50), v.UserId) ID, v.Email As Name,v.Email FROM [dbo].[VW_User_ALL_Claims_For_OU] v WHERE v.IsEnabled = 1",
                  connection);
                command.CommandTimeout = _commandTimeout;
                await connection.OpenAsync();

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new User()
                        {
                            Id = reader["ID"] as string,
                            Name = reader["Name"] as string,
                            Email = reader["Email"] as string
                        });
                    }
                }
            }
            _logger.LogDebug($"Found {users.Count} users in DIS");
            return users;

        }

        /// <summary>Gets users by group asynchronous.</summary>
        /// <returns>A dictionary of groups with users for each group</returns>
        public async Task<Dictionary<string, List<User>>> GetUsersByGroupAsync()
        {
            _logger.LogDebug("Getting user groups from DIS");

            var userGroups = new List<Tuple<string, string>>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                var sql = @"select DISTINCT v.Email UserId, Ou.Code as GroupName
                            FROM [dbo].[VW_User_ALL_Claims_For_OU] v
                            INNER JOIN[dbo].[OperatingUnit](NOLOCK) ou ON v.OUId = CONVERT(NVARCHAR(50), ou.Id)
                            WHERE ou.IsDelete=0 AND v.IsEnabled=1
                            ORDER BY ou.Code,v.email";

                SqlCommand command = new SqlCommand(sql, connection);
                command.CommandTimeout = _commandTimeout;
                await connection.OpenAsync();

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        userGroups.Add(new Tuple<string, string>(reader["UserId"] as string, reader["GroupName"] as string));
                    }
                }
            }

            var usersByGroup = userGroups
                             .GroupBy(x => x.Item2)  //Group Name
                             .ToDictionary(g => g.Key.ToLower(), g => g.Select(u => new User { Name = u.Item1 }).ToList());

            return usersByGroup;
        }
    }
}
