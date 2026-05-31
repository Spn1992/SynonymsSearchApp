<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DocManagement.Default" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Document Management</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 800px; margin: auto; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
        .search-bar { margin-bottom: 20px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th, td { padding: 10px; border: 1px solid #ccc; text-align: left; }
        th { background-color: #f4f4f4; }
        .btn { padding: 8px 12px; background-color: #007bff; color: #fff; text-decoration: none; border: none; cursor: pointer; }
        .btn:hover { background-color: #0056b3; }
        .btn-danger { background-color: #dc3545; }
        .btn-danger:hover { background-color: #c82333; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="header">
                <h2>Document Management</h2>
                <div>
                    <a href="Upload.aspx" class="btn">Upload New Document</a>
                    <a href="IndexManager.aspx" class="btn" style="background-color: #28a745;">Index Manager</a>
                    <a href="Status.aspx" class="btn" style="background-color: #17a2b8;">Search Integrity</a>
                </div>
            </div>

            <div class="search-bar">
                <asp:TextBox ID="txtSearch" runat="server" Placeholder="Search documents..." Width="300px" Height="25px"></asp:TextBox>
                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn" OnClick="btnSearch_Click" />
                <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn" OnClick="btnClear_Click" />
            </div>

            <asp:Label ID="lblMessage" runat="server" ForeColor="Red"></asp:Label>
            <br /><br />

            <asp:GridView ID="gvDocuments" runat="server" AutoGenerateColumns="False" DataKeyNames="RecordId" OnRowDeleting="gvDocuments_RowDeleting">
                <Columns>
                    <asp:BoundField DataField="RecordId" HeaderText="ID" ReadOnly="True" />
                    <asp:BoundField DataField="FileName" HeaderText="File Name" />
                    <asp:BoundField DataField="FilePath" HeaderText="File Path" />
                    <asp:TemplateField HeaderText="Actions">
                        <ItemTemplate>
                            <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" CssClass="btn btn-danger" OnClientClick="return confirm('Are you sure you want to delete this document?');">Delete</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <p>No documents found.</p>
                </EmptyDataTemplate>
            </asp:GridView>
        </div>
    </form>
</body>
</html>
