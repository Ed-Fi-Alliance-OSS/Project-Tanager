# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Install the connector Jar file into the sink connector service
Invoke-Restmethod https://github.com/Aiven-Open/cloud-storage-connectors-for-apache-kafka/releases/download/v3.2.0/s3-sink-connector-for-apache-kafka-3.2.0.tar -outfile s3-sink-connector-for-apache-kafka-3.2.0.tar
docker cp s3-sink-connector-for-apache-kafka-3.2.0.tar kafka-opensearch-sink:/tmp
docker exec -it kafka-opensearch-sink bash -c "tar -xvf /tmp/s3-sink-connector-for-apache-kafka-3.2.0.tar -C /kafka/connect/s3-sink-connector"
docker restart kafka-opensearch-sink

# Install the connector configuration
$sinkUrl = "http://localhost:8084/connectors"
Invoke-RestMethod -Method Post -uri $sinkUrl -ContentType "application/json" -Body $(Get-Content connector.json)

# If you need to delete the connector, run the line below.
# Invoke-RestMethod -Method Delete $sinkUrl/dms_s3_sink
