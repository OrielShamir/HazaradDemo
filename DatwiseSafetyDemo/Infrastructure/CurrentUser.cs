using System;
using System.Text;
using System.Web;
using System.Web.Security;

namespace DatwiseSafetyDemo.Infrastructure
{
    public static class CurrentUser
    {
        // UserData format: userId|role|base64(fullName)
        public static bool TryGet(out int userId, out string role, out string fullName)
        {
            userId = 0;
            role = null;
            fullName = null;

            var ctx = HttpContext.Current;
            if (ctx == null || ctx.User == null || ctx.User.Identity == null || !ctx.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var formsIdentity = ctx.User.Identity as FormsIdentity;
            if (formsIdentity == null)
            {
                return false;
            }

            var ticket = formsIdentity.Ticket;
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.UserData))
            {
                return false;
            }

            var parts = ticket.UserData.Split('|');
            if (parts.Length < 2)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out userId))
            {
                return false;
            }

            role = parts[1];

            if (parts.Length >= 3)
            {
                try
                {
                    var bytes = Convert.FromBase64String(parts[2]);
                    fullName = Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    fullName = null;
                }
            }

            return true;
        }
    }
}
