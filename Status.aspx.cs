using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace DocManagement
{
    public partial class Status : Page
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Security: Must be admin
            if (User == null || !User.Identity.IsAuthenticated || !User.IsInRole("Admin"))
            {
                Response.StatusCode = 403;
                Response.End();
                return;
            }

            if (!IsPostBack)
            {
                LoadDashboardData();
            }
        }

        private void LoadDashboardData()
        {
            lblError.Visible = false;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 1. Missing Filters (Requirement 1 & 5)
                    string missingFiltersQuery = @"
                        SELECT d.FileExtension, COUNT(*) AS TotalDocs
                        FROM Documents d WITH (NOLOCK)
                        WHERE NOT EXISTS (
                            SELECT 1 
                            FROM sys.fulltext_document_types t
                            WHERE t.document_type = d.FileExtension
                        )
                        GROUP BY d.FileExtension
                        ORDER BY TotalDocs DESC;
                    ";

                    using (SqlCommand cmd = new SqlCommand(missingFiltersQuery, con))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dtMissingFilters = new DataTable();
                            da.Fill(dtMissingFilters);
                            gvMissingFilters.DataSource = dtMissingFilters;
                            gvMissingFilters.DataBind();
                        }
                    }

                    // 2. Unindexed Documents (Requirement 2)
                    string unindexedQuery = @"
                        SELECT d.RecordId, d.FileName, d.FileExtension
                        FROM Documents d WITH (NOLOCK)
                        WHERE NOT EXISTS (
                            SELECT 1 
                            FROM sys.dm_fts_index_keywords_by_document(DB_ID(), OBJECT_ID('Documents')) k
                            WHERE k.document_id = d.RecordId
                        );
                    ";
                    
                    int totalDocs = 0;
                    int unindexedDocsCount = 0;
                    
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Documents WITH (NOLOCK);", con))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            totalDocs = Convert.ToInt32(result);
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(unindexedQuery, con))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dtUnindexedDocs = new DataTable();
                            da.Fill(dtUnindexedDocs);
                            gvUnindexedDocs.DataSource = dtUnindexedDocs;
                            gvUnindexedDocs.DataBind();
                            
                            unindexedDocsCount = dtUnindexedDocs.Rows.Count;
                        }
                    }

                    // 3. Search Health Score (Requirement 3)
                    int indexedDocs = totalDocs - unindexedDocsCount;
                    lblTotalDocs.Text = totalDocs.ToString();
                    lblIndexedDocs.Text = indexedDocs.ToString();
                    
                    if (totalDocs > 0)
                    {
                        double score = ((double)indexedDocs / totalDocs) * 100.0;
                        lblHealthScore.Text = score.ToString("0.##") + "%";
                        lblHealthScore.CssClass = score == 100 ? "status-ok" : "status-error";
                    }
                    else
                    {
                        lblHealthScore.Text = "N/A";
                        lblHealthScore.CssClass = "";
                    }
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Error loading diagnostic data: " + ex.Message;
                lblError.Visible = true;
            }
        }
    }
}
