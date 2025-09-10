-- Use this command to restore the Kentico.Community database from a backup file.
-- Make sure to adjust the file paths and database names as necessary.

-- Only try to alter/drop the database if it exists
IF EXISTS (SELECT name
FROM sys.databases
WHERE name = 'Kentico.Community')
BEGIN
    ALTER DATABASE [Kentico.Community] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [Kentico.Community];
END

-- Restore the database - let SQL Server determine file paths automatically
RESTORE DATABASE [Kentico.Community]
FROM DISK = '/var/backups/Kentico.Community.bak'
WITH REPLACE;