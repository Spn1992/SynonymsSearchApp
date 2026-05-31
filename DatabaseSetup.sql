-- 0. Verify required iFilters are present
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_document_types WHERE document_type = '.pdf')
   OR NOT EXISTS (SELECT 1 FROM sys.fulltext_document_types WHERE document_type = '.docx')
BEGIN
    RAISERROR ('Required Full-Text Search iFilters (.pdf, .docx) are missing on this server. Setup aborted.', 16, 1);
    SET NOEXEC ON;
END
GO

-- 1. Create the Database
CREATE DATABASE DocManagementDB;
GO

USE DocManagementDB;
GO

-- 2. Create the Documents Table
-- Note: A FileExtension column is required to full-text index a VARBINARY(MAX) column.
CREATE TABLE Documents
(
    RecordId INT IDENTITY(1,1) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NULL,
    FileExtension NVARCHAR(50) NOT NULL,
    FileData VARBINARY(MAX) NOT NULL,
    CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (RecordId)
);
GO

-- Check if Full-Text Search is installed
IF SERVERPROPERTY('IsFullTextInstalled') = 1
BEGIN
    -- 3. Create a Full-Text Catalog
    EXEC('CREATE FULLTEXT CATALOG DocCatalog AS DEFAULT');

    -- 4. Create a Full-Text Index
    -- The TYPE COLUMN allows the full-text engine to know how to parse the VARBINARY(MAX) data
    -- (e.g., .txt, .pdf, .docx).
    EXEC('CREATE FULLTEXT INDEX ON Documents
    (
        FileName Language 1033,
        FileData TYPE COLUMN FileExtension Language 1033
    )
    KEY INDEX PK_Documents
    ON DocCatalog
    WITH CHANGE_TRACKING AUTO');

    -- Confirm existence of catalog before applying service-level configurations
    IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'DocCatalog')
    BEGIN
        EXEC sp_fulltext_service 'load_os_resources', 1;
    END
END
ELSE
BEGIN
    PRINT 'WARNING: Full-Text Search components are not installed on this SQL Server instance. Document content indexing will be skipped.';
END
GO

-- 5. Trigger for Normalizing File Extensions
CREATE TRIGGER trg_NormalizeFileExtension
ON Documents
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
        RETURN;

    UPDATE d
    SET FileExtension = CASE 
                            WHEN left(ltrim(rtrim(i.FileExtension)), 1) = '.' 
                            THEN ltrim(rtrim(i.FileExtension))
                            ELSE '.' + ltrim(rtrim(i.FileExtension))
                        END
    FROM Documents d
    INNER JOIN inserted i ON d.RecordId = i.RecordId
    WHERE d.FileExtension <> CASE 
                            WHEN left(ltrim(rtrim(i.FileExtension)), 1) = '.' 
                            THEN ltrim(rtrim(i.FileExtension))
                            ELSE '.' + ltrim(rtrim(i.FileExtension))
                        END;
END;
GO

SET NOEXEC OFF;
GO

