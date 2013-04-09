CREATE TABLE [dbo].[Article](
	[ArticleId] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[UrlFriendlyTitle] [nvarchar](100) NOT NULL,
	[Text] [nvarchar](max) NOT NULL,
	[TextHash] [binary](16) NOT NULL,
	[Revision] [int] NOT NULL,
	[LastUpdatedDtm] [datetime] NOT NULL,
	[LastUpdatedBy] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Article] PRIMARY KEY NONCLUSTERED 
(
	[ArticleId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_PADDING OFF
GO
