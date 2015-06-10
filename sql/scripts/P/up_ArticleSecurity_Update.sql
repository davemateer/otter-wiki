-- P  up_ArticleSecurity_Update
CREATE PROCEDURE dbo.up_ArticleSecurity_Update (
	@ArticleId int
	,@Scope char(1)
	,@EntityId nvarchar(256)
	,@Permission char(1)
) AS

SET NOCOUNT ON

MERGE dbo.ArticleSecurity s
USING ( 
    VALUES (@ArticleId, @Scope, @EntityId)
) AS update_values (ArticleId, Scope, EntityId) 
ON
	s.ArticleId = update_values.ArticleId
	AND s.Scope = update_values.Scope
	AND s.EntityId = update_values.EntityId
WHEN MATCHED THEN
   UPDATE SET s.Permission = @Permission
WHEN NOT MATCHED THEN
   INSERT (ArticleId, Scope, EntityId, Permission) VALUES (@ArticleId, @Scope, @EntityId, @Permission)
;

