<%@ Page Title="Hazards" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="HazardList.aspx.cs" Inherits="DatwiseSafetyDemo.Hazards.HazardList" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row">
        <div class="col-md-8">
            <h2>Hazards</h2>
            <p class="text-muted">Role-based list with quick actions, export and drill-down support.</p>
        </div>
        <div class="col-md-4 text-right" style="margin-top:20px;">
            <asp:HyperLink ID="lnkExportCsv" runat="server" CssClass="btn btn-default btn-sm" Text="Export CSV" />
            <asp:HyperLink ID="lnkExportPdf" runat="server" CssClass="btn btn-default btn-sm" Text="Export PDF" />
            <asp:Button ID="btnNewHazard" runat="server" CssClass="btn btn-primary btn-sm" Text="New Hazard" OnClick="btnNewHazard_OnClick" />
        </div>
    </div>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Filters</strong></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-4">
                    <asp:Label AssociatedControlID="txtSearch" runat="server" Text="Search" />
                    <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" placeholder="Title / description..." />
                </div>

                <div class="col-md-2">
                    <asp:Label AssociatedControlID="ddlStatus" runat="server" Text="Status" />
                    <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control" />
                </div>

                <div class="col-md-2">
                    <asp:Label AssociatedControlID="ddlSeverity" runat="server" Text="Severity" />
                    <asp:DropDownList ID="ddlSeverity" runat="server" CssClass="form-control" />
                </div>

                <div class="col-md-2">
                    <asp:Label AssociatedControlID="ddlType" runat="server" Text="Type" />
                    <asp:DropDownList ID="ddlType" runat="server" CssClass="form-control" />
                </div>

                <div class="col-md-2">
                    <asp:Label runat="server" Text="Overdue" />
                    <div>
                        <asp:CheckBox ID="chkOverdue" runat="server" Text="Only overdue" />
                    </div>
                </div>
            </div>

            <asp:Panel ID="pnlAssignedTo" runat="server" Visible="false" CssClass="row" style="margin-top:10px;">
                <div class="col-md-4">
                    <asp:Label AssociatedControlID="ddlAssignedTo" runat="server" Text="Assigned to (Site Manager)" />
                    <asp:DropDownList ID="ddlAssignedTo" runat="server" CssClass="form-control" />
                </div>
            </asp:Panel>

            <div class="row" style="margin-top:10px;">
                <div class="col-md-12">
                    <asp:Button ID="btnFilter" runat="server" CssClass="btn btn-success" Text="Apply" OnClick="btnFilter_OnClick" />
                    <asp:Button ID="btnClear" runat="server" CssClass="btn btn-default" Text="Clear" OnClick="btnClear_OnClick" CausesValidation="false" />
                    <a class="btn btn-link" href="<%= ResolveUrl("~/Dashboard.aspx") %>">Go to Dashboard</a>
                </div>
            </div>
        </div>
    </div>

    <asp:GridView ID="gvHazards" runat="server" CssClass="table table-striped table-hover"
        AutoGenerateColumns="false" OnRowCommand="gvHazards_RowCommand" OnRowDataBound="gvHazards_RowDataBound">
        <Columns>
            <asp:BoundField DataField="HazardId" HeaderText="#" ItemStyle-Width="50px" />

            <asp:TemplateField HeaderText="Title">
                <ItemTemplate>
                    <a href='<%# "HazardEdit.aspx?id=" + Eval("HazardId") %>'><%# HttpUtility.HtmlEncode((string)Eval("Title")) %></a>
                    <div class="text-muted small">
                        Reported by: <%# HttpUtility.HtmlEncode((string)Eval("ReportedByName")) %>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:BoundField DataField="Status" HeaderText="Status" ItemStyle-Width="110px" />
            <asp:BoundField DataField="Severity" HeaderText="Severity" ItemStyle-Width="90px" />
            <asp:BoundField DataField="Type" HeaderText="Type" ItemStyle-Width="110px" />

            <asp:TemplateField HeaderText="Assigned To" ItemStyle-Width="160px">
                <ItemTemplate>
                    <%# string.IsNullOrWhiteSpace((string)Eval("AssignedToName")) ? "-" : HttpUtility.HtmlEncode((string)Eval("AssignedToName")) %>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Due" ItemStyle-Width="120px">
                <ItemTemplate>
                    <%# Eval("DueDate") == null ? "-" : ((DateTime)Eval("DueDate")).ToString("yyyy-MM-dd") %>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:BoundField DataField="CreatedAt" HeaderText="Created" DataFormatString="{0:yyyy-MM-dd}" ItemStyle-Width="120px" />

            <asp:TemplateField HeaderText="Actions" ItemStyle-Width="220px">
                <ItemTemplate>
                    <asp:LinkButton ID="btnTake" runat="server" Text="Take" CssClass="btn btn-xs btn-default"
                        CommandName="Take" CommandArgument='<%# Eval("HazardId") %>' CausesValidation="false" />

                    <asp:LinkButton ID="btnStart" runat="server" Text="Start" CssClass="btn btn-xs btn-info"
                        CommandName="Start" CommandArgument='<%# Eval("HazardId") %>' CausesValidation="false" />

                    <asp:LinkButton ID="btnResolve" runat="server" Text="Resolve" CssClass="btn btn-xs btn-success"
                        CommandName="Resolve" CommandArgument='<%# Eval("HazardId") %>' CausesValidation="false" />

                    <a class="btn btn-xs btn-primary" href='<%# "HazardEdit.aspx?id=" + Eval("HazardId") %>'>View / Edit</a>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Content>
