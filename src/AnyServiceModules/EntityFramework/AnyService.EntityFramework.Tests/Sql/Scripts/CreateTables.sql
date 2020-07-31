SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SqlBulkTestClasses](
	[Id] [uniqueidentifier] NOT NULL,
	[Value] [int] NOT NULL
	)
GO

ALTER TABLE [dbo].[SqlBulkTestClasses] ADD  DEFAULT (newsequentialid()) FOR [Id]
GO