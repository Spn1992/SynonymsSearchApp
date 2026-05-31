using System;
using System.Configuration;
using System.Data.SqlClient;

namespace DocManagement
{
    public static class DiagnosticLogger
    {
        public static void LogError(string message, Exception ex = null)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Ensure table exists just in case DatabaseSetup.sql was not run
                    string createTableQuery = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DiagnosticLogs' AND xtype='U')
                        CREATE TABLE DiagnosticLogs (
                            LogId INT IDENTITY(1,1) PRIMARY KEY,
                            LogTime DATETIME DEFAULT GETDATE(),
                            LogLevel NVARCHAR(50),
                            Message NVARCHAR(MAX),
                            ExceptionDetails NVARCHAR(MAX)
                        );";
                    using (SqlCommand cmd = new SqlCommand(createTableQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    string query = @"INSERT INTO DiagnosticLogs (LogLevel, Message, ExceptionDetails) 
                                     VALUES (@LogLevel, @Message, @ExceptionDetails)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@LogLevel", "Error");
                        cmd.Parameters.AddWithValue("@Message", message ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ExceptionDetails", ex?.ToString() ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Fallback: If DB logging fails, ignore to avoid crashing
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"INSERT INTO DiagnosticLogs (LogLevel, Message, ExceptionDetails) 
                                     VALUES (@LogLevel, @Message, @ExceptionDetails)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@LogLevel", "Info");
                        cmd.Parameters.AddWithValue("@Message", message ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ExceptionDetails", DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }
        }
    }
}
