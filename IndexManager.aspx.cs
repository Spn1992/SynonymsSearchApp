using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace DocManagement
{
    public partial class IndexManager : Page
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
                LoadDashboard();
            }
        }

        private void LoadDashboard()
        {
            lblMessage.Text = "";
            lblError.Text = "";

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 1. Engine Status
                    LoadEngineStatus(con);

                    // 2. Missing Filters Audit
                    LoadMissingFilters(con);

                    // 3. Summary Report
                    LoadSummaryReport(con);

                    // 4. Specific Unindexed Records (Error Logs)
                    LoadUnindexedRecords(con);
                }
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("Error loading dashboard", ex);
                lblError.Text = "Error loading dashboard: " + ex.Message;
            }
        }

        private void LoadEngineStatus(SqlConnection con)
        {
            string query = "SELECT FULLTEXTCATALOGPROPERTY('DocCatalog', 'PopulateStatus') AS PopulateStatus;";
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                {
                    int status = Convert.ToInt32(result);
                    switch (status)
                    {
                        case 0:
                            lblEngineState.Text = "Idle";
                            lblEngineState.CssClass = "status-ok";
                            break;
                        case 1:
                            lblEngineState.Text = "Full Crawl in Progress";
                            lblEngineState.CssClass = "status-warn";
                            break;
                        case 2:
                            lblEngineState.Text = "Paused";
                            lblEngineState.CssClass = "status-warn";
                            break;
                        case 3:
                            lblEngineState.Text = "Throttled";
                            lblEngineState.CssClass = "status-warn";
                            break;
                        case 4:
                            lblEngineState.Text = "Recovering";
                            lblEngineState.CssClass = "status-warn";
                            break;
                        case 5:
                            lblEngineState.Text = "Shutdown / Offline";
                            lblEngineState.CssClass = "status-error";
                            break;
                        case 6:
                            lblEngineState.Text = "Incremental Crawl in Progress";
                            lblEngineState.CssClass = "status-warn";
                            break;
                        case 7:
                            lblEngineState.Text = "Building Index";
                            lblEngineState.CssClass = "status-warn";
                            break;
                        case 8:
                            lblEngineState.Text = "Disk Full. Paused.";
                            lblEngineState.CssClass = "status-error";
                            break;
                        case 9:
                            lblEngineState.Text = "Change Tracking (Auto)";
                            lblEngineState.CssClass = "status-ok";
                            break;
                        default:
                            lblEngineState.Text = "Unknown (" + status + ")";
                            lblEngineState.CssClass = "status-error";
                            break;
                    }
                }
                else
                {
                    lblEngineState.Text = "Catalog not found";
                    lblEngineState.CssClass = "status-error";
                }
            }
        }

        private void LoadMissingFilters(SqlConnection con)
        {
            string query = @"
                SELECT DISTINCT d.FileExtension 
                FROM Documents d 
                LEFT JOIN sys.fulltext_document_types fdt ON d.FileExtension = fdt.document_type 
                WHERE fdt.document_type IS NULL;
            ";
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandTimeout = 120;
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    gvMissingFilters.DataSource = dt;
                    gvMissingFilters.DataBind();
                }
            }
        }

        private void LoadSummaryReport(SqlConnection con)
        {
            string query = @"
                DECLARE @FileColId INT = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'FileData');

                SELECT 
                    d.FileExtension,
                    COUNT(d.RecordId) AS TotalDocuments,
                    SUM(CASE WHEN k.document_id IS NOT NULL THEN 1 ELSE 0 END) AS SearchableDocuments,
                    SUM(CASE WHEN k.document_id IS NULL THEN 1 ELSE 0 END) AS UnsearchableDocuments
                FROM Documents d
                LEFT JOIN (
                    SELECT DISTINCT document_id 
                    FROM sys.dm_fts_index_keywords_by_document(DB_ID(), OBJECT_ID('Documents'))
                    WHERE column_id = @FileColId
                ) k ON d.RecordId = k.document_id
                GROUP BY d.FileExtension;
            ";
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandTimeout = 120;
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    gvSummaryReport.DataSource = dt;
                    gvSummaryReport.DataBind();
                }
            }
        }

        private void LoadUnindexedRecords(SqlConnection con)
        {
            string query = @"
                DECLARE @FileColId INT = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'FileData');

                SELECT d.RecordId, d.FileName, d.FileExtension,
                    CASE 
                        WHEN fdt.document_type IS NULL THEN 'Missing iFilter for ' + d.FileExtension
                        ELSE 'Content could not be extracted (corrupt or unsupported format)'
                    END AS ErrorReason
                FROM Documents d
                LEFT JOIN (
                    SELECT DISTINCT document_id 
                    FROM sys.dm_fts_index_keywords_by_document(DB_ID(), OBJECT_ID('Documents'))
                    WHERE column_id = @FileColId
                ) k ON d.RecordId = k.document_id
                LEFT JOIN sys.fulltext_document_types fdt ON d.FileExtension = fdt.document_type
                WHERE k.document_id IS NULL;
            ";
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.CommandTimeout = 120;
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    gvUnindexedRecords.DataSource = dt;
                    gvUnindexedRecords.DataBind();
                    lblTotalUnindexed.Text = dt.Rows.Count.ToString();
                }
            }
        }

        protected void btnReloadFilters_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        EXEC sp_fulltext_service 'load_os_resources', 1;
                        EXEC sp_fulltext_service 'verify_signature', 0;
                        EXEC sp_fulltext_service 'update_languages';
                        EXEC sp_fulltext_service 'restart_all_fdhosts';
                    ";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                lblMessage.Text = "System filters reloaded successfully. The Search Engine OS resources have been refreshed.";
                LoadDashboard();
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("Error reloading filters", ex);
                lblError.Text = "Error reloading filters: " + ex.Message;
            }
        }

        protected void btnFullRecrawl_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "ALTER FULLTEXT INDEX ON Documents START FULL POPULATION;";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                lblMessage.Text = "Full re-crawl triggered successfully. Background indexing is now in progress.";
                LoadDashboard();
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("Error triggering full re-crawl", ex);
                lblError.Text = "Error triggering full re-crawl: " + ex.Message;
            }
        }

        protected void btnReindexUnindexed_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // "Touch" unindexed records to trigger Change Tracking (auto background population)
                    string query = @"
                        DECLARE @FileColId INT = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'FileData');

                        UPDATE d
                        SET FileExtension = d.FileExtension
                        FROM Documents d
                        LEFT JOIN (
                            SELECT DISTINCT document_id 
                            FROM sys.dm_fts_index_keywords_by_document(DB_ID(), OBJECT_ID('Documents'))
                            WHERE column_id = @FileColId
                        ) k ON d.RecordId = k.document_id
                        WHERE k.document_id IS NULL;
                    ";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.CommandTimeout = 300; // allow 5 mins
                        int rowsAffected = cmd.ExecuteNonQuery();
                        lblMessage.Text = $"Successfully flagged {rowsAffected} unindexed records for re-crawling. Change tracking will process them in the background.";
                    }
                }
                LoadDashboard();
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("Error triggering re-index for unindexed records", ex);
                lblError.Text = "Error triggering re-index for unindexed records: " + ex.Message;
            }
        }
    }
}
