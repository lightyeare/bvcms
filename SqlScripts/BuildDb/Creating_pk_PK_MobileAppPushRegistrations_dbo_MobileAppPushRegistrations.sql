ALTER TABLE [dbo].[MobileAppPushRegistrations] ADD CONSTRAINT [PK_MobileAppPushRegistrations] PRIMARY KEY CLUSTERED  ([Id])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
