using System;
using System.Configuration;
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

                    // 0. OS Resource Loading Diagnostic
                    try
                    {
                        string osResourceQuery = "SELECT FULLTEXTSERVICEPROPERTY('LoadOSResources') AS LoadOSResources;";
                        using (SqlCommand cmd = new SqlCommand(osResourceQuery, con))
                        {
                            object result = cmd.ExecuteScalar();
                            if (result != DBNull.Value && result != null)
                            {
                                int isLoaded = Convert.ToInt32(result);
                                if (isLoaded == 1)
                                {
                                    pnlSystemHealthy.Visible = true;
                                    pnlBinaryWarning.Visible = false;
                                }
                                else
                                {
                                    pnlSystemHealthy.Visible = false;
                                    pnlBinaryWarning.Visible = true;
                                }
                            }
                            else
                            {
                                // If null, assume missing/disabled
                                pnlSystemHealthy.Visible = false;
                                pnlBinaryWarning.Visible = true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Gracefully handle permission errors
                        pnlSystemHealthy.Visible = false;
                        pnlBinaryWarning.Visible = true; // Safe fallback assumption for diagnostic
                    }

                    // 1. Catalog Status and Counts
                    string catalogQuery = @"
                        DECLARE @PopulateStatus INT = FULLTEXTCATALOGPROPERTY('DocCatalog', 'PopulateStatus');
                        DECLARE @IndexedCount INT = ISNULL(FULLTEXTCATALOGPROPERTY('DocCatalog', 'ItemCount'), 0);
                        
                        -- Fast count using partitions
                        DECLARE @TotalCount INT;
                        SELECT @TotalCount = SUM(rows)
                        FROM sys.partitions
                        WHERE object_id = OBJECT_ID('Documents') AND index_id IN (0,1);

                        SELECT @PopulateStatus AS PopulateStatus, @IndexedCount AS IndexedCount, ISNULL(@TotalCount, 0) AS TotalCount;
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

                                lblTotalDocs.Text = totalCount.ToString();
                                lblIndexedDocs.Text = indexedCount.ToString();
                                
                                int nonIndexed = totalCount - indexedCount;
                                lblNonIndexedDocs.Text = nonIndexed > 0 ? nonIndexed.ToString() : "0";

                                // Check offline or suspended state
                                if (populateStatus == 2 || populateStatus == 5 || populateStatus == 8 || populateStatus == -1)
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
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Error loading diagnostic data: " + ex.Message;
                lblError.Visible = true;
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
