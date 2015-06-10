-- P  up_ArticleImage_Insert
CREATE PROCEDURE dbo.up_ArticleImage_Insert (
	@ArticleId int
	,@Filename varchar(100)
	,@Title nvarchar(100)
) AS

SET NOCOUNT ON

INSERT INTO dbo.ArticleImage ([ArticleId],[Filename],[Title])
VALUES (@ArticleId, @Filename, @Title)

