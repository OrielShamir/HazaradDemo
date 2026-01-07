using System;
using System.Diagnostics.CodeAnalysis;
using DatwiseSafetyDemo.Infrastructure;

namespace DatwiseSafetyDemo.Account
{
    [ExcludeFromCodeCoverage]
    public partial class Logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AuthTicketHelper.SignOut();
            Response.Redirect("~/Account/Login.aspx");
        }
    }
}
