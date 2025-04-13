# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

import base64
import requests
import random
import time

# Parameters
student_count = 5
dms_port = 8080
config_port = 8081
sys_admin_id = "DmsConfigurationService"
sys_admin_secret = "s3creT@09"

# Base URLsj
config_base_url = f"http://localhost:{config_port}"
dms_base_url = f"http://localhost:{dms_port}"


# Helper function to make requests
def invoke_request(uri, method, headers=None, body=None):
    try:
        if method == "POST":
            response = requests.post(uri, json=body, headers=headers)
        elif method == "GET":
            response = requests.get(uri, headers=headers)
        else:
            raise ValueError(f"Unsupported HTTP method: {method}")

        response.raise_for_status()

        # Check if the response has a JSON content type
        if response.headers.get("Content-Type", "").startswith("application/json"):
            return response.json() if response and response.text else None
        elif response.text.strip():
            return response.text if response else None
        else:
            return None
    except requests.exceptions.RequestException as e:
        print(f"Response: {response.text if response else 'No response'}")
        print(f"Request to {uri} failed: {e}")
        raise


# Step 1: Create a Config Service token
print("Create a Config Service token")
token_response = requests.post(
    f"{config_base_url}/connect/token",
    data={
        "client_id": sys_admin_id,
        "client_secret": sys_admin_secret,
        "grant_type": "client_credentials",
        "scope": "edfi_admin_api/full_access",
    },
    headers={"Content-Type": "application/x-www-form-urlencoded"},
)
token_response.raise_for_status()
config_token = token_response.json()["access_token"]

# Step 2: Create a new vendor
print("Create a new vendor")
vendor_response = invoke_request(
    f"{config_base_url}/v2/vendors",
    "POST",
    headers={"Authorization": f"Bearer {config_token}"},
    body={
        "company": f"Demo Vendor {random.randint(0, 9999999)}",
        "contactName": "George Washington",
        "contactEmailAddress": "george@example.com",
        "namespacePrefixes": "uri://ed-fi.org",
    },
)
vendor_id = vendor_response["id"]

# Step 3: Create a new application
print("Create a new application")
application_response = invoke_request(
    f"{config_base_url}/v2/applications",
    "POST",
    headers={"Authorization": f"Bearer {config_token}"},
    body={
        "vendorId": vendor_id,
        "applicationName": "Demo application",
        "claimSetName": "E2E-NoFurtherAuthRequiredClaimSet",
    },
)
client_key = application_response["key"]
client_secret = application_response["secret"]

# Step 4: Read the Token URL from the Discovery API
print("Read the Token URL from the Discovery API")
discovery_response = invoke_request(f"{dms_base_url}", "GET")
token_url = discovery_response["urls"]["oauth"]
data_api = discovery_response["urls"]["dataManagementApi"]

# Step 5: Create a DMS token
print("Create a DMS token")
credentials = f"{client_key}:{client_secret}"
encoded_credentials = base64.b64encode(credentials.encode("utf-8")).decode("utf-8")

dms_token_response = requests.post(
    token_url,
    data={"grant_type": "client_credentials"},
    headers={"Authorization": f"Basic {encoded_credentials}"},
)
dms_token_response.raise_for_status()
dms_token = dms_token_response.json()["access_token"]

# Step 6: Create a school year
print("Create a school year")
invoke_request(
    f"{data_api}/ed-fi/schoolYearTypes",
    "POST",
    headers={"Authorization": f"Bearer {dms_token}"},
    body={
        "schoolYear": 2024,
        "beginDate": "2024-08-01",
        "endDate": "2025-05-31",
        "schoolYearDescription": "2024-2025",
        "currentSchoolYear": True,
    },
)

# Step 7: Create grade level descriptors
print("Grade level descriptors")
invoke_request(
    f"{data_api}/ed-fi/gradeLevelDescriptors",
    "POST",
    headers={"Authorization": f"Bearer {dms_token}"},
    body={
        "namespace": "uri://ed-fi.org/GradeLevelDescriptor",
        "codeValue": "Ninth grade",
        "shortDescription": "9th Grade",
    },
)

# Step 8: Create education organization category descriptors
print("Education organization category descriptors")
invoke_request(
    f"{data_api}/ed-fi/educationOrganizationCategoryDescriptors",
    "POST",
    headers={"Authorization": f"Bearer {dms_token}"},
    body={
        "namespace": "uri://ed-fi.org/EducationOrganizationCategoryDescriptor",
        "codeValue": "School",
        "shortDescription": "School",
    },
)

# Step 9: Create a school
print("Create a school")
invoke_request(
    f"{data_api}/ed-fi/schools",
    "POST",
    headers={"Authorization": f"Bearer {dms_token}"},
    body={
        "schoolId": 1,
        "nameOfInstitution": "Grand Bend High School",
        "shortNameOfInstitution": "GBMS",
        "webSite": "http://www.GBISD.edu/GBMS/",
        "educationOrganizationCategories": [
            {
                "educationOrganizationCategoryDescriptor": "uri://ed-fi.org/EducationOrganizationCategoryDescriptor#School",
            }
        ],
        "gradeLevels": [
            {
                "gradeLevelDescriptor": "uri://ed-fi.org/GradeLevelDescriptor#Ninth grade",
            }
        ],
    },
)

# Step 10: Create students and their education organization associations
print("Creating students and their education organization associations")
start_time = time.time()
for i in range(1, student_count + 1):
    student_unique_id = f"12345678{i}"

    # Create student
    # print(f"Create student {i}")
    invoke_request(
        f"{data_api}/ed-fi/students",
        "POST",
        headers={"Authorization": f"Bearer {dms_token}"},
        body={
            "studentUniqueId": student_unique_id,
            "firstName": f"Student{i}",
            "lastSurname": f"LastName{i}",
            "birthDate": "2012-01-01",
        },
    )

    # Create student education organization association
    # print(f"Create student education organization association for student {i}")
    invoke_request(
        f"{data_api}/ed-fi/studentEducationOrganizationAssociations",
        "POST",
        headers={"Authorization": f"Bearer {dms_token}"},
        body={
            "studentReference": {"studentUniqueId": student_unique_id},
            "educationOrganizationReference": {"educationOrganizationId": 1},
            "beginDate": "2024-08-01",
            "endDate": "2025-05-31",
        },
    )

end_time = time.time()
print(
    f"Created {student_count} students and their education organization "
    + f"associations in {end_time - start_time:.2f} seconds"
)
