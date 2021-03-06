/****** Object:  DatabaseRole [otter_application]    Script Date: 8/13/2015 11:16:04 AM ******/
CREATE ROLE [otter_application]
GO
ALTER ROLE [db_datareader] ADD MEMBER [otter_application]
GO
/****** Object:  UserDefinedTableType [dbo].[SecurityEntityTable]    Script Date: 8/13/2015 11:16:04 AM ******/
CREATE TYPE [dbo].[SecurityEntityTable] AS TABLE(
	[EntityId] [nvarchar](256) NULL
)
GO
/****** Object:  UserDefinedFunction [dbo].[ufx_GetConfigurationValueInt]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- FN ufx_GetConfigurationValueInt
CREATE FUNCTION [dbo].[ufx_GetConfigurationValueInt] (
	@ConfigKey varchar(50)
	,@Default int
) RETURNS int AS BEGIN

	DECLARE @Result int
	DECLARE @TempVariant sql_variant
	
	SELECT @TempVariant = ConfigValue
	FROM dbo.Configuration
	WHERE ConfigKey = @ConfigKey
	
	IF ( SQL_VARIANT_PROPERTY(@TempVariant, 'BaseType') IN ('int', 'smallint', 'tinyint', 'bit') ) BEGIN
	
		SET @Result = CAST(@TempVariant AS int)
		
	END ELSE IF ( SQL_VARIANT_PROPERTY(@TempVariant, 'BaseType') IN ('nvarchar', 'nchar', 'varchar', 'char') ) BEGIN
	
		DECLARE @TempString varchar(max)
		SET @TempString = CAST(@TempVariant AS varchar)
		
		IF ( ISNUMERIC(@TempString + '0e0') = 1 AND LEN(@TempString) <= 37 ) BEGIN
		
			DECLARE @TempDecimal decimal(38,0)
			SET @TempDecimal = CAST(@TempString as decimal(38,0))
			
			IF ( @TempDecimal BETWEEN -2147483648 AND 2147483647 ) BEGIN
				SET @Result = CAST(@TempDecimal as int)
			END
		
		END
	
	END

	RETURN ISNULL(@Result, @Default)

END

GO
/****** Object:  Table [dbo].[Article]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Article](
	[ArticleId] [int] IDENTITY(1,1) NOT NULL,
	[Revision] [int] NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[UrlTitle] [nvarchar](100) NOT NULL,
	[CreatedBy] [nvarchar](50) NOT NULL,
	[CreatedWhen] [datetime] NOT NULL,
	[UpdatedBy] [nvarchar](50) NOT NULL,
	[UpdatedWhen] [datetime] NOT NULL,
	[Comment] [nvarchar](100) NOT NULL,
	[Text] [nvarchar](max) NOT NULL,
	[Html] [nvarchar](max) NOT NULL,
	[LastReviewedBy] [nvarchar](50) NOT NULL,
	[LastReviewedWhen] [datetime] NOT NULL,
	[LastViewedBy] [nvarchar](50) NOT NULL,
	[LastViewedWhen] [datetime] NOT NULL,
 CONSTRAINT [PK_Article] PRIMARY KEY NONCLUSTERED 
(
	[ArticleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_Article_UrlTitle]    Script Date: 8/13/2015 11:16:04 AM ******/
CREATE UNIQUE CLUSTERED INDEX [IX_Article_UrlTitle] ON [dbo].[Article]
(
	[UrlTitle] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ArticleAttachment]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ArticleAttachment](
	[ArticleAttachmentId] [int] IDENTITY(1,1) NOT NULL,
	[ArticleId] [int] NOT NULL,
	[Filename] [varchar](100) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_ArticleAttachment] PRIMARY KEY CLUSTERED 
(
	[ArticleAttachmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ArticleHistory]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ArticleHistory](
	[ArticleId] [int] NOT NULL,
	[Revision] [int] NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[UpdatedBy] [nvarchar](50) NOT NULL,
	[UpdatedWhen] [datetime] NOT NULL,
	[Comment] [nvarchar](100) NOT NULL,
	[Delta] [nvarchar](max) NOT NULL,
	[Text] [nvarchar](max) NULL,
 CONSTRAINT [PK_ArticleHistory] PRIMARY KEY CLUSTERED 
(
	[ArticleId] ASC,
	[Revision] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[ArticleImage]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ArticleImage](
	[ArticleImageId] [int] IDENTITY(1,1) NOT NULL,
	[ArticleId] [int] NOT NULL,
	[Filename] [varchar](100) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_ArticleImage] PRIMARY KEY CLUSTERED 
(
	[ArticleImageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ArticleSecurity]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ArticleSecurity](
	[ArticleId] [int] NOT NULL,
	[Scope] [char](1) NOT NULL,
	[EntityId] [nvarchar](256) NOT NULL,
	[Permission] [char](1) NOT NULL,
 CONSTRAINT [PK_ArticleSecurity] PRIMARY KEY CLUSTERED 
(
	[ArticleId] ASC,
	[Scope] ASC,
	[EntityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ArticleTag]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ArticleTag](
	[ArticleId] [int] NOT NULL,
	[Tag] [nvarchar](30) NOT NULL,
 CONSTRAINT [PK_ArticleTag] PRIMARY KEY NONCLUSTERED 
(
	[ArticleId] ASC,
	[Tag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_ArticleTag_Tag]    Script Date: 8/13/2015 11:16:04 AM ******/
CREATE CLUSTERED INDEX [IX_ArticleTag_Tag] ON [dbo].[ArticleTag]
(
	[Tag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Configuration]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Configuration](
	[ConfigKey] [varchar](50) NOT NULL,
	[ConfigValue] [sql_variant] NULL,
	[Description] [varchar](200) NULL,
 CONSTRAINT [PK_Configuration] PRIMARY KEY CLUSTERED 
(
	[ConfigKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_ArticleId_Filename]    Script Date: 8/13/2015 11:16:04 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleId_Filename] ON [dbo].[ArticleAttachment]
(
	[ArticleId] ASC,
	[Filename] ASC
)
INCLUDE ( 	[Title]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_ArticleId_Filename]    Script Date: 8/13/2015 11:16:04 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleId_Filename] ON [dbo].[ArticleImage]
(
	[ArticleId] ASC,
	[Filename] ASC
)
INCLUDE ( 	[Title]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ArticleAttachment]  WITH CHECK ADD  CONSTRAINT [FK_ArticleAttachment_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO
ALTER TABLE [dbo].[ArticleAttachment] CHECK CONSTRAINT [FK_ArticleAttachment_Article]
GO
ALTER TABLE [dbo].[ArticleHistory]  WITH CHECK ADD  CONSTRAINT [FK_ArticleHistory_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO
ALTER TABLE [dbo].[ArticleHistory] CHECK CONSTRAINT [FK_ArticleHistory_Article]
GO
ALTER TABLE [dbo].[ArticleImage]  WITH CHECK ADD  CONSTRAINT [FK_ArticleImage_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO
ALTER TABLE [dbo].[ArticleImage] CHECK CONSTRAINT [FK_ArticleImage_Article]
GO
ALTER TABLE [dbo].[ArticleSecurity]  WITH CHECK ADD  CONSTRAINT [FK_ArticleSecurity_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO
ALTER TABLE [dbo].[ArticleSecurity] CHECK CONSTRAINT [FK_ArticleSecurity_Article]
GO
ALTER TABLE [dbo].[ArticleTag]  WITH CHECK ADD  CONSTRAINT [FK_ArticleTag_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO
ALTER TABLE [dbo].[ArticleTag] CHECK CONSTRAINT [FK_ArticleTag_Article]
GO
ALTER TABLE [dbo].[ArticleSecurity]  WITH CHECK ADD  CONSTRAINT [CK_ArticleSecurity_Permission] CHECK  (([Permission]='M' OR [Permission]='V'))
GO
ALTER TABLE [dbo].[ArticleSecurity] CHECK CONSTRAINT [CK_ArticleSecurity_Permission]
GO
ALTER TABLE [dbo].[ArticleSecurity]  WITH CHECK ADD  CONSTRAINT [CK_ArticleSecurity_Scope] CHECK  (([Scope]='I' OR [Scope]='G' OR [Scope]='E'))
GO
ALTER TABLE [dbo].[ArticleSecurity] CHECK CONSTRAINT [CK_ArticleSecurity_Scope]
GO
ALTER TABLE [dbo].[ArticleTag]  WITH CHECK ADD  CONSTRAINT [CK_ArticleTag_TagLength] CHECK  ((len([Tag])>=(3)))
GO
ALTER TABLE [dbo].[ArticleTag] CHECK CONSTRAINT [CK_ArticleTag_TagLength]
GO
/****** Object:  StoredProcedure [dbo].[up_Article_GetSummaryByTag]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_Article_GetSummaryByTag
CREATE PROCEDURE [dbo].[up_Article_GetSummaryByTag] (
	@UserId nvarchar(256)
	,@UserGroups SecurityEntityTable READONLY
) AS

SET NOCOUNT ON

SELECT
	t.Tag
	,COUNT(*) AS CountOfArticles
FROM
	dbo.Article a
	INNER JOIN dbo.ArticleSecurity s ON s.ArticleId = a.ArticleId
	LEFT OUTER JOIN dbo.ArticleTag t ON t.ArticleId = a.ArticleId
WHERE
	s.Scope = 'E'
	OR ( s.Scope = 'G' AND s.EntityId IN ( SELECT EntityId FROM @UserGroups ) )
	OR ( s.Scope = 'I' AND s.EntityId = @UserId )
GROUP BY
	t.Tag

GO
/****** Object:  StoredProcedure [dbo].[up_Article_Insert]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_Article_Insert
CREATE PROCEDURE [dbo].[up_Article_Insert] (
	@ArticleId int OUTPUT
	,@Title nvarchar(100)
	,@UrlTitle nvarchar(100)
	,@Text nvarchar(max)
	,@Html nvarchar(max)
	,@UpdatedBy nvarchar(50)
) AS

SET NOCOUNT ON

INSERT INTO dbo.Article (
	[Title]
   ,[UrlTitle]
   ,[Text]
   ,[Html]
   ,[Revision]
   ,[CreatedBy]
   ,[CreatedWhen]
   ,[UpdatedBy]
   ,[UpdatedWhen]
   ,[LastReviewedBy]
   ,[LastReviewedWhen]
   ,[LastViewedBy]
   ,[LastViewedWhen]
   ,[Comment]
) VALUES (
	@Title
	,@UrlTitle
	,@Text
	,@Html
	,1  -- Revision
	,@UpdatedBy
	,GETDATE()
	,@UpdatedBy
	,GETDATE()
	,@UpdatedBy
	,GETDATE()
	,@UpdatedBy
	,GETDATE()
	,'Initial version'
)

SET @ArticleId = @@IDENTITY


GO
/****** Object:  StoredProcedure [dbo].[up_Article_SelectRevisionTextDeltaSequence]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_Article_SelectRevisionTextDeltaSequence
CREATE PROCEDURE [dbo].[up_Article_SelectRevisionTextDeltaSequence] (
	@ArticleId int,
	@Revision int
) AS

SET NOCOUNT ON

DECLARE @ClosestSnapshotRevision int

SELECT @ClosestSnapshotRevision = Revision
FROM dbo.ArticleHistory
WHERE 
	[Text] IS NOT NULL
	AND Revision < @Revision
	
SELECT a.Revision, a.[Text]
FROM dbo.Article a
WHERE
	@ClosestSnapshotRevision IS NULL
	AND a.ArticleId = @ArticleId

UNION

SELECT ah.Revision, ah.[Text]
FROM dbo.ArticleHistory ah
WHERE
	ah.ArticleId = @ArticleId
	AND @ClosestSnapshotRevision IS NOT NULL
	AND ah.Revision = @ClosestSnapshotRevision

UNION

SELECT ah.Revision, ah.Delta
FROM dbo.ArticleHistory ah
WHERE
	ah.ArticleId = @ArticleId
	AND ah.Revision >= @Revision
	AND (
		@ClosestSnapshotRevision IS NULL
		OR ( @ClosestSnapshotRevision IS NOT NULL AND ah.Revision < @ClosestSnapshotRevision )
	)
	
ORDER BY 1 DESC  -- Order by Revision, from most recent to earliest.


GO
/****** Object:  StoredProcedure [dbo].[up_Article_Update]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_Article_Update
CREATE PROCEDURE [dbo].[up_Article_Update] (
	@ArticleId int
	,@Title nvarchar(100)
	,@UrlTitle nvarchar(100)
	,@UpdatedBy nvarchar(50)
	,@Comment nvarchar(100)
	,@Text nvarchar(max)
	,@Html nvarchar(max)
	,@Delta nvarchar(max)
) AS

SET NOCOUNT ON

DECLARE @SnapshotInterval int
SET @SnapshotInterval = dbo.ufx_GetConfigurationValueInt('SnapshotInterval', 50)

DECLARE @LastError int
DECLARE @Revision int

SELECT @Revision = Revision + 1
FROM dbo.Article
WHERE ArticleId = @ArticleId

BEGIN TRANSACTION

	INSERT INTO [dbo].[ArticleHistory] (
		[ArticleId]
		,[Revision]
		,[UpdatedWhen]
		,[UpdatedBy]
		,[Comment]
		,[Title]
		,[Delta]
		,[Text]
	)
	SELECT
		a.ArticleId
		,a.Revision
		,a.UpdatedWhen
		,a.UpdatedBy
		,a.Comment
		,a.Title
		,@Delta
		,CASE
			WHEN a.Revision % @SnapshotInterval = 0 THEN a.[Text]
			ELSE NULL
		END
	FROM dbo.Article a
	WHERE a.ArticleId = @ArticleId
	
	SET @LastError = @@ERROR
	IF (@LastError <> 0) BEGIN
		ROLLBACK
		RETURN @@ERROR
	END

	UPDATE [dbo].[Article]
	SET
		[Title] = @Title
		,[UrlTitle] = @UrlTitle
		,[Text] = @Text
		,[Html] = @Html
		,[Revision] = @Revision
		,[UpdatedWhen] = GETDATE()
		,[UpdatedBy] = @UpdatedBy
		,[Comment] = @Comment
	WHERE ArticleId = @ArticleId

	SET @LastError = @@ERROR
	IF (@LastError <> 0) BEGIN
		ROLLBACK
		RETURN @@ERROR
	END

COMMIT


GO
/****** Object:  StoredProcedure [dbo].[up_Article_View]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[up_Article_View] (
	@ArticleId int
	,@ViewedBy nvarchar(50)
) AS

SET NOCOUNT ON

UPDATE Article 
SET LastViewedBy = @ViewedBy, LastViewedWhen = GETDATE()
WHERE ArticleId = @ArticleId
GO
/****** Object:  StoredProcedure [dbo].[up_ArticleAttachment_Delete]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[up_ArticleAttachment_Delete] (
	@ArticleAttachmentId int
) AS

SET NOCOUNT ON

DELETE FROM dbo.ArticleAttachment
WHERE ArticleAttachmentId = @ArticleAttachmentId

GO
/****** Object:  StoredProcedure [dbo].[up_ArticleAttachment_Insert]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[up_ArticleAttachment_Insert] (
	@ArticleId int
	,@Filename varchar(100)
	,@Title nvarchar(100)
) AS

SET NOCOUNT ON

INSERT INTO dbo.ArticleAttachment ([ArticleId],[Filename],[Title])
VALUES (@ArticleId, @Filename, @Title)

GO
/****** Object:  StoredProcedure [dbo].[up_ArticleImage_Delete]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_ArticleImage_Delete
CREATE PROCEDURE [dbo].[up_ArticleImage_Delete] (
	@ArticleImageId int
) AS

SET NOCOUNT ON

DELETE FROM dbo.ArticleImage
WHERE ArticleImageId = @ArticleImageId

GO
/****** Object:  StoredProcedure [dbo].[up_ArticleImage_Insert]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_ArticleImage_Insert
CREATE PROCEDURE [dbo].[up_ArticleImage_Insert] (
	@ArticleId int
	,@Filename varchar(100)
	,@Title nvarchar(100)
) AS

SET NOCOUNT ON

INSERT INTO dbo.ArticleImage ([ArticleId],[Filename],[Title])
VALUES (@ArticleId, @Filename, @Title)


GO
/****** Object:  StoredProcedure [dbo].[up_ArticleSecurity_Delete]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_ArticleSecurity_Delete
CREATE PROCEDURE [dbo].[up_ArticleSecurity_Delete] (
	@ArticleId int
	,@Scope char(1)
	,@EntityId nvarchar(256)
) AS

SET NOCOUNT ON

DELETE FROM [dbo].[ArticleSecurity]
WHERE
	ArticleId = @ArticleId
	AND Scope = @Scope
	AND EntityId = @EntityId


GO
/****** Object:  StoredProcedure [dbo].[up_ArticleSecurity_Update]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_ArticleSecurity_Update
CREATE PROCEDURE [dbo].[up_ArticleSecurity_Update] (
	@ArticleId int
	,@Scope char(1)
	,@EntityId nvarchar(256)
	,@Permission char(1)
) AS

SET NOCOUNT ON

MERGE dbo.ArticleSecurity s
USING ( 
    VALUES (@ArticleId, @Scope, @EntityId)
) AS update_values (ArticleId, Scope, EntityId) 
ON
	s.ArticleId = update_values.ArticleId
	AND s.Scope = update_values.Scope
	AND s.EntityId = update_values.EntityId
WHEN MATCHED THEN
   UPDATE SET s.Permission = @Permission
WHEN NOT MATCHED THEN
   INSERT (ArticleId, Scope, EntityId, Permission) VALUES (@ArticleId, @Scope, @EntityId, @Permission)
;


GO
/****** Object:  StoredProcedure [dbo].[up_ArticleTag_Delete]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_ArticleTag_Delete
CREATE PROCEDURE [dbo].[up_ArticleTag_Delete] (
	@ArticleId int
	,@Tag nvarchar(30)
) AS

SET NOCOUNT ON

DELETE FROM [dbo].[ArticleTag]
WHERE
	ArticleId = @ArticleId
	AND Tag = @Tag


GO
/****** Object:  StoredProcedure [dbo].[up_ArticleTag_Insert]    Script Date: 8/13/2015 11:16:04 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- P  up_ArticleTag_Insert
CREATE PROCEDURE [dbo].[up_ArticleTag_Insert] (
	@ArticleId int,
	@Tag nvarchar(30)
) AS

SET NOCOUNT ON

INSERT INTO [dbo].[ArticleTag] ( [ArticleId], [Tag] )
VALUES ( @ArticleId, @Tag )



GO
