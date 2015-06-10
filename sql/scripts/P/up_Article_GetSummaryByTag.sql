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
