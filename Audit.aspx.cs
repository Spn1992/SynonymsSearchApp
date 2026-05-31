using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace DocManagement
{
    public partial class Audit : Page
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        private void BindGrid(string searchQuery = null)
        {
            lblMessage.Text = "";
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "SELECT LogDate, FileName, Status, ErrorMessage FROM AuditLogs ORDER BY LogDate DESC";
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        query = "SELECT LogDate, FileName, Status, ErrorMessage FROM AuditLogs WHERE FileName LIKE @SearchTerm OR CONVERT(VARCHAR, LogDate, 120) LIKE @SearchTerm ORDER BY LogDate DESC";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        if (!string.IsNullOrEmpty(searchQuery))
                        {
                            cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchQuery + "%");
                        }

                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            gvAudit.DataSource = dt;
                            gvAudit.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading audit logs: " + ex.Message;
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindGrid(txtSearch.Text.Trim());
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            BindGrid();
        }
        
        public static void LogAudit(string fileName, string status, string errorMessage)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO AuditLogs (FileName, Status, ErrorMessage) VALUES (@FileName, @Status, @ErrorMessage)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FileName", (object)fileName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }

                    // Enforce 90-day retention policy on logs
                    string cleanupQuery = "DELETE FROM AuditLogs WHERE LogDate < DATEADD(day, -90, GETDATE())";
                    using (SqlCommand cleanupCmd = new SqlCommand(cleanupQuery, con))
                    {
                        cleanupCmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Ignore logging errors to prevent breaking the application
            }
        }
    }
}
