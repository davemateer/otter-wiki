CREATE TABLE [dbo].[Article](
	[ArticleId] [int] IDENTITY(1,1) NOT NULL,
	[Revision] [int] NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[UrlTitle] [nvarchar](100) NOT NULL,
	[UpdatedDtm] [datetime] NOT NULL,
	[UpdatedBy] [nvarchar](50) NOT NULL,
	[Comment] [nvarchar](50) NOT NULL,
	[Text] [nvarchar](max) NOT NULL,
	[Html] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Article] PRIMARY KEY NONCLUSTERED 
(
	[ArticleId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE UNIQUE CLUSTERED INDEX [IX_Article_UrlTitle] ON [dbo].[Article] 
(
	[UrlTitle] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

