-- P  up_Article_Insert
CREATE PROCEDURE [dbo].[up_Article_Insert] (
	@ArticleId int OUTPUT
	,@Title nvarchar(100)
	,@UrlTitle nvarchar(100)
	,@Text nvarchar(max)
	,@Html nvarchar(max)
	,@UpdatedBy nvarchar(50)
) AS

SET NOCOUNT ON

INSERT INTO dbo.Article (
	[Title]
   ,[UrlTitle]
   ,[Text]
   ,[Html]
   ,[Revision]
   ,[CreatedBy]
   ,[CreatedWhen]
   ,[UpdatedBy]
   ,[UpdatedWhen]
   ,[Comment]
) VALUES (
	@Title
	,@UrlTitle
	,@Text
	,@Html
	,1  -- Revision
	,@UpdatedBy
	,GETDATE()
	,@UpdatedBy
	,GETDATE()
	,'Initial version'
)

SET @ArticleId = @@IDENTITY

