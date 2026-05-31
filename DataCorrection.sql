USE DocManagementDB;
GO

-- 1. Correct legacy data: add leading dot to extensions that don't have one
UPDATE Documents
SET FileExtension = '.' + FileExtension
WHERE LEFT(FileExtension, 1) <> '.' AND LEN(FileExtension) > 0;
GO

-- 2. Trigger a refresh of the full-text index to ensure content becomes discoverable
ALTER FULLTEXT INDEX ON Documents START FULL POPULATION;
GO
