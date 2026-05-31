<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Health.aspx.cs" Inherits="DocManagement.Health" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Health Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 800px; margin: auto; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 20px; }
        th, td { padding: 10px; border: 1px solid #ccc; text-align: left; }
        th { background-color: #f4f4f4; }
        .btn { padding: 8px 12px; background-color: #28a745; color: #fff; text-decoration: none; border: none; cursor: pointer; }
        .btn:hover { background-color: #218838; }
        .status-ok { color: green; font-weight: bold; }
        .status-error { color: red; font-weight: bold; }
        .card { border: 1px solid #ccc; padding: 15px; margin-bottom: 20px; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="header">
                <h2>Health Dashboard</h2>
                <a href="Default.aspx" class="btn" style="background-color:#007bff;">Back to Home</a>
            </div>

            <asp:Label ID="lblError" runat="server" CssClass="status-error" Visible="false"></asp:Label>
            <asp:Label ID="lblMessage" runat="server" ForeColor="Green" Visible="false"></asp:Label>

            <asp:Panel ID="pnlSystemHealthy" runat="server" Visible="false" CssClass="card" BackColor="#ddffdd" BorderColor="Green">
                <h3 style="color: green; margin-top: 0;">System Healthy</h3>
                <p>Binary content indexing is active and correctly configured. The system is correctly configured to index both file names and internal binary content like PDFs.</p>
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

            <div class="card">
                <h3>Search Catalog Status</h3>
                <p>Status: <asp:Label ID="lblCatalogStatus" runat="server" Text="Checking..."></asp:Label></p>
                <p>Total Documents: <asp:Label ID="lblTotalDocs" runat="server" Text="0"></asp:Label></p>
                <p>Indexed Documents: <asp:Label ID="lblIndexedDocs" runat="server" Text="0"></asp:Label></p>
                <p>Non-Indexed (Backlog): <asp:Label ID="lblNonIndexedDocs" runat="server" Text="0"></asp:Label></p>
                <asp:Button ID="btnReindex" runat="server" Text="Manual Re-Index" CssClass="btn" OnClick="btnReindex_Click" />
            </div>

            <div class="card">
                <h3>iFilter Registration Status</h3>
                <table>
                    <tr>
                        <th>Document Type</th>
                        <th>Status</th>
                    </tr>
                    <tr>
                        <td>PDF (.pdf)</td>
                        <td><asp:Label ID="lblPdfStatus" runat="server" Text="Checking..."></asp:Label></td>
                    </tr>
                    <tr>
                        <td>Word (.docx)</td>
                        <td><asp:Label ID="lblDocxStatus" runat="server" Text="Checking..."></asp:Label></td>
                    </tr>
                </table>
            </div>
        </div>
    </form>
</body>
</html>
