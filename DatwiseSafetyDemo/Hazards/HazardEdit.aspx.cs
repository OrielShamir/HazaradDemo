using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using DatwiseSafetyDemo.Data;
using DatwiseSafetyDemo.Infrastructure;
using DatwiseSafetyDemo.Models;
using DatwiseSafetyDemo.Services;

namespace DatwiseSafetyDemo.Hazards
{
    [ExcludeFromCodeCoverage]
    public partial class HazardEdit : SecurePage
    {
        protected override string[] AllowedRoles => new[] { Roles.FieldWorker, Roles.SiteManager, Roles.SafetyOfficer };

        private readonly IHazardRepository _hazards;
        private readonly IUserRepository _users;
        private readonly IHazardClassificationService _classifier;

        public HazardEdit()
        {
            _hazards = new SqlHazardRepository();
            _users = new SqlUserRepository();
            _classifier = new StubHazardClassificationService();
        }

        private int? HazardId
        {
            get
            {
                if (int.TryParse(Request.QueryString["id"], out var id))
                {
                    return id;
                }

                return null;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                PopulateStaticDropdowns();

                if (HazardId.HasValue)
                {
                    LoadExisting(HazardId.Value, userId, role);
                }
                else
                {
                    // New hazard
                    litHeader.Text = "New Hazard";
                    ddlStatus.Items.Clear();
                    ddlStatus.Items.Add(new ListItem("Open", "Open"));
                    ddlStatus.SelectedValue = "Open";

                    pnlAssign.Visible = false;
                    pnlStatus.Visible = false;

                    btnSave.Text = "Create";
                    ConfigureEditMode(canEdit: true, canComment: HazardAuthorization.CanAddComment(role));
                }
            }
        }

        private void LoadExisting(int hazardId, int userId, string role)
        {
            var hazard = _hazards.GetById(hazardId, userId, role);
            if (hazard == null)
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            litHeader.Text = $"Hazard #{hazard.HazardId} - {HttpUtility.HtmlEncode(hazard.Title)}";

            txtTitle.Text = hazard.Title;
            txtDescription.Text = hazard.Description;
            ddlSeverity.SelectedValue = hazard.Severity;
            if (ddlType.Items.FindByValue(hazard.Type) != null)
            {
                ddlType.SelectedValue = hazard.Type;
            }
            else
            {
                // Be resilient to legacy / out-of-list values.
                ddlType.SelectedValue = "General";
            }
            txtDueDate.Text = hazard.DueDate.HasValue ? hazard.DueDate.Value.ToString("yyyy-MM-dd") : string.Empty;

            litMeta.Text = $"{HttpUtility.HtmlEncode(hazard.Status)} | Reported by {HttpUtility.HtmlEncode(hazard.ReportedByName)} | Assigned to {(string.IsNullOrWhiteSpace(hazard.AssignedToName) ? "-" : HttpUtility.HtmlEncode(hazard.AssignedToName))}";

            var canEdit = HazardAuthorization.CanEditDetails(role, userId, hazard);
            var canComment = HazardAuthorization.CanAddComment(role);

            ConfigureEditMode(canEdit, canComment);

            // Assignment panel
            if (HazardAuthorization.CanAssignToOther(role))
            {
                pnlAssign.Visible = true;

                ddlAssignTo.Items.Clear();
                ddlAssignTo.Items.Add(new ListItem("-- select --", ""));

                var managers = _users.GetActiveUsersByRole(Roles.SiteManager);
                foreach (var m in managers.OrderBy(x => x.FullName))
                {
                    ddlAssignTo.Items.Add(new ListItem(m.FullName, m.UserId.ToString()));
                }

                if (hazard.AssignedToUserId.HasValue)
                {
                    ddlAssignTo.SelectedValue = hazard.AssignedToUserId.Value.ToString();
                }
            }
            else if (HazardAuthorization.CanSelfAssign(role, userId, hazard))
            {
                pnlAssign.Visible = true;
                lblAssignHeader.Text = "Assignment";
                ddlAssignTo.Visible = false;
                btnAssign.Text = "Take ownership";
            }
            else
            {
                pnlAssign.Visible = false;
            }

            // Status panel
            var allowedNext = HazardAuthorization.AllowedNextStatuses(role, userId, hazard);
            pnlStatus.Visible = allowedNext.Length > 0;

            ddlStatus.Items.Clear();
            foreach (var s in allowedNext)
            {
                ddlStatus.Items.Add(new ListItem(s, s));
            }

            // Logs
            BindLogs(hazardId, userId, role);
        }

        private void ConfigureEditMode(bool canEdit, bool canComment)
        {
            txtTitle.Enabled = canEdit;
            txtDescription.Enabled = canEdit;
            ddlSeverity.Enabled = canEdit;
            ddlType.Enabled = canEdit;
            txtDueDate.Enabled = canEdit;

            rfvTitle.Enabled = canEdit;
            rfvDescription.Enabled = canEdit;

            btnSuggest.Visible = canEdit;

            btnSave.Visible = canEdit;
            litReadOnly.Visible = !canEdit;

            pnlComment.Visible = canComment;
        }

        protected void btnSave_OnClick(object sender, EventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!Page.IsValid)
            {
                return;
            }

            if (!HazardId.HasValue)
            {
                // Create new
                var hazard = BuildHazardFromForm(userId);
                var newId = _hazards.Create(hazard, userId);
                Response.Redirect("HazardEdit.aspx?id=" + newId);
                return;
            }

            // Update existing
            var existing = _hazards.GetById(HazardId.Value, userId, role);
            if (existing == null)
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            if (!HazardAuthorization.CanEditDetails(role, userId, existing))
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            var hazardToUpdate = BuildHazardFromForm(existing.ReportedByUserId);
            hazardToUpdate.HazardId = existing.HazardId;

            _hazards.UpdateDetails(hazardToUpdate, userId);

            LoadExisting(existing.HazardId, userId, role);
        }

        protected void btnAssign_OnClick(object sender, EventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!HazardId.HasValue) return;

            var hazard = _hazards.GetById(HazardId.Value, userId, role);
            if (hazard == null)
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            if (HazardAuthorization.CanAssignToOther(role))
            {
                if (!int.TryParse(ddlAssignTo.SelectedValue, out var assignedTo))
                {
                    litAssignError.Text = "Please select a site manager.";
                    return;
                }

                _hazards.Assign(hazard.HazardId, assignedTo, userId);
            }
            else if (HazardAuthorization.CanSelfAssign(role, userId, hazard))
            {
                _hazards.Assign(hazard.HazardId, userId, userId);
            }
            else
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            LoadExisting(hazard.HazardId, userId, role);
        }

        protected void btnChangeStatus_OnClick(object sender, EventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!HazardId.HasValue) return;

            var hazard = _hazards.GetById(HazardId.Value, userId, role);
            if (hazard == null)
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            var newStatus = ddlStatus.SelectedValue;
            if (!HazardAuthorization.CanChangeStatus(role, userId, hazard, newStatus))
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            var details = string.IsNullOrWhiteSpace(txtStatusDetails.Text) ? null : txtStatusDetails.Text.Trim();
            _hazards.ChangeStatus(hazard.HazardId, newStatus, userId, details);

            LoadExisting(hazard.HazardId, userId, role);
        }

        protected void btnAddComment_OnClick(object sender, EventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!HazardId.HasValue) return;

            var hazard = _hazards.GetById(HazardId.Value, userId, role);
            if (hazard == null)
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            if (!HazardAuthorization.CanAddComment(role))
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            var comment = (txtComment.Text ?? string.Empty).Trim();
            if (comment.Length < 3)
            {
                litCommentError.Text = "Comment must be at least 3 characters.";
                return;
            }

            // We store comments as log entries via ChangeStatus proc with ActionType=Comment.
            // To keep the repository surface small, we reuse ChangeStatus with no status change by using a details-only stored procedure.
            // Implemented as dbo.usp_AddHazardLog in schema_v3 and called indirectly via dbo.usp_ChangeHazardStatus when @NewStatus is NULL.
            _hazards.ChangeStatus(hazard.HazardId, null, userId, comment);

            txtComment.Text = string.Empty;

            LoadExisting(hazard.HazardId, userId, role);
        }

        protected void btnSuggest_OnClick(object sender, EventArgs e)
        {
            var suggestion = _classifier.Classify(txtDescription.Text);
            if (suggestion == null) return;

            if (!string.IsNullOrWhiteSpace(suggestion.Severity))
                ddlSeverity.SelectedValue = suggestion.Severity;

            if (!string.IsNullOrWhiteSpace(suggestion.Type))
                ddlType.SelectedValue = suggestion.Type;
        }

        protected void btnCancel_OnClick(object sender, EventArgs e)
        {
            Response.Redirect("HazardList.aspx");
        }

        protected void cvDueDate_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Value))
            {
                args.IsValid = true;
                return;
            }

            args.IsValid = DateTime.TryParse(args.Value, out _);
        }

        private Hazard BuildHazardFromForm(int reportedByUserId)
        {
            DateTime? due = null;
            if (DateTime.TryParse(txtDueDate.Text, out var d))
            {
                due = d;
            }

            return new Hazard
            {
                Title = (txtTitle.Text ?? string.Empty).Trim(),
                Description = (txtDescription.Text ?? string.Empty).Trim(),
                Severity = ddlSeverity.SelectedValue,
                Type = ddlType.SelectedValue,
                DueDate = due,
                ReportedByUserId = reportedByUserId
            };
        }

        private void BindLogs(int hazardId, int userId, string role)
        {
            var logs = _hazards.GetLogs(hazardId, userId, role);
            gvLogs.DataSource = logs;
            gvLogs.DataBind();
        }

        private void PopulateStaticDropdowns()
        {
            ddlSeverity.Items.Clear();
            ddlSeverity.Items.Add(new ListItem("Low", "Low"));
            ddlSeverity.Items.Add(new ListItem("Medium", "Medium"));
            ddlSeverity.Items.Add(new ListItem("High", "High"));
            ddlSeverity.Items.Add(new ListItem("Critical", "Critical"));

            ddlType.Items.Clear();
            ddlType.Items.Add(new ListItem("Fire", "Fire"));
            ddlType.Items.Add(new ListItem("Electrical", "Electrical"));
            ddlType.Items.Add(new ListItem("Chemical", "Chemical"));
            ddlType.Items.Add(new ListItem("Equipment", "Equipment"));
            ddlType.Items.Add(new ListItem("Physical", "Physical"));
            ddlType.Items.Add(new ListItem("General", "General"));
        }
    }
}
