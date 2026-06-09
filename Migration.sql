USE DocManagementDB;
GO

-- 1. One-time data migration: Add leading dot to FileExtension if missing
UPDATE Documents
SET FileExtension = '.' + FileExtension
WHERE FileExtension NOT LIKE '.%';
GO
