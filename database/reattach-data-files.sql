-- Kill any existing connections
USE [master];
ALTER DATABASE [Kentico.Community] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Detach the database
EXEC sp_detach_db 'Kentico.Community', 'true';

-- Reattach the database
CREATE DATABASE [Kentico.Community]
ON (FILENAME = '/var/opt/mssql/data/Kentico.Community.mdf'),
   (FILENAME = '/var/opt/mssql/data/Kentico.Community_Log.ldf')
FOR ATTACH;