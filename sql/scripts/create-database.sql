-- UDTT SecurityEntityTable
CREATE TYPE [dbo].[SecurityEntityTable] AS TABLE(
	[EntityId] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
)
GO
-- FTC ArticleFullTextCatalog
CREATE FULLTEXT CATALOG [ArticleFullTextCatalog]WITH ACCENT_SENSITIVITY = OFF
AS DEFAULT

GO
-- TBL Configuration
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Configuration](
	[ConfigKey] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ConfigValue] [sql_variant] NULL,
	[Description] [varchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_Configuration] PRIMARY KEY CLUSTERED 
(
	[ConfigKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
-- TBL Article
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Article](
	[ArticleId] [int] IDENTITY(1,1) NOT NULL,
	[Revision] [int] NOT NULL,
	[Title] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UrlTitle] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UpdatedDtm] [datetime] NOT NULL,
	[UpdatedBy] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Comment] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Text] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Html] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Article] PRIMARY KEY NONCLUSTERED 
(
	[ArticleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING ON

GO

CREATE UNIQUE CLUSTERED INDEX [IX_Article_UrlTitle] ON [dbo].[Article]
(
	[UrlTitle] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE FULLTEXT INDEX ON [dbo].[Article](
[Text] LANGUAGE [English], 
[Title] LANGUAGE [English])
KEY INDEX [IX_Article_UrlTitle]ON ([ArticleFullTextCatalog], FILEGROUP [PRIMARY])
WITH (CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM)


GO
-- TBL ArticleHistory
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ArticleHistory](
	[ArticleId] [int] NOT NULL,
	[Revision] [int] NOT NULL,
	[Title] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[UpdatedDtm] [datetime] NOT NULL,
	[UpdatedBy] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Comment] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Delta] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Text] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_ArticleHistory] PRIMARY KEY CLUSTERED 
(
	[ArticleId] ASC,
	[Revision] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[ArticleHistory]  WITH CHECK ADD  CONSTRAINT [FK_ArticleHistory_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO

ALTER TABLE [dbo].[ArticleHistory] CHECK CONSTRAINT [FK_ArticleHistory_Article]
GO
-- TBL ArticleTag
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ArticleTag](
	[ArticleId] [int] NOT NULL,
	[Tag] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_ArticleTag] PRIMARY KEY NONCLUSTERED 
(
	[ArticleId] ASC,
	[Tag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING ON

GO

CREATE CLUSTERED INDEX [IX_ArticleTag_Tag] ON [dbo].[ArticleTag]
(
	[Tag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ArticleTag]  WITH CHECK ADD  CONSTRAINT [FK_ArticleTag_Article] FOREIGN KEY([ArticleId])
REFERENCES [dbo].[Article] ([ArticleId])
GO

ALTER TABLE [dbo].[ArticleTag] CHECK CONSTRAINT [FK_ArticleTag_Article]
GO

ALTER TABLE [dbo].[ArticleTag]  WITH CHECK ADD  CONSTRAINT [CK_ArticleTag_TagLength] CHECK  ((len([Tag])>=(3)))
GO

ALTER TABLE [dbo].[ArticleTag] CHECK CONSTRAINT [CK_ArticleTag_TagLength]
GO
-- TBL ArticleSecurity
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ArticleSecurity](
	[ArticleId] [int] NOT NULL,
	[Scope] [char](1) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[EntityId] [nvarchar](256) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Permission] [char](1) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_ArticleSecurity] PRIMARY KEY CLUSTERED 
(
	[ArticleId] ASC,
	[Scope] ASC,
	[EntityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
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
-- FN ufx_GetConfigurationValueInt
CREATE FUNCTION dbo.ufx_GetConfigurationValueInt (
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
		,[UpdatedDtm]
		,[UpdatedBy]
		,[Comment]
		,[Title]
		,[Delta]
		,[Text]
	)
	SELECT
		a.ArticleId
		,a.Revision
		,a.UpdatedDtm
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
		,[UpdatedDtm] = GETDATE()
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
-- P  up_Article_SelectRevisionTextDeltaSequence
CREATE PROCEDURE up_Article_SelectRevisionTextDeltaSequence (
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
-- P  up_ArticleTag_Insert
CREATE PROCEDURE dbo.up_ArticleTag_Insert (
	@ArticleId int,
	@Tag nvarchar(30)
) AS

SET NOCOUNT ON

INSERT INTO [dbo].[ArticleTag] ( [ArticleId], [Tag] )
VALUES ( @ArticleId, @Tag )


GO
-- P  up_Article_GetSummaryByTag
CREATE PROCEDURE dbo.up_Article_GetSummaryByTag (
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
-- P  up_Article_SearchByTag
CREATE PROCEDURE dbo.up_Article_SearchByTag (
	@Tag nvarchar(30)
	,@UserId nvarchar(256)
	,@UserGroups SecurityEntityTable READONLY
) AS

SET NOCOUNT ON

SELECT
	a.UrlTitle
	,a.Title
	,a.UpdatedBy
	,a.UpdatedDtm
FROM
	dbo.Article a
	INNER JOIN dbo.ArticleSecurity s ON s.ArticleId = a.ArticleId
	LEFT OUTER JOIN dbo.ArticleTag t ON t.ArticleId = a.ArticleId
WHERE
	( @Tag IS NULL AND t.Tag IS NULL )
	OR ( @Tag IS NOT NULL AND t.Tag = @Tag )
	AND (
		s.Scope = 'E'
		OR ( s.Scope = 'G' AND s.EntityId IN ( SELECT EntityId FROM @UserGroups ) )
		OR ( s.Scope = 'I' AND s.EntityId = @UserId )
	)
GO
-- P  up_Article_SearchByQuery

CREATE PROCEDURE [dbo].[up_Article_SearchByQuery] (
	@Query nvarchar(1000)
	,@UserId nvarchar(256)
	,@UserGroups SecurityEntityTable READONLY
) AS

-- Extremely naive search implementation. Tweak as necessary to get decent results.

SET NOCOUNT ON

DECLARE @TextWeight decimal
DECLARE @TitleWeight decimal
SET @TextWeight = 1.0
SET @TitleWeight = 1.0

SELECT
	a.UrlTitle
	,a.Title
	,a.UpdatedBy
	,a.UpdatedDtm
FROM
	(
		SELECT [Key], [RANK] * @TextWeight AS [Rank]
		FROM FREETEXTTABLE(dbo.Article, [Text], @Query)

		UNION ALL

		SELECT [Key], [RANK] * @TitleWeight
		FROM FREETEXTTABLE(dbo.Article, Title, @Query)
	) tResults
	INNER JOIN dbo.Article a ON a.UrlTitle = tResults.[Key]
	INNER JOIN dbo.ArticleSecurity s ON s.ArticleId = a.ArticleId
WHERE
	s.Scope = 'E'
	OR ( s.Scope = 'G' AND s.EntityId IN ( SELECT EntityId FROM @UserGroups ) )
	OR ( s.Scope = 'I' AND s.EntityId = @UserId )
GROUP BY
	a.UrlTitle
	,a.Title
	,a.UpdatedBy
	,a.UpdatedDtm
ORDER BY 
	SUM(tResults.Rank) DESC

GO
-- P  up_ArticleSecurity_Delete
CREATE PROCEDURE dbo.up_ArticleSecurity_Delete (
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
-- P  up_ArticleSecurity_Update
CREATE PROCEDURE dbo.up_ArticleSecurity_Update (
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
-- P  up_ArticleTag_Delete
CREATE PROCEDURE dbo.up_ArticleTag_Delete (
	@ArticleId int
	,@Tag nvarchar(30)
) AS

SET NOCOUNT ON

DELETE FROM [dbo].[ArticleTag]
WHERE
	ArticleId = @ArticleId
	AND Tag = @Tag

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
   ,[UpdatedDtm]
   ,[UpdatedBy]
   ,[Comment]
) VALUES (
	@Title
	,@UrlTitle
	,@Text
	,@Html
	,1  -- Revision
	,GETDATE()
	,@UpdatedBy
	,'Initial version'
)

SET @ArticleId = @@IDENTITY

GO
