-- 0. Check for Required iFilters
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_document_types WHERE document_type = '.pdf')
   OR NOT EXISTS (SELECT 1 FROM sys.fulltext_document_types WHERE document_type = '.docx')
BEGIN
    THROW 50000, 'Required iFilters (.pdf, .docx) are missing. Please install them before proceeding.', 1;
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
    SearchExtension AS (CASE 
                            WHEN left(ltrim(rtrim(FileExtension)), 1) = '.' 
                            THEN ltrim(rtrim(FileExtension))
                            ELSE '.' + ltrim(rtrim(FileExtension))
                        END),
    CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (RecordId)
);
GO

-- Check if Full-Text Search is installed
IF SERVERPROPERTY('IsFullTextInstalled') = 1
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
        RETURN;

    UPDATE d
    SET FileExtension = LOWER(CASE 
                            WHEN left(ltrim(rtrim(i.FileExtension)), 1) = '.' 
                            THEN ltrim(rtrim(i.FileExtension))
                            ELSE '.' + ltrim(rtrim(i.FileExtension))
                        END)
    FROM Documents d
    INNER JOIN inserted i ON d.RecordId = i.RecordId
    WHERE d.FileExtension <> LOWER(CASE 
                            WHEN left(ltrim(rtrim(i.FileExtension)), 1) = '.' 
                            THEN ltrim(rtrim(i.FileExtension))
                            ELSE '.' + ltrim(rtrim(i.FileExtension))
                        END);
END;
GO
