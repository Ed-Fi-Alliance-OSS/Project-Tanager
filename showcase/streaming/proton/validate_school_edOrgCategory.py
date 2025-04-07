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

print("Creating external stream if it does not exist yet.")
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

print("Create a materialized view if it does not exist yet.")
c.execute(
    """
    CREATE MATERIALIZED VIEW IF NOT EXISTS schools
    AS SELECT raw:edfidoc FROM document
    WHERE raw:resourcename='School'
    AND raw:__deleted='false'
    """
)


def validate_schools(view):
    for row in c.execute_iter(f"SELECT * FROM {view}"):
        school = json.loads(row[0])

        for category in school["educationOrganizationCategories"]:
            if not category["educationOrganizationCategoryDescriptor"].endswith(
                "#School"
            ):
                print(
                    f"❌ School {school['schoolId']} has invalid category {category['educationOrganizationCategoryDescriptor']}"
                )


print(".")
print("Select all schools that were previously created.")
# Note that this uses `table(schools)`, which is more of a snapshot query
validate_schools("table(schools)")

print(".")
print("Continuous polling for new data. Control-C to stop.")
print("New records will arrive here as requests are submitted to the API.")
# Note that this does not use the `table()` function, thus it is a streaming query`
validate_schools("schools")
