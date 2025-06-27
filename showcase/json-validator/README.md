# JSON File Validation using the ApiSchema File

## User Story

I want to validate a JSON file based on JSON Schema definition provided by an
ApiSchema file for the Ed-Fi Data Standard.

### Approach

* ApiSchema file is embedded in NuGet package `EdFi.DataStandard52.ApiSchema`:
  * Install latest version from feed URL `https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_packaging/EdFi/nuget/v3/index.json`
  * Open the assembly, get the resource called `EdFi.DataStandard52.ApiSchema.ApiSchema.json`
  * Parse that as JSON
* The JSON file's parent directory is the resource type. For example, the file path might be `/ed-fi/absenceEventCategoryDescriptors/unexcused.json`. Then the resource type is `absenceEventCategoryDescriptors`
* In the parsed ApiSchema data, find a JSON Schema definition at a path like `$.projectSchema.resourceSchema.{resourceType}.jsonSchemaForInsert`
* Use that JSON Schema to validate the JSON file
* Exit with code 0 if valid
* If not valid, print the validation error and then exit with code 1

### Architectural Requirements

* Create a C# 8.0 application in folder `./showcase/json-validator`
* Save this prompt as file `./showcase/README.md`
* Write unit tests using NUnit and Shouldly
* Use package `JsonSchema.Net` for JSON schema validation
* Use `JsonPath.Net` for traversing JSON paths
* Use built-in System.Text.Json for all other JSON parsing and manipulation, not Newtonsoft

## Test Cases

### Happy Path

#### Required Fields Only, Valid

```json
{
    "codeValue": "Flex time",
    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
    "shortDescription": "Flex time"
}
```

#### All Fields Provided and Valid, No Extra Fields

```json
{
    "id": "02bf731571d742c3afd41af408afab2b",
    "codeValue": "Flex time",
    "description": "Flex time",
    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
    "shortDescription": "Flex time",
    "effectiveBeginDate": "2025-01-01",
    "effectiveEndDate": "2025-06-01"
}
```

#### All Fields Provided and Valid, Includes Metadata Fields

```json
{
    "id": "02bf731571d742c3afd41af408afab2b",
    "codeValue": "Flex time",
    "description": "Flex time",
    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
    "shortDescription": "Flex time",
    "effectiveBeginDate": "2025-01-01",
    "effectiveEndDate": "2025-06-01",
    "_etag": "5250549072223211944",
    "_lastModifiedDate": "2025-06-23T19:56:19.582404Z"
}
```

### Negative Path

#### Missing a required field

```json
{
    "__codeValue": "Flex time",
    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
    "shortDescription": "Flex time"
}
```

#### A string is too long

```json
{
    "codeValue": "Flex time xxx xx xx xxxx xxx xx xxx xxxxxx xxx xx xxx xxxxxx",
    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
    "shortDescription": "Flex time"
}
```

#### Contains an invalid date string

```json
{
    "id": "02bf731571d742c3afd41af408afab2b",
    "codeValue": "Flex time",
    "description": "Flex time",
    "namespace": "uri://ed-fi.org/AbsenceEventCategoryDescriptor",
    "shortDescription": "Flex time",
    "effectiveBeginDate": "a2025-01-01",
    "effectiveEndDate": "2025-06-01"
}
```