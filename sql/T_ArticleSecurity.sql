CREATE TABLE [dbo].[ArticleSecurity](
	[ArticleId] [int] NOT NULL,
	[Scope] [char](1) NOT NULL,
	[EntityId] [nvarchar](50) NOT NULL,
	[Permission] [char](1) NOT NULL,
 CONSTRAINT [PK_ArticleSecurity] PRIMARY KEY CLUSTERED 
(
	[ArticleId] ASC,
	[Scope] ASC,
	[EntityId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[ArticleSecurity]  WITH CHECK ADD  CONSTRAINT [FK_ArticleSecurity_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO

ALTER TABLE [dbo].[ArticleSecurity] CHECK CONSTRAINT [FK_ArticleSecurity_Article]
GO

ALTER TABLE [dbo].[ArticleSecurity]  WITH CHECK ADD  CONSTRAINT [CK_ArticleSecurity_Permission] CHECK  (([Permission]='M' OR [Permission]='V'))
GO

ALTER TABLE [dbo].[ArticleSecurity] CHECK CONSTRAINT [CK_ArticleSecurity_Permission]
GO

ALTER TABLE [dbo].[ArticleSecurity]  WITH CHECK ADD  CONSTRAINT [CK_ArticleSecurity_Scope] CHECK  (([Scope]='I' OR [Scope]='G' OR [Scope]='E'))
GO

ALTER TABLE [dbo].[ArticleSecurity] CHECK CONSTRAINT [CK_ArticleSecurity_Scope]
GO

