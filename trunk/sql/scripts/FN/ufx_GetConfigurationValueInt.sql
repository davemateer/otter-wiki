CREATE FUNCTION dbo.ufx_GetConfigurationValueInt (
	@ConfigKey varchar(50)
	,@Default int
) RETURNS int AS BEGIN

	DECLARE @Result int
	DECLARE @TempVariant sql_variant
	
	SELECT @TempVariant = ConfigValue
	FROM dbo.Configuration
	WHERE ConfigKey = @ConfigKey
	
	IF ( SQL_VARIANT_PROPERTY(@TempVariant, 'BaseType') IN ('int', 'smallint', 'tinyint', 'bit') ) BEGIN
	
		SET @Result = CAST(@TempVariant AS int)
		
	END ELSE IF ( SQL_VARIANT_PROPERTY(@TempVariant, 'BaseType') IN ('nvarchar', 'nchar', 'varchar', 'char') ) BEGIN
	
		DECLARE @TempString varchar(max)
		SET @TempString = CAST(@TempVariant AS varchar)
		
		IF ( ISNUMERIC(@TempString + '0e0') = 1 AND LEN(@TempString) <= 37 ) BEGIN
		
			DECLARE @TempDecimal decimal(38,0)
			SET @TempDecimal = CAST(@TempString as decimal(38,0))
			
			IF ( @TempDecimal BETWEEN -2147483648 AND 2147483647 ) BEGIN
				SET @Result = CAST(@TempDecimal as int)
			END
		
		END
	
	END

	RETURN ISNULL(@Result, @Default)

END