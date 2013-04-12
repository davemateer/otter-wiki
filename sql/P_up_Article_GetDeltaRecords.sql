DECLARE @ArticleId int
DECLARE @Revision int

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
	
FROM
	dbo.Article a
	LEFT OUTER JOIN dbo.ArticleHistory ah ON
		ah.ArticleId = a.ArticleId
		AND ah.Revision >= @Revision
		AND (
			@ClosestSnapshotRevision IS NULL
			OR ( @ClosestSnapshotRevision IS NOT NULL AND ah.Revision <= @ClosestSnapshotRevision )
		)
