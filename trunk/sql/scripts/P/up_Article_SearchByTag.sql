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