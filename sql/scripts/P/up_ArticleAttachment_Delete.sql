CREATE PROCEDURE [dbo].[up_ArticleAttachment_Delete] (
	@ArticleAttachmentId int
) AS

SET NOCOUNT ON

DELETE FROM dbo.ArticleAttachment
WHERE ArticleAttachmentId = @ArticleAttachmentId
