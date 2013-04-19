ALTER PROCEDURE dbo.up_Article_Search (
	@Query nvarchar(1000),
	@UserId nvarchar(50)
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
GROUP BY
	a.UrlTitle
	,a.Title
	,a.UpdatedBy
	,a.UpdatedDtm
ORDER BY 
	SUM(tResults.Rank) DESC