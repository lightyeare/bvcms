ALTER TABLE [dbo].[MobileAppActionTypes] ADD CONSTRAINT [PK_MobileAppActionTypes] PRIMARY KEY CLUSTERED  ([id])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
