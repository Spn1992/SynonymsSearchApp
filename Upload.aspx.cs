using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI;

namespace DocManagement
{
    public partial class Upload : Page
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (fileUploadControl.HasFile)
            {
                try
                {
                    string fileName = Path.GetFileName(fileUploadControl.PostedFile.FileName);
                    string fileExtension = Path.GetExtension(fileName);

                    // Preserve the leading dot as required by the Full-Text Engine for proper indexing.
                    // Removed the code that strips the leading dot.

                    // A basic validation to ensure it's a file
                    if (string.IsNullOrEmpty(fileExtension))
                    {
                        lblMessage.Text = "File must have an extension to be indexed.";
                        return;
                    }

                    byte[] fileData;
                    using (Stream fs = fileUploadControl.PostedFile.InputStream)
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            fileData = br.ReadBytes((int)fs.Length);
                        }
                    }

                    // Validate binary signature matches extension
                    bool isValidSignature = true;
                    string extForValidation = fileExtension.ToLowerInvariant();
                    if (extForValidation == "pdf")
                    {
                        // %PDF (25 50 44 46)
                        if (fileData.Length < 4 || fileData[0] != 0x25 || fileData[1] != 0x50 || fileData[2] != 0x44 || fileData[3] != 0x46)
                            isValidSignature = false;
                    }
                    else if (extForValidation == "docx")
                    {
                        // PK.. (50 4B 03 04)
                        if (fileData.Length < 4 || fileData[0] != 0x50 || fileData[1] != 0x4B || fileData[2] != 0x03 || fileData[3] != 0x04)
                            isValidSignature = false;
                    }
                    else if (extForValidation == "doc")
                    {
                        // D0 CF 11 E0 A1 B1 1A E1
                        if (fileData.Length < 8 || fileData[0] != 0xD0 || fileData[1] != 0xCF || fileData[2] != 0x11 || fileData[3] != 0xE0 || 
                            fileData[4] != 0xA1 || fileData[5] != 0xB1 || fileData[6] != 0x1A || fileData[7] != 0xE1)
                            isValidSignature = false;
                    }

                    if (!isValidSignature)
                    {
                        lblMessage.ForeColor = System.Drawing.Color.Red;
                        lblMessage.Text = "File signature does not match the extension.";
                        return;
                    }

                    // Insert the record into the database
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        string query = @"INSERT INTO Documents (FileName, FilePath, FileExtension, FileData)
                                         VALUES (@FileName, @FilePath, @FileExtension, @FileData)";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@FileName", fileName);
                            // We aren't saving to disk in this example, so FilePath is a mocked virtual path
                            cmd.Parameters.AddWithValue("@FilePath", "~/virtual/" + fileName);
                            cmd.Parameters.AddWithValue("@FileExtension", fileExtension);
                            cmd.Parameters.AddWithValue("@FileData", fileData);

                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }

                    lblMessage.ForeColor = System.Drawing.Color.Green;
                    lblMessage.Text = "File uploaded successfully!";
                }
                catch (Exception ex)
                {
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Error uploading file: " + ex.Message;
                }
            }
            else
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "Please select a file to upload.";
            }
        }
    }
}
