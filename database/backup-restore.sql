-- Use this command to restore the Kentico.Community database from a backup file.
-- Make sure to adjust the file paths and database names as necessary.

ALTER DATABASE [Kentico.Community] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [Kentico.Community]

RESTORE DATABASE [Kentico.Community]
FROM DISK = '/var/backups/Kentico.Community.bak'
WITH MOVE 'Kentico.Community' TO '/var/opt/mssql/data/Kentico.Community.mdf',
MOVE 'Kentico.Community_Log' TO '/var/opt/mssql/data/Kentico.Community_Log.ldf'