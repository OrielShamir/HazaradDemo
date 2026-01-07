using Microsoft.VisualStudio.TestTools.UnitTesting;
using DatwiseSafetyDemo.Infrastructure;
using DatwiseSafetyDemo.Models;
using DatwiseSafetyDemo.Services;

namespace DatwiseSafetyDemo.Tests
{
    [TestClass]
    public class HazardAuthorizationTests
    {
        [TestMethod]
        public void SafetyOfficer_CanView_AnyHazard()
        {
            var hazard = new Hazard { ReportedByUserId = 10, AssignedToUserId = null, Status = "Open" };
            Assert.IsTrue(HazardAuthorization.CanView(Roles.SafetyOfficer, 999, hazard));
        }

        [TestMethod]
        public void FieldWorker_CanView_WhenReportedByOrAssigned()
        {
            var reported = new Hazard { ReportedByUserId = 5, AssignedToUserId = null, Status = "Open" };
            var assigned = new Hazard { ReportedByUserId = 1, AssignedToUserId = 5, Status = "InProgress" };
            var other = new Hazard { ReportedByUserId = 1, AssignedToUserId = 2, Status = "Open" };

            Assert.IsTrue(HazardAuthorization.CanView(Roles.FieldWorker, 5, reported));
            Assert.IsTrue(HazardAuthorization.CanView(Roles.FieldWorker, 5, assigned));
            Assert.IsFalse(HazardAuthorization.CanView(Roles.FieldWorker, 5, other));
        }

        [TestMethod]
        public void SiteManager_CanView_OpenUnassigned()
        {
            var openUnassigned = new Hazard { ReportedByUserId = 1, AssignedToUserId = null, Status = "Open" };
            var inProgressUnassigned = new Hazard { ReportedByUserId = 1, AssignedToUserId = null, Status = "InProgress" };

            Assert.IsTrue(HazardAuthorization.CanView(Roles.SiteManager, 7, openUnassigned));
            Assert.IsFalse(HazardAuthorization.CanView(Roles.SiteManager, 7, inProgressUnassigned));
        }

        [TestMethod]
        public void CanAddComment_MatchesRolePolicy()
        {
            Assert.IsTrue(HazardAuthorization.CanAddComment(Roles.SafetyOfficer));
            Assert.IsTrue(HazardAuthorization.CanAddComment(Roles.SiteManager));
            Assert.IsTrue(HazardAuthorization.CanAddComment(Roles.FieldWorker));
            Assert.IsFalse(HazardAuthorization.CanAddComment("Unknown"));
        }
    }

    [TestClass]
    public class PasswordHasherTests
    {
        [TestMethod]
        public void Hash_ThenVerify_Succeeds_ForCorrectPassword()
        {
            var hasher = new Pbkdf2PasswordHasher();

            var result = hasher.Hash("Password123!", iterations: 10_000);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Salt);
            Assert.IsNotNull(result.Hash);
            Assert.IsTrue(hasher.Verify("Password123!", result.Salt, result.Hash, result.Iterations));
        }

        [TestMethod]
        public void Verify_Fails_ForWrongPassword()
        {
            var hasher = new Pbkdf2PasswordHasher();

            var result = hasher.Hash("Password123!", iterations: 10_000);
            Assert.IsFalse(hasher.Verify("WrongPassword", result.Salt, result.Hash, result.Iterations));
        }
    }
}
