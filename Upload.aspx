<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="DocManagement.Upload" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Upload Document</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ccc; border-radius: 5px; }
        .form-group { margin-bottom: 15px; }
        .btn { padding: 8px 12px; background-color: #28a745; color: #fff; text-decoration: none; border: none; cursor: pointer; }
        .btn:hover { background-color: #218838; }
        .link { display: inline-block; margin-top: 15px; color: #007bff; text-decoration: none; }
        .link:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <h2>Upload New Document</h2>

            <asp:Label ID="lblMessage" runat="server" ForeColor="Red"></asp:Label>
            <br /><br />

            <div class="form-group">
                <asp:FileUpload ID="fileUploadControl" runat="server" Width="100%" />
            </div>

            <div class="form-group">
                <asp:Button ID="btnUpload" runat="server" Text="Upload" CssClass="btn" OnClick="btnUpload_Click" />
            </div>

            <a href="Default.aspx" class="link">&larr; Back to Main Screen</a>
        </div>
    </form>
</body>
</html>
