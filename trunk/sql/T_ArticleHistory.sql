CREATE TABLE [dbo].[ArticleHistory](
	[ArticleId] [int] NOT NULL,
	[Revision] [int] NOT NULL,
	[UpdatedDtm] [datetime] NOT NULL,
	[UpdatedBy] [nvarchar](50) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[Delta] [nvarchar](max) NOT NULL,
	[SnapshotText] [nvarchar](max) NULL,
 CONSTRAINT [PK_ArticleHistory] PRIMARY KEY CLUSTERED 
(
	[ArticleId] ASC,
	[Revision] DESC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[ArticleHistory]  WITH CHECK ADD  CONSTRAINT [FK_ArticleHistory_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO

ALTER TABLE [dbo].[ArticleHistory] CHECK CONSTRAINT [FK_ArticleHistory_Article]
GO


