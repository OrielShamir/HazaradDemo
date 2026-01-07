using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DatwiseSafetyDemo.Models;

namespace DatwiseSafetyDemo.Data
{
    public sealed class HazardFilter
    {
        public string SearchText { get; set; }
        public string Status { get; set; }
        public string Severity { get; set; }
        public string Type { get; set; }
        public int? AssignedToUserId { get; set; }
        public bool OverdueOnly { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public interface IHazardRepository
    {
        IList<Hazard> GetHazards(HazardFilter filter, int currentUserId, string currentUserRole);
        Hazard GetById(int hazardId, int currentUserId, string currentUserRole);

        int Create(Hazard hazard, int performedByUserId);
        void UpdateDetails(Hazard hazard, int performedByUserId);

        void Assign(int hazardId, int assignedToUserId, int performedByUserId);
        void ChangeStatus(int hazardId, string newStatus, int performedByUserId, string details = null);

        IList<HazardLog> GetLogs(int hazardId, int currentUserId, string currentUserRole);
        DashboardMetrics GetDashboardMetrics(int currentUserId, string currentUserRole);
    }
    [ExcludeFromCodeCoverage]
    public class SqlHazardRepository : IHazardRepository
    {
        private readonly string _connectionString;

        public SqlHazardRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["HazardsConnection"].ConnectionString;
        }

        public IList<Hazard> GetHazards(HazardFilter filter, int currentUserId, string currentUserRole)
        {
            filter = filter ?? new HazardFilter();
            var hazards = new List<Hazard>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_GetHazards", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@CurrentUserId", SqlDbType.Int).Value = currentUserId;
                cmd.Parameters.Add("@CurrentUserRole", SqlDbType.NVarChar, 50).Value = (object)currentUserRole ?? DBNull.Value;

                cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar, 200).Value = (object)(filter.SearchText ?? string.Empty) ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = (object)filter.Status ?? DBNull.Value;
                cmd.Parameters.Add("@Severity", SqlDbType.NVarChar, 20).Value = (object)filter.Severity ?? DBNull.Value;
                cmd.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = (object)filter.Type ?? DBNull.Value;
                cmd.Parameters.Add("@AssignedToUserId", SqlDbType.Int).Value = (object)filter.AssignedToUserId ?? DBNull.Value;
                cmd.Parameters.Add("@OverdueOnly", SqlDbType.Bit).Value = filter.OverdueOnly;
                cmd.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = (object)filter.FromDate ?? DBNull.Value;
                cmd.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = (object)filter.ToDate ?? DBNull.Value;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        hazards.Add(MapHazard(reader));
                    }
                }
            }

            return hazards;
        }

        public Hazard GetById(int hazardId, int currentUserId, string currentUserRole)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_GetHazardById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@HazardId", SqlDbType.Int).Value = hazardId;
                cmd.Parameters.Add("@CurrentUserId", SqlDbType.Int).Value = currentUserId;
                cmd.Parameters.Add("@CurrentUserRole", SqlDbType.NVarChar, 50).Value = (object)currentUserRole ?? DBNull.Value;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return MapHazard(reader);
                }
            }
        }

        public int Create(Hazard hazard, int performedByUserId)
        {
            if (hazard == null) throw new ArgumentNullException(nameof(hazard));

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_CreateHazard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = hazard.Title ?? string.Empty;
                cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = hazard.Description ?? string.Empty;
                cmd.Parameters.Add("@ReportedByUserId", SqlDbType.Int).Value = hazard.ReportedByUserId;
                cmd.Parameters.Add("@Severity", SqlDbType.NVarChar, 20).Value = hazard.Severity ?? "Low";
                cmd.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = hazard.Type ?? "General";
                cmd.Parameters.Add("@DueDate", SqlDbType.DateTime).Value = (object)hazard.DueDate ?? DBNull.Value;

                cmd.Parameters.Add("@PerformedByUserId", SqlDbType.Int).Value = performedByUserId;

                var outId = cmd.Parameters.Add("@HazardId", SqlDbType.Int);
                outId.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return (int)outId.Value;
            }
        }

        public void UpdateDetails(Hazard hazard, int performedByUserId)
        {
            if (hazard == null) throw new ArgumentNullException(nameof(hazard));

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_UpdateHazard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@HazardId", SqlDbType.Int).Value = hazard.HazardId;
                cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = hazard.Title ?? string.Empty;
                cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = hazard.Description ?? string.Empty;
                cmd.Parameters.Add("@Severity", SqlDbType.NVarChar, 20).Value = hazard.Severity ?? "Low";
                cmd.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = hazard.Type ?? "General";
                cmd.Parameters.Add("@DueDate", SqlDbType.DateTime).Value = (object)hazard.DueDate ?? DBNull.Value;

                cmd.Parameters.Add("@PerformedByUserId", SqlDbType.Int).Value = performedByUserId;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Assign(int hazardId, int assignedToUserId, int performedByUserId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_AssignHazard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@HazardId", SqlDbType.Int).Value = hazardId;
                cmd.Parameters.Add("@AssignedToUserId", SqlDbType.Int).Value = assignedToUserId;
                cmd.Parameters.Add("@PerformedByUserId", SqlDbType.Int).Value = performedByUserId;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void ChangeStatus(int hazardId, string newStatus, int performedByUserId, string details = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_ChangeHazardStatus", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@HazardId", SqlDbType.Int).Value = hazardId;
                cmd.Parameters.Add("@NewStatus", SqlDbType.NVarChar, 20).Value = (object)newStatus ?? DBNull.Value;
                cmd.Parameters.Add("@PerformedByUserId", SqlDbType.Int).Value = performedByUserId;
                cmd.Parameters.Add("@Details", SqlDbType.NVarChar, 2000).Value = (object)details ?? DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public IList<HazardLog> GetLogs(int hazardId, int currentUserId, string currentUserRole)
        {
            var logs = new List<HazardLog>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_GetHazardLogs", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@HazardId", SqlDbType.Int).Value = hazardId;
                cmd.Parameters.Add("@CurrentUserId", SqlDbType.Int).Value = currentUserId;
                cmd.Parameters.Add("@CurrentUserRole", SqlDbType.NVarChar, 50).Value = (object)currentUserRole ?? DBNull.Value;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new HazardLog
                        {
                            HazardLogId = (int)reader["HazardLogId"],
                            HazardId = (int)reader["HazardId"],
                            ActionType = (string)reader["ActionType"],
                            Details = reader["Details"] as string,
                            PerformedByUserId = (int)reader["PerformedByUserId"],
                            PerformedByName = reader["PerformedByName"] as string,
                            CreatedAt = (DateTime)reader["CreatedAt"]
                        });
                    }
                }
            }

            return logs;
        }

        public DashboardMetrics GetDashboardMetrics(int currentUserId, string currentUserRole)
        {
            var metrics = new DashboardMetrics();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.usp_GetDashboardMetrics", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@CurrentUserId", SqlDbType.Int).Value = currentUserId;
                cmd.Parameters.Add("@CurrentUserRole", SqlDbType.NVarChar, 50).Value = (object)currentUserRole ?? DBNull.Value;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    // Result set 1: main KPIs
                    if (reader.Read())
                    {
                        metrics.OpenCount = (int)reader["OpenCount"];
                        metrics.InProgressCount = (int)reader["InProgressCount"];
                        metrics.ResolvedCount = (int)reader["ResolvedCount"];
                        metrics.OverdueOpenCount = ReadIntSafe(reader, "OverdueOpenCount", ReadIntSafe(reader, "OverdueCount"));
                    }

                    // Result set 2: by severity
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var key = (string)reader["Key"];
                            var count = (int)reader["Count"];
                            metrics.BySeverity[key] = count;
                        }
                    }

                    // Result set 3: by type
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var key = (string)reader["Key"];
                            var count = (int)reader["Count"];
                            metrics.ByType[key] = count;
                        }
                    }
                }
            }

            return metrics;
        }

private static int ReadIntSafe(IDataRecord reader, string columnName, int defaultValue = 0)
{
    try
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return defaultValue;
        return Convert.ToInt32(reader.GetValue(ordinal));
    }
    catch (IndexOutOfRangeException)
    {
        return defaultValue;
    }
}

        private static Hazard MapHazard(IDataRecord reader)
        {
            return new Hazard
            {
                HazardId = (int)reader["HazardId"],
                Title = (string)reader["Title"],
                Description = (string)reader["Description"],
                ReportedByUserId = (int)reader["ReportedByUserId"],
                AssignedToUserId = reader["AssignedToUserId"] == DBNull.Value ? (int?)null : (int)reader["AssignedToUserId"],
                Status = (string)reader["Status"],
                Severity = (string)reader["Severity"],
                Type = (string)reader["Type"],
                CreatedAt = (DateTime)reader["CreatedAt"],
                DueDate = reader["DueDate"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["DueDate"],
                ReportedByName = reader["ReportedByName"] as string,
                AssignedToName = reader["AssignedToName"] as string
            };
        }
    }
}
