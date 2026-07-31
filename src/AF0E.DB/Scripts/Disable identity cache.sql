/*
    Disable SQL Server identity caching for this database.

    Symptom:
      PotaActivations.ActivationId sometimes jumps by 1000 after creating a new activation.

    Cause:
      SQL Server caches IDENTITY values for int columns. If the SQL Server service restarts,
      crashes, fails over, or the database is otherwise recovered before the cache is consumed,
      SQL Server discards the unused cached values. The next insert can therefore advance by
      roughly 1000 even though the identity increment is still 1.

    Effect:
      This prevents future identity cache jumps for all IDENTITY columns in the current database.
      It does not renumber existing rows or remove already-created gaps.

    Requirements:
      SQL Server 2017+ / Azure SQL Database, and permission to alter the database-scoped
      configuration. For SQL Server 2012-2016, use instance trace flag 272 instead.
*/

EXEC(N'ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = OFF;');
GO

SELECT
    name,
    value,
    value_for_secondary
FROM sys.database_scoped_configurations
WHERE name = N'IDENTITY_CACHE';
GO

EXEC(N'DBCC CHECKIDENT (N''dbo.PotaActivations'', NORESEED);');
GO

