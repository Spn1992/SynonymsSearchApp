USE DocManagementDB;
GO

-- 1. Correct legacy data: add leading dot, remove whitespace, and standardize to lowercase
UPDATE Documents
SET FileExtension = LOWER(CASE 
                        WHEN left(ltrim(rtrim(FileExtension)), 1) = '.' 
                        THEN ltrim(rtrim(FileExtension))
                        ELSE '.' + ltrim(rtrim(FileExtension))
                    END)
WHERE FileExtension <> LOWER(CASE 
                        WHEN left(ltrim(rtrim(FileExtension)), 1) = '.' 
                        THEN ltrim(rtrim(FileExtension))
                        ELSE '.' + ltrim(rtrim(FileExtension))
                    END);
GO

-- 2. Add the computed column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'SearchExtension')
BEGIN
    ALTER TABLE Documents ADD SearchExtension AS (CASE 
                            WHEN left(ltrim(rtrim(FileExtension)), 1) = '.' 
                            THEN ltrim(rtrim(FileExtension))
                            ELSE '.' + ltrim(rtrim(FileExtension))
                        END);
END
GO

-- 3. Update Full-Text Index to use the new computed column
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1 AND EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Documents'))
BEGIN
    ALTER FULLTEXT INDEX ON Documents DROP (FileData);
    ALTER FULLTEXT INDEX ON Documents ADD (FileData TYPE COLUMN SearchExtension Language 1033);
    
    -- 4. Trigger a refresh of the full-text index
    ALTER FULLTEXT INDEX ON Documents START FULL POPULATION;
END
GO
