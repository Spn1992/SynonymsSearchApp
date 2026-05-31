<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="IndexManager.aspx.cs" Inherits="DocManagement.IndexManager" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Full-Service Index Manager</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f9f9f9; }
        .container { max-width: 1000px; margin: auto; background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; border-bottom: 1px solid #ddd; padding-bottom: 10px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 20px; font-size: 14px; }
        th, td { padding: 10px; border: 1px solid #ddd; text-align: left; }
        th { background-color: #f4f4f4; font-weight: bold; }
        .btn { padding: 8px 12px; background-color: #007bff; color: #fff; text-decoration: none; border: none; cursor: pointer; border-radius: 4px; }
        .btn:hover { background-color: #0056b3; }
        .btn-success { background-color: #28a745; }
        .btn-success:hover { background-color: #218838; }
        .btn-warning { background-color: #ffc107; color: #212529; }
        .btn-warning:hover { background-color: #e0a800; }
        .status-ok { color: green; font-weight: bold; }
        .status-error { color: red; font-weight: bold; }
        .status-warn { color: #d39e00; font-weight: bold; }
        .card { border: 1px solid #e3e3e3; padding: 15px; margin-bottom: 20px; border-radius: 6px; background-color: #fafafa; }
        .card h3 { margin-top: 0; color: #333; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="header">
                <h2>Full-Service Index Manager</h2>
                <a href="Default.aspx" class="btn">Back to Home</a>
            </div>

            <asp:Label ID="lblMessage" runat="server" ForeColor="Green" Font-Bold="true"></asp:Label>
            <asp:Label ID="lblError" runat="server" CssClass="status-error" Font-Bold="true"></asp:Label>
            <br /><br />

            <asp:Panel ID="pnlSystemHealthy" runat="server" Visible="false" CssClass="card" BackColor="#ddffdd" BorderColor="Green">
                <h3 style="color: green; margin-top: 0;">System Healthy</h3>
                <p>Binary content indexing is active and correctly configured.</p>
            </asp:Panel>

            <asp:Panel ID="pnlBinaryWarning" runat="server" Visible="false" CssClass="card" BackColor="#ffdddd" BorderColor="Red">
                <h3 style="color: red; margin-top: 0;">Search Warning: Binary content indexing is disabled</h3>
                <p>Search within file contents is currently inactive. The SQL Filter Daemon cannot parse documents because OS resource loading is disabled.</p>
                <h4>How to Fix:</h4>
                <p>Execute the following SQL commands and restart the filter daemon host process to resolve the issue:</p>
                <pre style="background: #fff; padding: 10px; border: 1px solid #ccc; overflow-x: auto;">EXEC sp_fulltext_service 'load_os_resources', 1;
EXEC sp_fulltext_service 'restart_all_fdhosts';</pre>
                <p><em>Note: You may need DBA privileges to execute these commands.</em></p>
            </asp:Panel>

            <!-- Index Engine Status -->
            <div class="card">
                <h3>Indexing Engine Status</h3>
                <p>Operational State: <asp:Label ID="lblEngineState" runat="server" Text="Unknown"></asp:Label></p>
                <p>
                    <asp:Button ID="btnReloadFilters" runat="server" Text="Reload System Filters" CssClass="btn btn-warning" OnClick="btnReloadFilters_Click" />
                    <asp:Button ID="btnFullRecrawl" runat="server" Text="Trigger Full Re-crawl" CssClass="btn btn-success" OnClick="btnFullRecrawl_Click" />
                    <asp:Button ID="btnReindexUnindexed" runat="server" Text="Re-index Unindexed Only" CssClass="btn" OnClick="btnReindexUnindexed_Click" />
                </p>
            </div>

            <!-- Missing Filters -->
            <div class="card">
                <h3>Missing System Filters (iFilters)</h3>
                <asp:GridView ID="gvMissingFilters" runat="server" AutoGenerateColumns="False" EmptyDataText="All extensions in DB have registered filters.">
                    <Columns>
                        <asp:BoundField DataField="FileExtension" HeaderText="File Extension" />
                        <asp:TemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class="status-error">Missing Filter</span>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

            <!-- Summary Report -->
            <div class="card">
                <h3>Summary Report by Extension</h3>
                <asp:GridView ID="gvSummaryReport" runat="server" AutoGenerateColumns="False" EmptyDataText="No documents found.">
                    <Columns>
                        <asp:BoundField DataField="FileExtension" HeaderText="Extension" />
                        <asp:BoundField DataField="TotalDocuments" HeaderText="Total Documents" />
                        <asp:BoundField DataField="SearchableDocuments" HeaderText="Searchable (Indexed)" />
                        <asp:BoundField DataField="UnsearchableDocuments" HeaderText="Unsearchable (Failed)" />
                    </Columns>
                </asp:GridView>
            </div>

            <!-- Specific Unindexed Records -->
            <div class="card">
                <h3>Unindexed Documents (Action Required)</h3>
                <p>Total Unindexed Documents: <asp:Label ID="lblTotalUnindexed" runat="server" Text="0" Font-Bold="true"></asp:Label></p>
                <asp:GridView ID="gvUnindexedRecords" runat="server" AutoGenerateColumns="False" EmptyDataText="No unindexed records found.">
                    <Columns>
                        <asp:BoundField DataField="RecordId" HeaderText="Record ID" />
                        <asp:BoundField DataField="FileName" HeaderText="File Name" />
                        <asp:BoundField DataField="FileExtension" HeaderText="Extension" />
                        <asp:BoundField DataField="ErrorReason" HeaderText="Failure Reason / Error Log" ItemStyle-CssClass="status-error" />
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </form>
</body>
</html>
