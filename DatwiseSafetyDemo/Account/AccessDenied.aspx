<%@ Page Title="Access denied" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AccessDenied.aspx.cs" Inherits="DatwiseSafetyDemo.Account.AccessDenied" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="alert alert-warning mt-4">
        <h4 class="alert-heading">Access denied</h4>
        <p>You don't have permission to view this page.</p>
        <hr />
        <a class="btn btn-secondary" runat="server" href="~/">Go home</a>
    </div>
</asp:Content>
