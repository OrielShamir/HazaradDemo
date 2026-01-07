using System;
using DatwiseSafetyDemo.Data;
using DatwiseSafetyDemo.Models;

namespace DatwiseSafetyDemo.Services
{
    public interface IAuthenticationService
    {
        UserAuth Authenticate(string userName, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;

        public AuthenticationService(IUserRepository users, IPasswordHasher hasher)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        }

        public UserAuth Authenticate(string userName, string password)
        {
            var auth = _users.GetAuthByUserName(userName);
            if (auth == null || !auth.IsActive)
            {
                return null;
            }

            if (auth.PasswordSalt == null || auth.PasswordHash == null || auth.PasswordIterations <= 0)
            {
                return null;
            }

            var ok = _hasher.Verify(password, auth.PasswordSalt, auth.PasswordHash, auth.PasswordIterations);
            return ok ? auth : null;
        }
    }
}
