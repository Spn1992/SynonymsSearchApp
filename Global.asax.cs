using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace DocManagement
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            VerifyFullTextSearchConfiguration();
        }

        private void VerifyFullTextSearchConfiguration()
        {
            string logMessage = "";
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
                builder.ConnectTimeout = 2; // Strict 2-second limit on connection to meet performance constraints
                
                using (SqlConnection con = new SqlConnection(builder.ConnectionString))
                {
                    con.Open();

                    // 1. Check if FTS is installed
                    string checkFtsSql = "SELECT SERVERPROPERTY('IsFullTextInstalled');";
                    using (SqlCommand checkCmd = new SqlCommand(checkFtsSql, con))
                    {
                        checkCmd.CommandTimeout = 2;
                        object isInstalled = checkCmd.ExecuteScalar();
                        if (isInstalled == null || isInstalled == DBNull.Value || Convert.ToInt32(isInstalled) == 0)
                        {
                            logMessage = "Full-Text Search is not installed on this SQL Server instance. Skipping configuration.";
                            LogStartup(logMessage);
                            return;
                        }
                    }

                    // 2. Check load_os_resources
                    string checkSql = "SELECT FULLTEXTSERVICEPROPERTY('LoadOSResources');";
                    bool needsConfig = false;
                    using (SqlCommand cmd = new SqlCommand(checkSql, con))
                    {
                        cmd.CommandTimeout = 2;
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            int loadOsResources = Convert.ToInt32(result);
                            if (loadOsResources == 0)
                            {
                                needsConfig = true;
                            }
                        }
                        else
                        {
                            // If it returns null for some reason but FTS is installed, assume it needs config or it's an error.
                            // However, we only strictly configure if we know it's 0.
                        }
                    }

                    // 3. Apply if missing
                    if (needsConfig)
                    {
                        string applySql = @"
                            EXEC sp_fulltext_service 'load_os_resources', 1;
                            EXEC sp_fulltext_service 'verify_signature', 0;
                            EXEC sp_fulltext_service 'restart_all_fdhosts';
                        ";
                        using (SqlCommand cmd = new SqlCommand(applySql, con))
                        {
                            cmd.CommandTimeout = 5; // allow a bit more time for fdhost restart, but the check itself is fast
                            cmd.ExecuteNonQuery();
                        }
                        logMessage = "Success: Detected missing load_os_resources setting. Enabled load_os_resources, disabled signature verification, and restarted fdhosts.";
                    }
                    else
                    {
                        logMessage = "Success: Database environment is already compliant. load_os_resources is enabled.";
                    }
                }
            }
            catch (SqlException ex)
            {
                logMessage = "Failed (Permissions Denied or SQL Error): " + ex.Message;
            }
            catch (Exception ex)
            {
                logMessage = "Failed: " + ex.Message;
            }

            LogStartup(logMessage);
        }

        private void LogStartup(string message)
        {
            try
            {
                string logDir = HostingEnvironment.MapPath("~/App_Data");
                if (!string.IsNullOrEmpty(logDir))
                {
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    string logFile = Path.Combine(logDir, "startup_log.txt");
                    File.AppendAllText(logFile, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - {message}{Environment.NewLine}");
                }
                
                System.Diagnostics.Trace.WriteLine($"Startup Check: {message}");
            }
            catch
            {
                // Fail silently on logging errors to avoid crashing application startup
            }
        }
    }
}
