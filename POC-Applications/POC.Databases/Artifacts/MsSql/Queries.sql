-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

select $PARTITION.partition_function_Documents(partition_key) as partition_number, count(*) as records_count
from [EdFi_DataManagementService].[dbo].[Documents]
group by $PARTITION.partition_function_Documents(partition_key)
order by $PARTITION.partition_function_Documents(partition_key);
