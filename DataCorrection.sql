USE DocManagementDB;
GO

-- 1. Correct legacy data: add leading dot and remove whitespace
UPDATE Documents
SET FileExtension = CASE 
                        WHEN left(ltrim(rtrim(FileExtension)), 1) = '.' 
                        THEN ltrim(rtrim(FileExtension))
                        ELSE '.' + ltrim(rtrim(FileExtension))
                    END
WHERE FileExtension <> CASE 
                        WHEN left(ltrim(rtrim(FileExtension)), 1) = '.' 
                        THEN ltrim(rtrim(FileExtension))
                        ELSE '.' + ltrim(rtrim(FileExtension))
                    END;
GO

-- 2. Trigger a refresh of the full-text index to ensure content becomes discoverable
ALTER FULLTEXT INDEX ON Documents START FULL POPULATION;
GO
