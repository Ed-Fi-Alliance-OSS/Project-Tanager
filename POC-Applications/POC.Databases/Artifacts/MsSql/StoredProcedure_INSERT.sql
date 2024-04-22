USE [EdFi_DataManagementService]
GO

/****** Object:  StoredProcedure [dbo].[InsertDMSRecords]    Script Date: 4/19/2024 4:16:29 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[InsertDMSRecords]
    @resource_name varchar(256),
    @recordsCount int,
	  @isSubClass bit,
    @addReferences bit,
    @referenceName varchar(256)
AS
BEGIN
  DECLARE @counter INT
	SET @counter = 1

	DECLARE @document_uuid uniqueidentifier 

	DECLARE @edfi_doc varbinary(max)
	SET @edfi_doc =  CONVERT(varbinary,'0x7B227363686F6F6C5265666572656E6365223A7B227363686F6F6C4964223A3235353930313030312C226C696E6B223A7B2272656C223A225363686F6F6C222C2268726566223A222F65642D66692F7363686F6F6C732F3230656331396535303730323435313238613330666463633639323562623039227D7D2C2273747564656E745265666572656E6365223A7B2273747564656E74556E697175654964223A22363034383232222C226C696E6B223A7B2272656C223A2253747564656E74222C2268726566223A222F65642D66692F73747564656E74732F3766343237616632336564383439363139303764636364336437366235386435227D7D2C22656E74727944617465223A22323032312D30382D3233222C22656E74727947726164654C6576656C44657363726970746F72223A227572693A2F2F65642D66692E6F72672F47726164654C6576656C44657363726970746F72234E696E7468206772616465222C22616C7465726E617469766547726164756174696F6E506C616E73223A5B5D2C22656475636174696F6E506C616E73223A5B5D2C225F65746167223A2235323530313634373033303536393138333437222C225F6C6173744D6F64696669656444617465223A22323032342D30342D30345432333A30313A30322E393533303434335A227D')
	DECLARE @doc_partition_key tinyint;

	DECLARE @insertedDocumentId BIGINT
	DECLARE @byteArray varbinary(16);

	DECLARE @InsertedDocID TABLE (ID BIGINT);

	--Aliases
	DECLARE @referential_id uniqueidentifier
	DECLARE @alias_partition_key tinyint;

  DECLARE @superClass_referential_id uniqueidentifier
  DECLARE @superClass_referential_id_partitionkey tinyint;

  DECLARE @InsertedAliasID TABLE (ID BIGINT);
  DECLARE @insertedAlId BIGINT


  --References
  IF(@addReferences = 1)
	  BEGIN
		  DECLARE school_references CURSOR FOR
		  SELECT TOP 10 partition_key, referential_id FROM dbo.Aliases WHERE document_id IN
		  (SELECT id FROM dbo.Documents WHERE resource_name like @referenceName)
	  END

  DECLARE @reference_Id BIGINT;
  DECLARE @reference_partitionkey tinyint;

  WHILE @counter <= @recordsCount
	BEGIN
	  SELECT NEWID()
		SET @document_uuid = NEWID()

	  SET @doc_partition_key = dbo.GetPartitionKey(@document_uuid)

	  -- Insert record into Documents table
	  INSERT INTO dbo.[Documents] (partition_key, document_uuid, resource_name, edfi_doc) 
	  output INSERTED.ID INTO @InsertedDocID VALUES (@doc_partition_key, @document_uuid, @resource_name, @edfi_doc)

	  SET @insertedDocumentId = (SELECT ID FROM @InsertedDocID);

	  	-- Delete the id 
	   DELETE FROM @InsertedDocID;

	  -- Insert record into Aliases table
	  SET @referential_id = NEWID()
	  SET @alias_partition_key =  dbo.GetPartitionKey(@referential_id)

	  INSERT INTO dbo.[Aliases] (partition_key, referential_id, document_id, document_partition_key)
	  output INSERTED.ID INTO @InsertedAliasID VALUES (@alias_partition_key, @referential_id, @insertedDocumentId, @doc_partition_key)

    SET @insertedAlId = (SELECT ID FROM @InsertedAliasID);

	-- Delete the id 
	DELETE FROM @InsertedAliasID;

	  IF(@isSubClass = 1)
	  BEGIN
	      SET @superClass_referential_id = NEWID()
	      SET @superClass_referential_id_partitionkey =  dbo.GetPartitionKey(@referential_id)

		  INSERT INTO dbo.[Aliases] (partition_key, referential_id, document_id, document_partition_key)
		  output INSERTED.ID VALUES (@superClass_referential_id_partitionkey, @superClass_referential_id, @insertedDocumentId, @doc_partition_key)
	  END

    -- Insert record into References table
    IF(@addReferences = 1)
    BEGIN

     OPEN school_references  
     FETCH NEXT FROM school_references INTO @reference_partitionkey, @reference_Id

     WHILE @@FETCH_STATUS = 0  
     BEGIN
	   INSERT INTO dbo.[References] (partition_key, parent_alias_id, parent_partition_key,
     referenced_alias_id, referenced_partition_key)
     VALUES (@alias_partition_key, @insertedAlId, @alias_partition_key, @reference_Id, @reference_partitionkey)
		
	   FETCH NEXT FROM studentSectionAssociations_cursor INTO @reference_partitionkey, @reference_Id 
     END 

      CLOSE studentSectionAssociations_cursor  
      DEALLOCATE studentSectionAssociations_cursor
    END

	  SET @counter = @counter + 1
	END
END

GO


