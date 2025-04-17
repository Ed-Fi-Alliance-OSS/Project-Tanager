# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

import aiohttp
import asyncio
import base64
import time
import argparse

# Parse command-line arguments
parser = argparse.ArgumentParser(description="ODS API Performance Script")
parser.add_argument(
    "--student_count",
    type=int,
    default=10,
    help="Number of students to create. Default is 10.",
)
parser.add_argument(
    "--api_port",
    type=int,
    default=8001,
    help="Port number for the API. Default is 8001.",
)
parser.add_argument(
    "--client_id",
    type=str,
    default="minimalKey",
    help="Client ID for authentication. Default is 'minimalKey'.",
)
parser.add_argument(
    "--client_secret",
    type=str,
    default="minimalSecret",
    help="Client Secret for authentication. Default is 'minimalSecret'.",
)
parser.add_argument(
    "--system", type=str, choices=["ods", "dms"], help="System being tested."
)
args = parser.parse_args()

# Parameters from command-line arguments
student_count = args.student_count
api_port = args.api_port
client_id = args.client_id
client_secret = args.client_secret
system = args.system

# Base URLs
base_url = f"http://localhost:{api_port}"


async def invoke_request(session, uri, method, headers=None, body=None):
    async with session.request(method, uri, json=body, headers=headers) as response:
        if response.status >= 400:
            print(f"Request to {uri} failed with status {response.status}")
            print(await response.text())
            raise aiohttp.ClientResponseError(
                request_info=response.request_info,
                history=response.history,
                status=response.status,
            )

        # ODS/API is not returning a `Content-Type` header. aihttp infers
        # `application/octet-stream`, and we need to disable JSON content
        # type checking in the `json()` function call.
        if response.content_type.startswith(
            "application/json"
        ) or response.content_type.startswith("application/octet-stream"):
            return (
                await response.json(content_type=None),
                (
                    response.headers["location"]
                    if "location" in response.headers
                    else None
                ),
            )
        elif response.content_type:
            return (await response.text(),)
        else:
            return None


async def main():
    async with aiohttp.ClientSession() as session:
        # Step 1: Read the Token URL from the Discovery API
        print("Read the Token URL from the Discovery API")
        async with session.get(base_url) as discovery_response:
            discovery_data = await discovery_response.json()
            token_url = discovery_data["urls"]["oauth"]
            data_api = discovery_data["urls"]["dataManagementApi"].rstrip("/")

        # Step 2: Create a token
        print("Create a token")
        credentials = f"{client_id}:{client_secret}"
        encoded_credentials = base64.b64encode(credentials.encode("utf-8")).decode(
            "utf-8"
        )
        async with session.post(
            token_url,
            data={"grant_type": "client_credentials"},
            headers={"Authorization": f"Basic {encoded_credentials}"},
        ) as token_response:
            token_data = await token_response.json()
            token = token_data["access_token"]

        headers = {"Authorization": f"Bearer {token}"}

        # Step 3: Create students and their associations
        student_urls = []
        timings = {}
        start_time = time.time()

        print(f"Creating {student_count} students and their student school associations")
        for i in range(0, student_count):
            student_unique_id = f"473{i}"

            student_response = await invoke_request(
                session,
                f"{data_api}/ed-fi/students",
                "POST",
                headers=headers,
                body={
                    "studentUniqueId": student_unique_id,
                    "firstName": f"Student{i}",
                    "lastSurname": f"LastName{i}",
                    "birthDate": "2012-01-01",
                },
            )
            student_urls.append(student_response[1])

            await invoke_request(
                session,
                f"{data_api}/ed-fi/studentSchoolAssociations",
                "POST",
                headers=headers,
                body={
                    "studentReference": {"studentUniqueId": student_unique_id},
                    "schoolReference": {"schoolId": 1},
                    "entryDate": "2024-08-01",
                    "entryGradeLevelDescriptor": "uri://ed-fi.org/GradeLevelDescriptor#Ninth Grade",
                },
            )

        elapsed_time = time.time() - start_time
        timings["student_creation"] = elapsed_time
        print(
            f"Created {student_count} students and their associations in {elapsed_time:.2f} seconds"
        )

        # Step 4: Retrieve individual student records by ID
        print("Retrieve individual student records by ID")
        start_time = time.time()
        for url in student_urls:
            url = url if url.startswith("http") else f"{data_api}/ed-fi/students/{url}"
            # print(f"Retrieve student {url}")

            await invoke_request(
                session,
                url,
                "GET",
                headers=headers,
            )
        elapsed_time = time.time() - start_time
        timings["student_retrieval"] = elapsed_time
        print(
            f"Retrieved individual student records by ID in {elapsed_time:.2f} seconds"
        )

        # Step 5: Retrieve individual student records by query
        print("Retrieve individual student records by query")
        start_time = time.time()
        for i in range(1, student_count + 1):
            student_unique_id = f"473{i}"
            # print(f"Retrieve student {i}")
            await invoke_request(
                session,
                f"{data_api}/ed-fi/students?studentUniqueId={student_unique_id}",
                "GET",
                headers=headers,
            )
        elapsed_time = time.time() - start_time
        timings["student_query_retrieval"] = elapsed_time
        print(
            f"Retrieved individual student records by query in {elapsed_time:.2f} seconds"
        )

        # Step 6: Retrieve all students in batches
        print("Retrieve all students in batches")
        start_time = time.time()
        offset = 0
        while True:
            # print(f"Retrieve students with offset {offset}")
            students = await invoke_request(
                session,
                f"{data_api}/ed-fi/students?limit=500&offset={offset}",
                "GET",
                headers=headers,
            )
            if not students or len(students[0]) == 0:
                break
            offset += 500
        elapsed_time = time.time() - start_time
        timings["all_students_retrieval"] = elapsed_time
        print(f"Retrieved all students in {elapsed_time:.2f} seconds")

        # Convert timings to csv and print to screen
        print(
            f"{system},{student_count},"
            f'{timings["student_creation"]},'
            f'{timings["student_retrieval"]},'
            f'{timings["student_query_retrieval"]},'
            f'{timings["all_students_retrieval"]}'
        )


# Run the script
asyncio.run(main())
