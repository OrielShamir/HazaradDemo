using System;

namespace DatwiseSafetyDemo.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } // FieldWorker / SiteManager / SafetyOfficer
        public bool IsActive { get; set; }
    }

    public class UserAuth : User
    {
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public int PasswordIterations { get; set; }
        public string PasswordAlgorithm { get; set; } // PBKDF2-SHA1
    }
}
