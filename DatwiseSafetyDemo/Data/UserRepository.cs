using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DatwiseSafetyDemo.Models;

namespace DatwiseSafetyDemo.Data
{
    public interface IUserRepository
    {
        UserAuth GetAuthByUserName(string userName);

        /// <summary>
        /// Used for assignment dropdowns etc.
        /// </summary>
        IList<UserSummary> GetActiveUsersByRole(string role);
    }
    [ExcludeFromCodeCoverage]
    public class SqlUserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public SqlUserRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["HazardsConnection"].ConnectionString;
        }

        public UserAuth GetAuthByUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_GetUserAuthByUserName", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = userName.Trim();

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new UserAuth
                    {
                        UserId = (int)reader["UserId"],
                        UserName = (string)reader["UserName"],
                        FullName = (string)reader["FullName"],
                        Role = (string)reader["Role"],
                        IsActive = (bool)reader["IsActive"],
                        PasswordHash = reader["PasswordHash"] as byte[],
                        PasswordSalt = reader["PasswordSalt"] as byte[],
                        PasswordIterations = (int)reader["PasswordIterations"],
                        PasswordAlgorithm = (string)reader["PasswordAlgorithm"]
                    };
                }
            }
        }

        public IList<UserSummary> GetActiveUsersByRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return new List<UserSummary>();

            var results = new List<UserSummary>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_GetUsersByRole", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Role", SqlDbType.NVarChar, 50).Value = role;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new UserSummary
                        {
                            UserId = (int)reader["UserId"],
                            UserName = (string)reader["UserName"],
                            FullName = (string)reader["FullName"],
                            Role = (string)reader["Role"]
                        });
                    }
                }
            }

            return results;
        }
    }
}
