ALTER PROCEDURE [dbo].[up_Article_Update] (
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
			WHEN a.Revision % 50 = 0 THEN a.[Text]
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
