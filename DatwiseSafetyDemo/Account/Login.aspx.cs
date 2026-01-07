using System;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using DatwiseSafetyDemo.Data;
using DatwiseSafetyDemo.Infrastructure;
using DatwiseSafetyDemo.Services;

namespace DatwiseSafetyDemo.Account
{
    [ExcludeFromCodeCoverage]
    public partial class Login : System.Web.UI.Page
    {
        private readonly IAuthenticationService _auth;
        private static readonly LoginRateLimiter RateLimiter = new LoginRateLimiter();

        public Login()
        {
            _auth = new AuthenticationService(new SqlUserRepository(), new Pbkdf2PasswordHasher());
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Context.User != null && Context.User.Identity != null && Context.User.Identity.IsAuthenticated)
            {
                // Already signed in
                RedirectToReturnUrl();
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            var userName = (txtUserName.Text ?? string.Empty).Trim();
            var password = txtPassword.Text ?? string.Empty;

            var key = BuildRateLimitKey(userName);

            if (RateLimiter.IsBlocked(key, out var retryAfter))
            {
                lblError.Text = $"Too many attempts. Please try again in {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minute(s).";
                lblError.Visible = true;
                return;
            }

            var authUser = _auth.Authenticate(userName, password);
            if (authUser == null)
            {
                RateLimiter.RegisterFailure(key);

                // Do not reveal whether the username exists.
                lblError.Text = "Invalid username or password.";
                lblError.Visible = true;
                return;
            }

            RateLimiter.RegisterSuccess(key);

            AuthTicketHelper.SignIn(Response, authUser.UserName, authUser.UserId, authUser.Role, authUser.FullName, isPersistent: false);

            RedirectToReturnUrl();
        }

        private string BuildRateLimitKey(string userName)
        {
            var ip = Request?.UserHostAddress ?? "unknown";
            var u = string.IsNullOrWhiteSpace(userName) ? "unknown" : userName.ToLowerInvariant();
            return ip + "|" + u;
        }

        private void RedirectToReturnUrl()
        {
            var returnUrl = Request.QueryString["ReturnUrl"];
            if (!string.IsNullOrWhiteSpace(returnUrl) && UrlIsLocal(returnUrl))
            {
                Response.Redirect(returnUrl);
                return;
            }

            Response.Redirect("~/Hazards/HazardList.aspx");
        }

        private static bool UrlIsLocal(string url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                   (url.StartsWith("/") && !url.StartsWith("//") && !url.StartsWith("/\\"));
        }
    }
}
