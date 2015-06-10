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

