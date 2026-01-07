<%@ Page Title="Hazard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="HazardEdit.aspx.cs" Inherits="DatwiseSafetyDemo.Hazards.HazardEdit" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><asp:Literal ID="litHeader" runat="server" /></h2>
    <p class="text-muted"><asp:Literal ID="litMeta" runat="server" /></p>

    <asp:Panel ID="litReadOnly" runat="server" Visible="false" CssClass="alert alert-info">
        You have read-only access to this hazard based on your role.
    </asp:Panel>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Details</strong></div>
        <div class="panel-body">
            <div class="form-group">
                <asp:Label AssociatedControlID="txtTitle" runat="server" Text="Title" />
                <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" MaxLength="200" />
                <asp:RequiredFieldValidator ID="rfvTitle" runat="server" ControlToValidate="txtTitle"
                    ErrorMessage="Title is required" CssClass="text-danger" Display="Dynamic" />
            </div>

            <div class="form-group">
                <asp:Label AssociatedControlID="txtDescription" runat="server" Text="Description" />
                <asp:TextBox ID="txtDescription" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="5" MaxLength="2000" />
                <asp:RequiredFieldValidator ID="rfvDescription" runat="server" ControlToValidate="txtDescription"
                    ErrorMessage="Description is required" CssClass="text-danger" Display="Dynamic" />
            </div>

            <div class="row">
                <div class="col-md-3">
                    <asp:Label AssociatedControlID="ddlSeverity" runat="server" Text="Severity" />
                    <asp:DropDownList ID="ddlSeverity" runat="server" CssClass="form-control" />
                </div>
                <div class="col-md-3">
                    <asp:Label AssociatedControlID="ddlType" runat="server" Text="Type" />
                    <asp:DropDownList ID="ddlType" runat="server" CssClass="form-control" />
                </div>
                <div class="col-md-3">
                    <asp:Label AssociatedControlID="txtDueDate" runat="server" Text="Due date" />
                    <asp:TextBox ID="txtDueDate" runat="server" CssClass="form-control" placeholder="yyyy-MM-dd" />
                    <asp:CustomValidator ID="cvDueDate" runat="server" ControlToValidate="txtDueDate"
                        OnServerValidate="cvDueDate_ServerValidate" ErrorMessage="Invalid date" CssClass="text-danger" Display="Dynamic" />
                </div>
                <div class="col-md-3" style="margin-top:25px;">
                    <asp:Button ID="btnSuggest" runat="server" CssClass="btn btn-default" Text="AI Suggest Severity/Type" OnClick="btnSuggest_OnClick" CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>

    <asp:Panel ID="pnlAssign" runat="server" Visible="false" CssClass="panel panel-default">
        <div class="panel-heading"><strong><asp:Label ID="lblAssignHeader" runat="server" Text="Assign" /></strong></div>
        <div class="panel-body">
            <asp:DropDownList ID="ddlAssignTo" runat="server" CssClass="form-control" />
            <div class="text-danger" style="margin-top:5px;"><asp:Literal ID="litAssignError" runat="server" /></div>
            <div style="margin-top:10px;">
                <asp:Button ID="btnAssign" runat="server" CssClass="btn btn-warning" Text="Assign" OnClick="btnAssign_OnClick" CausesValidation="false" />
            </div>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlStatus" runat="server" Visible="false" CssClass="panel panel-default">
        <div class="panel-heading"><strong>Status</strong></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-3">
                    <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control" />
                </div>
                <div class="col-md-6">
                    <asp:TextBox ID="txtStatusDetails" runat="server" CssClass="form-control" placeholder="Optional status note..." MaxLength="2000" />
                </div>
                <div class="col-md-3">
                    <asp:Button ID="btnChangeStatus" runat="server" CssClass="btn btn-info" Text="Apply" OnClick="btnChangeStatus_OnClick" CausesValidation="false" />
                </div>
            </div>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlComment" runat="server" Visible="false" CssClass="panel panel-default">
        <div class="panel-heading"><strong>Add comment</strong></div>
        <div class="panel-body">
            <asp:TextBox ID="txtComment" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="2000" />
            <div class="text-danger" style="margin-top:5px;"><asp:Literal ID="litCommentError" runat="server" /></div>
            <div style="margin-top:10px;">
                <asp:Button ID="btnAddComment" runat="server" CssClass="btn btn-default" Text="Add Comment" OnClick="btnAddComment_OnClick" CausesValidation="false" />
            </div>
        </div>
    </asp:Panel>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Activity log</strong></div>
        <div class="panel-body">
            <asp:GridView ID="gvLogs" runat="server" CssClass="table table-condensed"
                AutoGenerateColumns="false">
                <Columns>
                    <asp:BoundField DataField="CreatedAt" HeaderText="When" DataFormatString="{0:yyyy-MM-dd HH:mm}" ItemStyle-Width="160px" />
                    <asp:BoundField DataField="PerformedByName" HeaderText="By" ItemStyle-Width="180px" />
                    <asp:BoundField DataField="ActionType" HeaderText="Action" ItemStyle-Width="140px" />
                    <asp:BoundField DataField="Details" HeaderText="Details" />
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <div style="margin-top:15px;">
        <asp:Button ID="btnSave" runat="server" CssClass="btn btn-success" Text="Save" OnClick="btnSave_OnClick" />
        <asp:Button ID="btnCancel" runat="server" CssClass="btn btn-default" Text="Back" OnClick="btnCancel_OnClick" CausesValidation="false" />
        <a class="btn btn-link" href="HazardList.aspx">Back to list</a>
    </div>
</asp:Content>
