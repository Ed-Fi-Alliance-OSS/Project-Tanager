# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

from proton_driver import client


proton_host = "localhost"
proton_port = 8463

kafka_host = "dms-kafka1"
kafka_port = 9092

c = client.Client(host=proton_host, port=proton_port)

for row in c.execute_iter("SELECT 'I am alive';"):
    print(row)

# print("Dropping existing stream")
# c.execute("DROP STREAM IF EXISTS document;")

print("Creating new stream")
c.execute(
    f"""
    CREATE EXTERNAL STREAM IF NOT EXISTS document(raw string)
    SETTINGS type='kafka',
         brokers='{kafka_host}:{kafka_port}',
         topic='edfi.dms.document',
         skip_ssl_cert_check='true';
    """
)

print(".")

print("List streams")
for row in c.execute_iter("SHOW STREAMS;"):
    print("+ ", row)

print(".")

print("Continuous polling for new data. Control-C to stop.")
print("New records will arrive here as requests are submitted to the API.")
rows = c.execute_iter(
    "SELECT * FROM document",
)
for row in rows:
    print(row)
