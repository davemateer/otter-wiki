CREATE PROCEDURE [dbo].[up_Article_Insert] (
	@Title nvarchar(100)
	,@UrlFriendlyTitle nvarchar(100)
	,@Text nvarchar(max)
	,@TextHash binary(16)
	,@LastUpdatedBy nvarchar(50)
) AS

SET NOCOUNT ON

INSERT INTO dbo.Article (
	[Title]
   ,[UrlFriendlyTitle]
   ,[Text]
   ,[TextHash]
   ,[Revision]
   ,[LastUpdatedDtm]
   ,[LastUpdatedBy]
) VALUES (
	@Title
	,@UrlFriendlyTitle
	,@Text
	,@TextHash
	,1  -- Revision
	,GETDATE()
	,@LastUpdatedBy
)

GO


