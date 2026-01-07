using System;
using System.Web;
using System.Web.UI;

namespace DatwiseSafetyDemo.Infrastructure
{
    public abstract class SecurePage : Page
    {
        /// <summary>
        /// If empty/null: any authenticated user. Otherwise, only users with these roles.
        /// Roles: FieldWorker / SiteManager / SafetyOfficer
        /// </summary>
        protected virtual string[] AllowedRoles => null;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            // Basic CSRF mitigation for WebForms:
// Use an identity-stable key to avoid postback failures when SessionId changes (common in some dev setups).
// (Session-based keys are fine in typical IIS, but can be flaky behind certain proxies / VM setups.)
var userKey = (Context?.User?.Identity != null && Context.User.Identity.IsAuthenticated)
    ? (Context.User.Identity.Name ?? string.Empty)
    : string.Empty;

if (!string.IsNullOrWhiteSpace(userKey))
{
    ViewStateUserKey = userKey;
}
else if (Context?.Session != null)
{
    ViewStateUserKey = Session.SessionID;
}
if (Context?.User?.Identity == null || !Context.User.Identity.IsAuthenticated)
            {
                var returnUrl = HttpUtility.UrlEncode(Request.RawUrl);
                Response.Redirect($"~/Account/Login.aspx?ReturnUrl={returnUrl}", endResponse: true);
                return;
            }

            var roles = AllowedRoles;
            if (roles != null && roles.Length > 0)
            {
                var ok = false;
                foreach (var role in roles)
                {
                    if (Context.User.IsInRole(role))
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    Response.Redirect("~/Account/AccessDenied.aspx", endResponse: true);
                    return;
                }
            }

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
        }
    }
}
