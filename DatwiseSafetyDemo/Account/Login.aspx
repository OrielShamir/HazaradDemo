<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="DatwiseSafetyDemo.Account.Login" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row justify-content-center">
        <div class="col-md-6 col-lg-5">
            <div class="card mt-4">
                <div class="card-header">
                    <h4 class="mb-0">Sign in</h4>
                </div>
                <div class="card-body">
                    <asp:Label ID="lblError" runat="server" CssClass="alert alert-danger" Visible="false" />
                    <div class="mb-3">
                        <label class="form-label">Username</label>
                        <asp:TextBox ID="txtUserName" runat="server" CssClass="form-control" />
                        <asp:RequiredFieldValidator ID="rfvUserName" runat="server" ControlToValidate="txtUserName" ErrorMessage="Username is required" Display="Dynamic" CssClass="text-danger" />
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Password</label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" />
                        <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword" ErrorMessage="Password is required" Display="Dynamic" CssClass="text-danger" />
                    </div>
                    <div class="d-flex justify-content-between align-items-center">
                        <asp:Button ID="btnLogin" runat="server" Text="Login" CssClass="btn btn-primary" OnClick="btnLogin_Click" />
                        <small class="text-muted">Demo users: safety / manager / worker (password: Password123!)</small>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
