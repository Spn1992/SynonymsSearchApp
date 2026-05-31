using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DocManagement
{
    public partial class Default : Page
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
                    string query = "SELECT RecordId, FileName, FilePath FROM Documents";
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        // Using CONTAINS to search both FileName and FileData.
                        // Full-text indexes support this.
                        query = "SELECT RecordId, FileName, FilePath FROM Documents WHERE CONTAINS((FileName, FileData), @SearchTerm)";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        if (!string.IsNullOrEmpty(searchQuery))
                        {
                            cmd.Parameters.AddWithValue("@SearchTerm", searchQuery);
                        }

                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            gvDocuments.DataSource = dt;
                            gvDocuments.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading documents: " + ex.Message;
            }
        }

        protected void gvDocuments_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int recordId = Convert.ToInt32(gvDocuments.DataKeys[e.RowIndex].Value);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "DELETE FROM Documents WHERE RecordId = @RecordId";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@RecordId", recordId);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                BindGrid(txtSearch.Text.Trim()); // Rebind with the current search term, if any
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error deleting document: " + ex.Message;
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                BindGrid();
                return;
            }

            // Execute the async search operation synchronously for WebForms context
            // Note: RegisterAsyncTask is generally preferred in WebForms, but Task.Run().Wait() is used here for simplicity.
            try
            {
                var searchTask = Task.Run(() => PerformSearchAsync(searchTerm));
                searchTask.Wait();
                string fullTextQuery = searchTask.Result;
                BindGrid(fullTextQuery);
            }
            catch (AggregateException ex)
            {
                 lblMessage.Text = "Error during search: " + ex.InnerExceptions.FirstOrDefault()?.Message;
                 Audit.LogAudit(searchTerm, "Error", "Exception during search: " + ex.InnerExceptions.FirstOrDefault()?.Message);
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error during search: " + ex.Message;
                Audit.LogAudit(searchTerm, "Error", "Exception during search: " + ex.Message);
            }
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            BindGrid();
        }

        private async Task<string> PerformSearchAsync(string searchTerm)
        {
            // 1. Tokenize the search term to avoid multi-word phrases causing SQL errors
            string[] tokens = searchTerm.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            using (HttpClient client = new HttpClient())
            {
                var fetchTasks = tokens.Select(async token =>
                {
                    List<string> searchTerms = new List<string> { token };
                    string apiUrl = $"https://api.datamuse.com/words?rel_syn={Uri.EscapeDataString(token)}&max=5";

                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(apiUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            var synonymsData = js.Deserialize<List<DatamuseWord>>(jsonResponse);

                            if (synonymsData != null)
                            {
                                foreach (var item in synonymsData)
                                {
                                    if (!string.IsNullOrEmpty(item.word) && 
                                        !item.word.Contains(" ") && 
                                        !searchTerms.Contains(item.word, StringComparer.OrdinalIgnoreCase))
                                    {
                                        searchTerms.Add(item.word);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If API fails, we just continue with the original search term
                    Audit.LogAudit(searchTerm, "Warning", "Datamuse API error (synonyms unavailable): " + ex.Message);
                }
            }

                    // 2. Construct SQL Full-Text Search query string using OR and FORMSOF for each token's synonyms
                    var orConditions = new List<string>();
                    foreach(var term in searchTerms)
                    {
                        string cleanTerm = term.Replace("\"", "\"\"");
                        orConditions.Add($"FORMSOF(INFLECTIONAL, \"{cleanTerm}\")");
                    }

                    return orConditions.Count > 0 ? "(" + string.Join(" OR ", orConditions) + ")" : string.Empty;
                }).ToList();

                var conditionsArray = await Task.WhenAll(fetchTasks);
                var andConditions = conditionsArray.Where(c => !string.IsNullOrEmpty(c)).ToList();

                // Combine each token's clauses with AND to ensure all query tokens are present
                return string.Join(" AND ", andConditions);
            }
        }

        // Helper class for deserializing Datamuse JSON response
        public class DatamuseWord
        {
            public string word { get; set; }
            public int score { get; set; }
        }
    }
}
