# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

import json

from proton_driver import client


proton_host = "localhost"
proton_port = 8463

kafka_host = "dms-kafka1"
kafka_port = 9092

c = client.Client(host=proton_host, port=proton_port)

for row in c.execute_iter("SELECT 'I am alive';"):
    print(row)

print(".")

print("Create a materialized view")
c.execute(
    """
    CREATE MATERIALIZED VIEW IF NOT EXISTS schools
    AS SELECT raw:edfidoc FROM document
    WHERE raw:resourcename='School'
    AND raw:__deleted='false'
    """
)

print(".")

print("Continuous polling for new data. Control-C to stop.")
print("New records will arrive here as requests are submitted to the API.")
rows = c.execute_iter(
    "SELECT * FROM schools"
)
for row in rows:
    school = json.loads(row[0])

    for category in school["educationOrganizationCategories"]:
        if not category["educationOrganizationCategoryDescriptor"].endswith("#School"):
            print(f"❌ School {school['schoolId']} has invalid category {category['educationOrganizationCategoryDescriptor']}")
            break
