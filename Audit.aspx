<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Audit.aspx.cs" Inherits="DocManagement.Audit" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Audit Suite</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 1000px; margin: auto; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
        .search-bar { margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 20px; }
        th, td { padding: 10px; border: 1px solid #ccc; text-align: left; }
        th { background-color: #f4f4f4; }
        .btn { padding: 8px 12px; background-color: #28a745; color: #fff; text-decoration: none; border: none; cursor: pointer; }
        .btn:hover { background-color: #218838; }
        .status-ok { color: green; font-weight: bold; }
        .status-error { color: red; font-weight: bold; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="header">
                <h2>Audit Suite Dashboard</h2>
                <div>
                    <a href="Health.aspx" class="btn" style="background-color:#17a2b8;">Health Dashboard</a>
                    <a href="Default.aspx" class="btn" style="background-color:#007bff;">Back to Home</a>
                </div>
            </div>

            <div class="search-bar">
                <asp:TextBox ID="txtSearch" runat="server" Placeholder="Search by filename or date..." Width="300px"></asp:TextBox>
                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn" OnClick="btnSearch_Click" />
                <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn" style="background-color:#6c757d;" OnClick="btnClear_Click" />
            </div>

            <asp:Label ID="lblMessage" runat="server" ForeColor="Red"></asp:Label>

            <asp:GridView ID="gvAudit" runat="server" AutoGenerateColumns="False" EmptyDataText="No audit logs found.">
                <Columns>
                    <asp:BoundField DataField="LogDate" HeaderText="Timestamp" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
                    <asp:BoundField DataField="FileName" HeaderText="File Name" />
                    <asp:BoundField DataField="Status" HeaderText="Status" />
                    <asp:BoundField DataField="ErrorMessage" HeaderText="Details" />
                </Columns>
            </asp:GridView>
        </div>
    </form>
</body>
</html>
