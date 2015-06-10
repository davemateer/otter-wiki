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

