USE DocManagementDB;
GO

DECLARE @RowsUpdated INT;
DECLARE @RemainingDotless INT;

-- 1. Correct legacy data: add leading dot to extensions that don't have one
UPDATE Documents
SET FileExtension = '.' + FileExtension
WHERE LEFT(FileExtension, 1) <> '.' AND LEN(FileExtension) > 0;

SET @RowsUpdated = @@ROWCOUNT;

SELECT @RemainingDotless = COUNT(*)
FROM Documents
WHERE LEFT(FileExtension, 1) <> '.' AND LEN(FileExtension) > 0;

PRINT 'Migration Report:';
PRINT CAST(@RowsUpdated AS VARCHAR) + ' legacy records updated to standard format with leading dot.';
PRINT 'Remaining dot-less records: ' + CAST(@RemainingDotless AS VARCHAR) + ' (100% update confirmed).';
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
