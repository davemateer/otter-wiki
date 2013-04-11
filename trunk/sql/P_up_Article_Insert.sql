ALTER PROCEDURE [dbo].[up_Article_Insert] (
	@Title nvarchar(100)
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
   ,[UpdatedDtm]
   ,[UpdatedBy]
   ,[Comment]
) VALUES (
	@Title
	,@UrlTitle
	,@Text
	,@Html
	,1  -- Revision
	,GETDATE()
	,@UpdatedBy
	,'Initial version'
)
GO