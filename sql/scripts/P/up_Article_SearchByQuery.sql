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

