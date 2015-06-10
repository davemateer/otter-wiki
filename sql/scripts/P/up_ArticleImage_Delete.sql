-- P  up_ArticleImage_Delete
CREATE PROCEDURE [dbo].[up_ArticleImage_Delete] (
	@ArticleImageId int
) AS

SET NOCOUNT ON

DELETE FROM dbo.ArticleImage
WHERE ArticleImageId = @ArticleImageId
