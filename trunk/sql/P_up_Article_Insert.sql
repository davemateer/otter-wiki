CREATE PROCEDURE [dbo].[up_Article_Insert] (
	@Title nvarchar(100)
	,@UrlTitle nvarchar(100)
	,@Text nvarchar(max)
	,@TextHash binary(16)
	,@Html nvarchar(max)
	,@LastUpdatedBy nvarchar(50)
) AS

SET NOCOUNT ON

INSERT INTO dbo.Article (
	[Title]
   ,[UrlTitle]
   ,[Text]
   ,[TextHash]
   ,[Html]
   ,[Revision]
   ,[LastUpdatedDtm]
   ,[LastUpdatedBy]
) VALUES (
	@Title
	,@UrlTitle
	,@Text
	,@TextHash
	,@Html
	,1  -- Revision
	,GETDATE()
	,@LastUpdatedBy
)
GO