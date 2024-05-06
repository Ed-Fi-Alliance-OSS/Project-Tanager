USE EdFi_DataManagementService;
GO

CREATE FUNCTION dbo.GetPartitionKey (@guid UNIQUEIDENTIFIER)
RETURNS TINYINT
AS
BEGIN
   DECLARE @byteArray varbinary(16);
   DECLARE @partition_key tinyint;

   SET @byteArray = CONVERT(varbinary(16), @guid);

   SET @partition_key = CAST(SUBSTRING(@byteArray, 1, 1) AS int) & 15;

   RETURN @partition_key
END
