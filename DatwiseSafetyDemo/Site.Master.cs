using System;
using System.Web;
using System.Web.UI;
using DatwiseSafetyDemo.Infrastructure;

namespace DatwiseSafetyDemo
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var isAuth = Context?.User?.Identity != null && Context.User.Identity.IsAuthenticated;
            phAnon.Visible = !isAuth;
            phAuth.Visible = isAuth;

            if (isAuth && CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                litUser.Text = HttpUtility.HtmlEncode($"{fullName} ({role})");
            }
            else
            {
                litUser.Text = string.Empty;
            }
        }
    }
}
