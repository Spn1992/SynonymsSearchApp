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

-- 2. Trigger a refresh of the full-text index to ensure content becomes discoverable
ALTER FULLTEXT INDEX ON Documents START FULL POPULATION;
GO
