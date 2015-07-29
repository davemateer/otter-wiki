CREATE PROCEDURE dbo.up_Article_View (
	@ArticleId int
	,@ViewedBy nvarchar(50)
) AS

SET NOCOUNT ON

UPDATE Article 
SET LastViewedBy = @ViewedBy, LastViewedWhen = GETDATE()
WHERE ArticleId = @ArticleId