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

-- 3. Create a Full-Text Catalog
CREATE FULLTEXT CATALOG DocCatalog AS DEFAULT;
GO

-- 4. Create a Full-Text Index
-- The TYPE COLUMN allows the full-text engine to know how to parse the VARBINARY(MAX) data
-- (e.g., .txt, .pdf, .docx).
CREATE FULLTEXT INDEX ON Documents
(
    FileName Language 1033,
    FileData TYPE COLUMN FileExtension Language 1033
)
KEY INDEX PK_Documents
ON DocCatalog
WITH CHANGE_TRACKING AUTO;
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

