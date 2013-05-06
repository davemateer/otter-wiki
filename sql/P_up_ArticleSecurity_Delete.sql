USE [otter]
GO

/****** Object:  StoredProcedure [dbo].[up_ArticleSecurity_Delete]    Script Date: 05/06/2013 16:28:23 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[up_ArticleSecurity_Delete] (
	@ArticleId int
	,@Scope char(1)
	,@EntityId nvarchar(256)
) AS

SET NOCOUNT ON

DELETE FROM [dbo].[ArticleSecurity]
WHERE
	ArticleId = @ArticleId
	AND Scope = @Scope
	AND EntityId = @EntityId

GO


