using System;
using DatwiseSafetyDemo.Models;

namespace DatwiseSafetyDemo.Infrastructure
{
    /// <summary>
    /// Centralized authorization rules for hazards (server-side RBAC).
    /// Keep this class free of HttpContext so it is unit-testable.
    /// </summary>
    public static class HazardAuthorization
    {
        public static bool CanView(string role, int currentUserId, Hazard hazard)
        {
            if (hazard == null) return false;

            if (string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(role, Roles.FieldWorker, StringComparison.OrdinalIgnoreCase))
                return hazard.ReportedByUserId == currentUserId || hazard.AssignedToUserId == currentUserId;

            if (string.Equals(role, Roles.SiteManager, StringComparison.OrdinalIgnoreCase))
            {
                // Site manager can see:
                // 1) hazards assigned to them
                // 2) hazards they reported
                // 3) open+unassigned hazards (to allow taking ownership)
                return hazard.AssignedToUserId == currentUserId
                       || hazard.ReportedByUserId == currentUserId
                       || (hazard.AssignedToUserId == null && IsStatus(hazard.Status, "Open"));
            }

            return false;
        }

        public static bool CanEditDetails(string role, int currentUserId, Hazard hazard)
        {
            if (!CanView(role, currentUserId, hazard)) return false;

            if (string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(role, Roles.FieldWorker, StringComparison.OrdinalIgnoreCase))
            {
                // Field worker can edit only their own hazards, while still open & not assigned.
                return hazard.ReportedByUserId == currentUserId
                       && hazard.AssignedToUserId == null
                       && IsStatus(hazard.Status, "Open");
            }

            if (string.Equals(role, Roles.SiteManager, StringComparison.OrdinalIgnoreCase))
            {
                // Site manager can edit if:
                // - assigned to them OR they reported it
                return hazard.AssignedToUserId == currentUserId || hazard.ReportedByUserId == currentUserId;
            }

            return false;
        }

        public static bool CanSelfAssign(string role, int currentUserId, Hazard hazard)
        {
            if (hazard == null) return false;
            if (!string.Equals(role, Roles.SiteManager, StringComparison.OrdinalIgnoreCase)) return false;

            return hazard.AssignedToUserId == null && IsStatus(hazard.Status, "Open");
        }

        public static bool CanAssignToOther(string role)
        {
            return string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanChangeStatus(string role, int currentUserId, Hazard hazard, string newStatus)
        {
            if (hazard == null) return false;
            if (!CanView(role, currentUserId, hazard)) return false;

            if (string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase))
                return IsKnownStatus(newStatus);

            if (string.Equals(role, Roles.SiteManager, StringComparison.OrdinalIgnoreCase))
            {
                if (hazard.AssignedToUserId != currentUserId) return false;

                // Strict transitions:
                // Open -> InProgress -> Resolved
                if (IsStatus(hazard.Status, "Open") && IsStatus(newStatus, "InProgress")) return true;
                if (IsStatus(hazard.Status, "InProgress") && IsStatus(newStatus, "Resolved")) return true;

                return false;
            }

            // FieldWorker: no status changes
            return false;
        }

        public static string[] AllowedNextStatuses(string role, int currentUserId, Hazard hazard)
        {
            if (hazard == null) return Array.Empty<string>();

            if (string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase))
            {
                // Safety officer can set any known status (including current) for simplicity.
                return new[] { "Open", "InProgress", "Resolved" };
            }

            if (string.Equals(role, Roles.SiteManager, StringComparison.OrdinalIgnoreCase))
            {
                if (hazard.AssignedToUserId != currentUserId) return Array.Empty<string>();

                if (IsStatus(hazard.Status, "Open")) return new[] { "InProgress" };
                if (IsStatus(hazard.Status, "InProgress")) return new[] { "Resolved" };
                return Array.Empty<string>();
            }

            return Array.Empty<string>();
        }

        public static bool CanAddComment(string role)
        {
            // All authenticated roles can add comments (audited).
            return string.Equals(role, Roles.FieldWorker, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(role, Roles.SiteManager, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStatus(string status, string expected)
        {
            return string.Equals(status ?? string.Empty, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsKnownStatus(string status)
        {
            return IsStatus(status, "Open") || IsStatus(status, "InProgress") || IsStatus(status, "Resolved");
        }
    }
}
