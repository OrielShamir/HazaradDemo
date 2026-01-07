using System;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Security.Principal;
using System.Threading;

namespace DatwiseSafetyDemo
{
    [ExcludeFromCodeCoverage]
    public class Global : HttpApplication
    {
                protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Infrastructure.SecurityHeaders.Apply(Response, Request.IsSecureConnection);
        }

protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null || string.IsNullOrWhiteSpace(authCookie.Value))
            {
                return;
            }

            FormsAuthenticationTicket ticket = null;
            try
            {
                ticket = FormsAuthentication.Decrypt(authCookie.Value);
            }
            catch
            {
                return;
            }

            if (ticket == null)
            {
                return;
            }

            string role = null;
            if (!string.IsNullOrWhiteSpace(ticket.UserData))
            {
                var parts = ticket.UserData.Split('|');
                if (parts.Length >= 2)
                {
                    role = parts[1];
                }
            }

            var identity = new FormsIdentity(ticket);
            var roles = string.IsNullOrWhiteSpace(role) ? new string[0] : new[] { role };
            var principal = new GenericPrincipal(identity, roles);

            Context.User = principal;
            Thread.CurrentPrincipal = principal;
        }

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
