-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- Query to find out the partition details for a specific table
select $PARTITION.partition_function_Documents(partition_key) as partition_number, count(*) as records_count
from [EdFi_DataManagementService].[dbo].[Documents]
group by $PARTITION.partition_function_Documents(partition_key)
order by $PARTITION.partition_function_Documents(partition_key);

-- Query for getting table and index sizes
DECLARE @tmpTableSizes TABLE
(
    tableName    VARCHAR(100),
    numberofRows VARCHAR(100),
    reservedSize VARCHAR(50),
    dataSize     VARCHAR(50),
    indexSize    VARCHAR(50),
    unusedSize   VARCHAR(50)
)

INSERT @tmpTableSizes 
    EXEC sp_MSforeachtable @command1="EXEC sp_spaceused '?'"

SELECT
    tableName,
    CAST(numberofRows AS INT)                              'numberOfRows',
    CAST(LEFT(reservedSize, LEN(reservedSize) - 3) AS INT) 'reservedSize KB',
    CAST(LEFT(dataSize, LEN(dataSize) - 3) AS INT)         'dataSize KB',
    CAST(LEFT(indexSize, LEN(indexSize) - 3) AS INT)       'indexSize KB',
    CAST(LEFT(unusedSize, LEN(unusedSize) - 3) AS INT)     'unusedSize KB'
    FROM
        @tmpTableSizes
    ORDER BY
        [reservedSize KB] DESC
