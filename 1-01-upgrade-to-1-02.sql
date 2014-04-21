--upgrade scripts from Nop.Plugin.Feed.GoogleShoppingAdvanced 1.01 to 1.02

--new locale resources
declare @resources xml
--a resource will be delete if its value is empty
set @resources='
<Language>
  <LocaleResource Name="Plugins.Feed.GoogleShoppingAdvanced.IsUseParentGroupedProductDescription">
    <Value>Use description of parent grouped product?</Value>
  </LocaleResource>
  <LocaleResource Name="Plugins.Feed.GoogleShoppingAdvanced.IsUseParentGroupedProductDescription.Hint">
    <Value>If simple products which belong to grouped products do not have their own description, choose to use the description of the parent grouped product instead.</Value>
  </LocaleResource>
    <LocaleResource Name="Plugins.Feed.GoogleShoppingAdvanced.MinProductDescriptionCharLimit">
    <Value>Minimum number of characters acceptable for product description</Value>
  </LocaleResource>
  <LocaleResource Name="Plugins.Feed.GoogleShoppingAdvanced.MinProductDescriptionCharLimit.Hint">
    <Value>If the product description is equal to or less than this figure, the parent grouped product description if available, will be used instead.</Value>
  </LocaleResource>
</Language>
'

CREATE TABLE #LocaleStringResourceTmp
	(
		[ResourceName] [nvarchar](200) NOT NULL,
		[ResourceValue] [nvarchar](max) NOT NULL
	)

INSERT INTO #LocaleStringResourceTmp (ResourceName, ResourceValue)
SELECT	nref.value('@Name', 'nvarchar(200)'), nref.value('Value[1]', 'nvarchar(MAX)')
FROM	@resources.nodes('//Language/LocaleResource') AS R(nref)

--do it for each existing language
DECLARE @ExistingLanguageID int
DECLARE cur_existinglanguage CURSOR FOR
SELECT [ID]
FROM [Language]
OPEN cur_existinglanguage
FETCH NEXT FROM cur_existinglanguage INTO @ExistingLanguageID
WHILE @@FETCH_STATUS = 0
BEGIN
	DECLARE @ResourceName nvarchar(200)
	DECLARE @ResourceValue nvarchar(MAX)
	DECLARE cur_localeresource CURSOR FOR
	SELECT ResourceName, ResourceValue
	FROM #LocaleStringResourceTmp
	OPEN cur_localeresource
	FETCH NEXT FROM cur_localeresource INTO @ResourceName, @ResourceValue
	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF (EXISTS (SELECT 1 FROM [LocaleStringResource] WHERE LanguageID=@ExistingLanguageID AND ResourceName=@ResourceName))
		BEGIN
			UPDATE [LocaleStringResource]
			SET [ResourceValue]=@ResourceValue
			WHERE LanguageID=@ExistingLanguageID AND ResourceName=@ResourceName
		END
		ELSE 
		BEGIN
			INSERT INTO [LocaleStringResource]
			(
				[LanguageId],
				[ResourceName],
				[ResourceValue]
			)
			VALUES
			(
				@ExistingLanguageID,
				@ResourceName,
				@ResourceValue
			)
		END
		
		IF (@ResourceValue is null or @ResourceValue = '')
		BEGIN
			DELETE [LocaleStringResource]
			WHERE LanguageID=@ExistingLanguageID AND ResourceName=@ResourceName
		END
		
		FETCH NEXT FROM cur_localeresource INTO @ResourceName, @ResourceValue
	END
	CLOSE cur_localeresource
	DEALLOCATE cur_localeresource


	--fetch next language identifier
	FETCH NEXT FROM cur_existinglanguage INTO @ExistingLanguageID
END
CLOSE cur_existinglanguage
DEALLOCATE cur_existinglanguage

DROP TABLE #LocaleStringResourceTmp
GO


--a new setting
IF NOT EXISTS (SELECT 1 FROM [Setting] WHERE [name] = N'feedgoogleshoppingadvancedsettings.isuseparentgroupedproductdescription')
BEGIN
	INSERT [Setting] ([Name], [Value], [StoreId])
	VALUES (N'feedgoogleshoppingadvancedsettings.isuseparentgroupedproductdescription', N'false', 0)
END
GO

--a new setting
IF NOT EXISTS (SELECT 1 FROM [Setting] WHERE [name] = N'feedgoogleshoppingadvancedsettings.minproductdescriptioncharlimit')
BEGIN
	INSERT [Setting] ([Name], [Value], [StoreId])
	VALUES (N'feedgoogleshoppingadvancedsettings.minproductdescriptioncharlimit', N'0', 0)
END
GO
