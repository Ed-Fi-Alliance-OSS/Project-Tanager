[CmdletBinding()]
param (
    # Stop services instead of starting them
    [Switch]
    $d,

    # Delete volumes after stopping services
    [Switch]
    $v
)
#Requires -Version 7
$ErrorActionPreference = "Stop"

# Total number of records
$totalRecords = 1000500
# Set the PostgreSQL password
$env:PGPASSWORD = "abcdefgh1!"
$sqlserverPassword = "abcdefgh1!"

if ($d) {
    if ($v) {
        Write-Output "Shutting down services and deleting volumes"
        docker-compose -p dms-test-db down -v
    } else {
        Write-Output "Shutting down services"
        docker-compose -p dms-test-db down
    }
    return
}

# Start Docker containers
docker-compose -p dms-test-db up -d

# Wait for containers to be fully up and running
Start-Sleep 30

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Create database
Write-Output "SQL Server database and table creation..."
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S "localhost" -U sa -P $sqlserverPassword -N -C -Q @"
  CREATE DATABASE TestDB;
"@

# Create the table
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S "localhost" -U sa -P $sqlserverPassword -N -C -Q @"
  USE TestDB;
  CREATE TABLE Records (
    ID INT PRIMARY KEY,
    Data NVARCHAR(100)
    );
"@

# Insert records into SQL Server
Write-Output "Inserting $totalRecords records into SQL Server..."
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S "localhost" -U sa -P $sqlserverPassword -N -C -Q "
  BEGIN TRANSACTION;
  DECLARE @i INT = 1;
  WHILE @i <= $totalRecords
  BEGIN
    INSERT INTO TestDB.dbo.Records (ID, Data) VALUES (@i, REPLICATE('A', 100));
    SET @i = @i + 1;
  END
  COMMIT;
" 1>$null

$stopwatch.Stop()

Write-Output "SQL Server data insertion complete in $($stopwatch.Elapsed.TotalSeconds) seconds."

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
# Create the database
Write-Output "PostgreSQL database and table creation..."
docker exec -it postgres psql -U postgres -c "CREATE DATABASE testdb;"

# Connect to the database and create the table
docker exec -it postgres psql -U postgres -d testdb -c @"
  CREATE TABLE records (
    id SERIAL PRIMARY KEY,
    data VARCHAR(100)
    );
"@


# Insert records into PostgreSQL
Write-Output "Inserting $totalRecords records into PostgreSQL..."
$query = @"
DO `$`$
DECLARE
i INT := 1;
BEGIN
  WHILE i <= $totalRecords LOOP
    INSERT INTO records (data) VALUES (REPEAT('A', 100));
    i := i + 1;
  END LOOP;
END `$`$;
"@

docker exec -it postgres psql -U postgres -d testdb -c $query >$null

$stopwatch.Stop()
Write-Output "PostgreSQL data insertion complete in $($stopwatch.Elapsed.TotalSeconds) seconds."

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
# Create index for OpenSearch and allow large result window
Write-Output "OpenSearch index creation..."
$indexUrl = "http://localhost:9200/testdb"
$indexBody = @"
{
  `"settings`": {
  `"index`": {
    `"max_result_window`": 1000500
    }
  },
  `"mappings`": {
    `"properties`": {
      `"id`": { `"type`": `"integer`" },
      `"data`": { `"type`": `"text`" }
      }
    }
  }
"@

Invoke-RestMethod -Uri $indexUrl -Method Put -ContentType "application/json" -Body $indexBody | Out-Null

# Insert records into OpenSearch
# Function to insert records in batches
function Insert-Batch  {
  param (
    [int]$start,
    [int]$end
  )
  $recordUrl = "http://localhost:9200/testdb/_bulk"
  $recordBody = ""
  for ($i = $start; $i -le $end; $i++){
    $recordBody += @"
    { "index": { "_id": $i } }
    { "id": $i, "data": "$([System.String]::new('A' * 100))" }
"@ + "`n"
  }
  Invoke-RestMethod -Uri $recordUrl -Method Post -ContentType "application/json" -Body $recordBody
}

# Number of records per batch
$batchSize = 10000
# Calculate number of batches
$numBatches = [math]::Ceiling($totalRecords / $batchSize)

Write-Output "Inserting $totalRecords records into OpenSearch..."

# Insert in batches
for ($batch = 0; $batch -lt $numBatches; $batch++) {
  $start = ($batch * $batchSize) + 1
  $end = [math]::Min(($start + $batchSize - 1), $totalRecords)
  Write-Output "Inserting records $start to $end into OpenSearch"
  Insert-Batch -start $start -end $end | Out-Null
}

$stopwatch.Stop()

Write-Output "OpenSearch data insertion complete in $($stopwatch.Elapsed.TotalSeconds) seconds."




