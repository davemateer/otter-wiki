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

