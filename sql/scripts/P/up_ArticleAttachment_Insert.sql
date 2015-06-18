CREATE PROCEDURE [dbo].[up_ArticleAttachment_Insert] (
	@ArticleId int
	,@Filename varchar(100)
	,@Title nvarchar(100)
) AS

SET NOCOUNT ON

INSERT INTO dbo.ArticleAttachment ([ArticleId],[Filename],[Title])
VALUES (@ArticleId, @Filename, @Title)
