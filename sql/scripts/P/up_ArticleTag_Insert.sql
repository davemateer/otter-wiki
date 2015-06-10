-- P  up_ArticleTag_Insert
CREATE PROCEDURE dbo.up_ArticleTag_Insert (
	@ArticleId int,
	@Tag nvarchar(30)
) AS

SET NOCOUNT ON

INSERT INTO [dbo].[ArticleTag] ( [ArticleId], [Tag] )
VALUES ( @ArticleId, @Tag )


