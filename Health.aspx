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

            <div class="card">
                <h3>Infrastructure Status</h3>
                <p>SQL Full-Text Service (FDLauncher): <asp:Label ID="lblFdLauncherStatus" runat="server" Text="Checking..."></asp:Label></p>
                <asp:Label ID="lblFdLauncherError" runat="server" CssClass="status-error" Visible="false"></asp:Label>
                <br />
                <asp:Button ID="btnSelfTest" runat="server" Text="Run End-to-End Self-Test" CssClass="btn" OnClick="btnSelfTest_Click" />
            </div>

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

            <div class="card">
                <h3>Diagnostic Logs</h3>
                <asp:GridView ID="gvDiagnosticLogs" runat="server" AutoGenerateColumns="False" Width="100%" CssClass="table" EmptyDataText="No diagnostic logs available.">
                    <Columns>
                        <asp:BoundField DataField="LogTime" HeaderText="Time" />
                        <asp:BoundField DataField="LogLevel" HeaderText="Level" />
                        <asp:BoundField DataField="Message" HeaderText="Message" />
                        <asp:BoundField DataField="ExceptionDetails" HeaderText="Details" />
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </form>
</body>
</html>
