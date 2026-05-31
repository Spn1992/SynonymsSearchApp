# Document Management System Setup Guide

## 1. Database Setup
To set up the document management system database, execute the `DatabaseSetup.sql` script on your SQL Server instance. This script requires sysadmin or serveradmin permissions as it performs instance-level configurations.

## 2. Full-Text Search Configuration
In order to search for text within uploaded documents (like PDFs), Full-Text Search must be configured to load OS resources (filters) which allow reading binary data. The `DatabaseSetup.sql` script automatically enables the `load_os_resources` setting using `sp_fulltext_service`.

### For Existing Installations (Retroactive Fix)
If your system is already installed and file content search is not working, you can manually enable this configuration by executing the following SQL command with sysadmin permissions:
```sql
EXEC sp_fulltext_service 'load_os_resources', 1;
```

### Service Restart Required
**IMPORTANT:** After running the database setup script, you must restart the **SQL Full-text Filter Daemon Launcher** service on your SQL Server for the changes to take effect. If you do not restart this service, document content will not be searchable.

To restart the service:
1. Open SQL Server Configuration Manager.
2. Select "SQL Server Services".
3. Right-click on "SQL Full-text Filter Daemon Launcher (<InstanceName>)" and select **Restart**.
   *(Alternatively, this can be done via Windows Services (services.msc)).*

### Manual Verification
After restarting the service, you can run the following SQL query to confirm that the indexing service is correctly configured and that OS resources (such as the PDF filter) are actively loaded:

```sql
SELECT document_type, path, version, manufacturer 
FROM sys.fulltext_document_types 
WHERE document_type = '.pdf';
```
If the query returns a row for `.pdf`, the service is correctly configured and the filter daemon has successfully loaded the OS resources.
