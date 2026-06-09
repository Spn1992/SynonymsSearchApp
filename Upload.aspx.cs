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
                    string fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

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

                    if (isSearchable)
                    {
                        lblMessage.ForeColor = System.Drawing.Color.Green;
                        lblMessage.Text = "File uploaded successfully!";
                    }
                    else
                    {
                        lblMessage.ForeColor = System.Drawing.Color.DarkOrange;
                        lblMessage.Text = "File uploaded successfully! Content search is unavailable for this file type.";
                    }
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
