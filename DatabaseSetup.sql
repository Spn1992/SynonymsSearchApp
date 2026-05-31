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

-- 3. Create a Full-Text Catalog
CREATE FULLTEXT CATALOG DocCatalog AS DEFAULT;
GO

-- 4. Create a Full-Text Index
-- The TYPE COLUMN allows the full-text engine to know how to parse the VARBINARY(MAX) data
-- (e.g., .txt, .pdf, .docx).
CREATE FULLTEXT INDEX ON Documents
(
    FileName Language 1033,
    FileData TYPE COLUMN SearchExtension Language 1033
)
KEY INDEX PK_Documents
ON DocCatalog
WITH CHANGE_TRACKING AUTO;
GO
