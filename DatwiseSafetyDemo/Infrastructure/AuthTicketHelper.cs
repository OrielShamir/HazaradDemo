using System;
using System.Text;
using System.Web;
using System.Web.Security;

namespace DatwiseSafetyDemo.Infrastructure
{
    public static class AuthTicketHelper
    {
        public static void SignIn(HttpResponse response, string userName, int userId, string role, string fullName, bool isPersistent, TimeSpan? duration = null)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            var expires = DateTime.Now.Add(duration ?? TimeSpan.FromHours(8));
            var fullNameB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(fullName ?? string.Empty));
            var userData = $"{userId}|{role}|{fullNameB64}";

            var ticket = new FormsAuthenticationTicket(
                1,
                userName,
                DateTime.Now,
                expires,
                isPersistent,
                userData,
                FormsAuthentication.FormsCookiePath
            );

            var encrypted = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
            {
                HttpOnly = true,
                Secure = HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.IsSecureConnection,
                Path = FormsAuthentication.FormsCookiePath,
                Expires = isPersistent ? expires : DateTime.MinValue,
                SameSite = SameSiteMode.Lax
            };

            response.Cookies.Remove(cookie.Name);
            response.Cookies.Add(cookie);
        }

        public static void SignOut()
        {
            FormsAuthentication.SignOut();
        }
    }
}
