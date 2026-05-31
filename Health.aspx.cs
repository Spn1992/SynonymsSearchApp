using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace DocManagement
{
    public partial class Health : Page
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
                LoadHealthData();
            }
        }

        private void LoadHealthData()
        {
            lblError.Visible = false;
            lblMessage.Visible = false;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 1. Catalog Status and Counts
                    string catalogQuery = @"
                        DECLARE @PopulateStatus INT = FULLTEXTCATALOGPROPERTY('DocCatalog', 'PopulateStatus');
                        DECLARE @IndexedCount INT = ISNULL(FULLTEXTCATALOGPROPERTY('DocCatalog', 'ItemCount'), 0);
                        DECLARE @TableFTSActive INT = OBJECTPROPERTY(OBJECT_ID('Documents'), 'TableHasActiveFulltextIndex');
                        
                        -- Fast count using partitions
                        DECLARE @TotalCount INT;
                        SELECT @TotalCount = SUM(rows)
                        FROM sys.partitions
                        WHERE object_id = OBJECT_ID('Documents') AND index_id IN (0,1);

                        SELECT @PopulateStatus AS PopulateStatus, @IndexedCount AS IndexedCount, ISNULL(@TotalCount, 0) AS TotalCount, ISNULL(@TableFTSActive, 0) AS TableFTSActive;
                    ";

                    using (SqlCommand cmd = new SqlCommand(catalogQuery, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int populateStatus = reader["PopulateStatus"] != DBNull.Value ? Convert.ToInt32(reader["PopulateStatus"]) : -1;
                                int indexedCount = Convert.ToInt32(reader["IndexedCount"]);
                                int totalCount = Convert.ToInt32(reader["TotalCount"]);
                                int tableFtsActive = Convert.ToInt32(reader["TableFTSActive"]);

                                lblTotalDocs.Text = totalCount.ToString();
                                lblIndexedDocs.Text = indexedCount.ToString();
                                
                                int nonIndexed = totalCount - indexedCount;
                                lblNonIndexedDocs.Text = nonIndexed > 0 ? nonIndexed.ToString() : "0";

                                // Check offline or suspended state
                                if (tableFtsActive == 0)
                                {
                                    lblCatalogStatus.Text = "Table Indexing Disabled";
                                    lblCatalogStatus.CssClass = "status-error";
                                    lblError.Text = "Error: Full-Text Indexing is disabled at the table level for 'Documents'.";
                                    lblError.Visible = true;
                                }
                                else if (populateStatus == 2 || populateStatus == 5 || populateStatus == 8 || populateStatus == -1)
                                {
                                    lblCatalogStatus.Text = "Offline / Suspended";
                                    lblCatalogStatus.CssClass = "status-error";
                                    lblError.Text = "Error: The search catalog is currently in an offline or suspended state.";
                                    lblError.Visible = true;
                                }
                                else
                                {
                                    lblCatalogStatus.Text = "Healthy";
                                    lblCatalogStatus.CssClass = "status-ok";
                                }
                            }
                        }
                    }

                    // 2. iFilter Registration Status
                    string filterQuery = @"
                        SELECT document_type 
                        FROM sys.fulltext_document_types 
                        WHERE document_type IN ('.pdf', '.docx');
                    ";

                    bool pdfFound = false;
                    bool docxFound = false;

                    using (SqlCommand cmd = new SqlCommand(filterQuery, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string docType = reader["document_type"].ToString().ToLowerInvariant();
                                if (docType == ".pdf") pdfFound = true;
                                if (docType == ".docx") docxFound = true;
                            }
                        }
                    }

                    if (pdfFound)
                    {
                        lblPdfStatus.Text = "Registered";
                        lblPdfStatus.CssClass = "status-ok";
                    }
                    else
                    {
                        lblPdfStatus.Text = "Missing / Improperly Registered";
                        lblPdfStatus.CssClass = "status-error";
                    }

                    if (docxFound)
                    {
                        lblDocxStatus.Text = "Registered";
                        lblDocxStatus.CssClass = "status-ok";
                    }
                    else
                    {
                        lblDocxStatus.Text = "Missing / Improperly Registered";
                        lblDocxStatus.CssClass = "status-error";
                    }

                    // 3. Check FDLauncher status
                    string serviceQuery = @"
                        SELECT status_desc, service_account 
                        FROM sys.dm_server_services 
                        WHERE servicename LIKE '%Filter Daemon Launcher%';
                    ";
                    bool isLauncherRunning = false;
                    string serviceAccount = "";
                    using (SqlCommand cmd = new SqlCommand(serviceQuery, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string status = reader["status_desc"].ToString();
                                serviceAccount = reader["service_account"].ToString();
                                if (status.Equals("Running", StringComparison.OrdinalIgnoreCase))
                                {
                                    isLauncherRunning = true;
                                }
                            }
                        }
                    }

                    if (isLauncherRunning)
                    {
                        lblFdLauncherStatus.Text = "Running (Healthy)";
                        lblFdLauncherStatus.CssClass = "status-ok";
                        lblFdLauncherError.Visible = false;
                    }
                    else
                    {
                        lblFdLauncherStatus.Text = "Stopped / Error";
                        lblFdLauncherStatus.CssClass = "status-error";
                        lblFdLauncherError.Text = "The MSSQLFDLauncher service is not running. Please verify that the account '" + serviceAccount + "' has appropriate NTFS permissions for the temporary directories required for text extraction.";
                        lblFdLauncherError.Visible = true;
                    }
                }

                LoadDiagnosticLogs();
            }
            catch (Exception ex)
            {
                lblError.Text = "Error loading diagnostic data: " + ex.Message;
                lblError.Visible = true;
            }
        }

        private void LoadDiagnosticLogs()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT LogTime, LogLevel, Message, ExceptionDetails FROM DiagnosticLogs ORDER BY LogTime DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            gvDiagnosticLogs.DataSource = dt;
                            gvDiagnosticLogs.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail if table doesn't exist
            }
        }

        protected void btnSelfTest_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Dummy index operation
                    string dummyQuery = @"
                        DECLARE @DummyId INT;
                        INSERT INTO Documents (FileName, FilePath, FileExtension, FileData) 
                        VALUES ('SelfTest.txt', 'SelfTest.txt', '.txt', CONVERT(varbinary(max), 'SelfTestContent'));
                        SET @DummyId = SCOPE_IDENTITY();
                        
                        -- Force index update
                        ALTER FULLTEXT INDEX ON Documents START UPDATE POPULATION;
                        
                        -- Wait for a moment to allow index to start processing
                        WAITFOR DELAY '00:00:02';
                        
                        -- Dummy search operation
                        DECLARE @SearchResult INT;
                        SELECT @SearchResult = COUNT(*) FROM Documents WHERE CONTAINS((FileName, FileData), 'SelfTestContent');
                        
                        -- Clean up
                        DELETE FROM Documents WHERE RecordId = @DummyId;
                    ";
                    using (SqlCommand cmd = new SqlCommand(dummyQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                DiagnosticLogger.LogInfo("Self-test completed successfully.");
                lblMessage.Text = "Self-test executed successfully.";
                lblMessage.Visible = true;
                LoadHealthData();
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("Self-test failed.", ex);
                lblError.Text = "Self-test failed: " + ex.Message;
                lblError.Visible = true;
                LoadHealthData();
            }
        }

        protected void btnReindex_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            lblMessage.Visible = false;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Trigger manual refresh/re-index
                    string reindexQuery = "ALTER FULLTEXT CATALOG DocCatalog REBUILD;";
                    using (SqlCommand cmd = new SqlCommand(reindexQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                lblMessage.Text = "Re-indexing triggered successfully.";
                lblMessage.Visible = true;

                LoadHealthData();
            }
            catch (Exception ex)
            {
                lblError.Text = "Error triggering re-index: " + ex.Message;
                lblError.Visible = true;
            }
        }
    }
}
