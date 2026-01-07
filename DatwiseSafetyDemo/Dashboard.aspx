<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="DatwiseSafetyDemo.Dashboard" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row">
        <div class="col-md-8">
            <h2>Safety Dashboard</h2>
            <p class="text-muted">KPIs and breakdowns are scoped to your role (RBAC).</p>
        </div>
        <div class="col-md-4 text-right" style="margin-top:20px;">
            <a class="btn btn-primary" href="<%= ResolveUrl("~/Hazards/HazardList.aspx") %>">View Hazards</a>
        </div>
    </div>

    <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger">
        <asp:Literal ID="litError" runat="server" />
    </asp:Panel>

    <div class="row">
        <div class="col-md-3">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>Open</strong></div>
                <div class="panel-body">
                    <h3><asp:HyperLink ID="lnkOpen" runat="server" /></h3>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>In Progress</strong></div>
                <div class="panel-body">
                    <h3><asp:HyperLink ID="lnkInProgress" runat="server" /></h3>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>Resolved</strong></div>
                <div class="panel-body">
                    <h3><asp:HyperLink ID="lnkResolved" runat="server" /></h3>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>Overdue</strong></div>
                <div class="panel-body">
                    <h3><asp:HyperLink ID="lnkOverdue" runat="server" /></h3>
                </div>
            </div>
        </div>
    </div>

    <div class="row">
        <div class="col-md-6">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>By Severity</strong></div>
                <div class="panel-body">
                    <asp:GridView ID="gvBySeverity" runat="server" CssClass="table table-condensed" AutoGenerateColumns="false">
                        <Columns>
                            <asp:TemplateField HeaderText="Severity">
                                <ItemTemplate>
                                    <a href='<%# Eval("Link") %>'><%# Eval("Key") %></a>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Count" HeaderText="Count" ItemStyle-Width="80px" />
                            <asp:TemplateField HeaderText="Visual">
                                <ItemTemplate>
                                    <div style='background:#eee;height:12px;border-radius:6px;'>
                                        <div style='background:#337ab7;height:12px;border-radius:6px;width:<%# Eval("Percent") %>%'></div>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>

        <div class="col-md-6">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>By Type</strong></div>
                <div class="panel-body">
                    <asp:GridView ID="gvByType" runat="server" CssClass="table table-condensed" AutoGenerateColumns="false">
                        <Columns>
                            <asp:TemplateField HeaderText="Type">
                                <ItemTemplate>
                                    <a href='<%# Eval("Link") %>'><%# Eval("Key") %></a>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Count" HeaderText="Count" ItemStyle-Width="80px" />
                            <asp:TemplateField HeaderText="Visual">
                                <ItemTemplate>
                                    <div style='background:#eee;height:12px;border-radius:6px;'>
                                        <div style='background:#5cb85c;height:12px;border-radius:6px;width:<%# Eval("Percent") %>%'></div>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
