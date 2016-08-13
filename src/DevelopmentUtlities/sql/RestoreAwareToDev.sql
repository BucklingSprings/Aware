USE aware;
GO
DROP DATABASE aware_dev;
Go

BACKUP DATABASE aware
TO DISK = 'C:\aware_dev\aware.Bak'
   WITH FORMAT,
      MEDIANAME = 'C_Aware_dev',
      NAME = 'Full Backup of Aware';

GO
RESTORE FILELISTONLY 
   FROM DISK = 'C:\aware_dev\aware.Bak';

GO

RESTORE DATABASE aware_dev
   FROM DISK = 'C:\aware_dev\aware.Bak'
   WITH MOVE 'awareDb.mdf' TO 'C:\aware_dev\awareDb.mdf',
   MOVE 'awareDb_log.ldf' TO 'C:\aware_dev\awareDb_log.ldf';


USE aware_dev;
GO
   select count(*) From ActivitySamples;



	use aware_dev;
exec sp_Help __MigrationHistory;

/*
ALTER TABLE dbo.__MigrationHistory DROP CONSTRAINT [PK_dbo.__MigrationHistory2];
ALTER TABLE dbo.__MigrationHistory DROP COLUMN ContextKey;
ALTER TABLE dbo.__MigrationHistory ADD CONSTRAINT [PK_dbo.__MigrationHistory] PRIMARY KEY (MigrationId);
*/