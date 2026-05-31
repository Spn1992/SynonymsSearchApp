<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Status.aspx.cs" Inherits="DocManagement.Status" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Search Integrity Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 900px; margin: auto; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 20px; }
        th, td { padding: 10px; border: 1px solid #ccc; text-align: left; }
        th { background-color: #f4f4f4; }
        .btn { padding: 8px 12px; background-color: #28a745; color: #fff; text-decoration: none; border: none; cursor: pointer; }
        .btn:hover { background-color: #218838; }
        .status-ok { color: green; font-weight: bold; }
        .status-error { color: #d9534f; font-weight: bold; }
        .card { border: 1px solid #ccc; padding: 15px; margin-bottom: 20px; }
        .score-container { font-size: 24px; font-weight: bold; margin-bottom: 15px; }
        .badge-missing { background-color: #d9534f; color: white; padding: 3px 8px; border-radius: 12px; font-size: 12px; font-weight: bold; }
        .badge-unindexed { background-color: #f0ad4e; color: white; padding: 3px 8px; border-radius: 12px; font-size: 12px; font-weight: bold; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="header">
                <h2>Search Integrity Dashboard</h2>
                <a href="Default.aspx" class="btn" style="background-color:#007bff;">Back to Home</a>
            </div>

            <asp:Label ID="lblError" runat="server" CssClass="status-error" Visible="false"></asp:Label>

            <div class="card">
                <h3>Search Health Score</h3>
                <div class="score-container">
                    <asp:Label ID="lblHealthScore" runat="server" Text="0%"></asp:Label>
                </div>
                <p>Total Documents: <asp:Label ID="lblTotalDocs" runat="server" Text="0"></asp:Label></p>
                <p>Successfully Indexed: <asp:Label ID="lblIndexedDocs" runat="server" Text="0"></asp:Label></p>
            </div>

            <div class="card">
                <h3>Missing Filters</h3>
                <p>Extensions currently stored in the database missing server-side iFilters.</p>
                <asp:GridView ID="gvMissingFilters" runat="server" AutoGenerateColumns="False" CssClass="table">
                    <Columns>
                        <asp:BoundField DataField="FileExtension" HeaderText="File Extension" />
                        <asp:BoundField DataField="TotalDocs" HeaderText="Total Documents" />
                        <asp:TemplateField HeaderText="Issue">
                            <ItemTemplate>
                                <span class="badge-missing">Missing Filter</span>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        No missing filters detected. All extensions have registered iFilters.
                    </EmptyDataTemplate>
                </asp:GridView>
            </div>

            <div class="card">
                <h3>Unindexed Documents</h3>
                <p>Specific documents with 0 search keywords.</p>
                <asp:GridView ID="gvUnindexedDocs" runat="server" AutoGenerateColumns="False" CssClass="table">
                    <Columns>
                        <asp:BoundField DataField="RecordId" HeaderText="Record ID" />
                        <asp:BoundField DataField="FileName" HeaderText="File Name" />
                        <asp:BoundField DataField="FileExtension" HeaderText="Extension" />
                        <asp:TemplateField HeaderText="Issue">
                            <ItemTemplate>
                                <span class="badge-unindexed">Unindexed File</span>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        All documents are successfully indexed.
                    </EmptyDataTemplate>
                </asp:GridView>
            </div>
        </div>
    </form>
</body>
</html>
