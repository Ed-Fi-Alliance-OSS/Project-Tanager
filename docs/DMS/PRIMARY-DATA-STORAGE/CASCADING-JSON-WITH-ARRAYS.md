# Cascading Updates into JSON Documents: Handling Arrays

## Problem

In a cascading update we must update not only the references between tables, but also the JSON documents.
Documents can have collections of references, expressed as arrays. A cascade will likely affect only one
element of a reference collection, and that element will need to be targeted for update.

However, the current datastore associates references from one document to another as a unordered list of
referentialIds. There is no relationship between referentialIds and reference collections that would allow for
direct array element targeting for an update. Support for individual element updates can be performed in one
of two ways:

- Extensive datastore modeling changes to associate a referentialId with a reference collection array index.
- Finding the reference element in the array using the old identity values themselves.

One challenge with a element finding method is that there are mismatches in ODS/API naming between the name of
an identity element in a document and the corresponding name of the element in a reference to that document.
One name cannot always be derived from the other. Because of this, the DMS ApiSchema provides a mapping
between these names.

Take the reference collection from `ReportCard` to `StudentCompetencyObjective` as an example. Here is a
trimmed portion of the DMS ApiSchema for that reference:

```json
  "StudentCompetencyObjective": {
      "referenceJsonPaths": [
        {
          "identityJsonPath": "$.gradingPeriodReference.schoolId",
          "referenceJsonPath": "$.studentCompetencyObjectives[*].studentCompetencyObjectiveReference.gradingPeriodSchoolId"
        },
        {
          "identityJsonPath": "$.objectiveCompetencyObjectiveReference.educationOrganizationId",
          "referenceJsonPath": "$.studentCompetencyObjectives[*].studentCompetencyObjectiveReference.objectiveEducationOrganizationId"
        }
      ]
    }
```

The above shows two naming mismatches captured by the DMS ApiSchema. The `schoolId` field in a
`StudentCompetencyObjective` document is referenced by `ReportCard` as `gradingPeriodSchoolId`, and the
`educationOrganizationId` is referenced as `objectiveEducationOrganizationId`. An operation to find a
reference element in an array using the old identity values would require this mapping information.

## Solutions Declined

Various solution approaches were considered, starting with a continuation of the existing SQL solution for
scalar references with regular names. This would have required either a large modeling change to add
referentialId-to-array-index metadata, or a way to look up elements by the old identity value. Lookups would
require backend access to the ApiSchema object to resolve naming issues. PostgreSQL does not have a good
mechanism to both look up an array position by search criteria and modify it. This would likely require the
addition of a GIN index to the document, slowing insertion performance. Additionally, some benchmarks show
that partial document update can be slower than full JSON document replacement.

A modeling change to add referentialId-to-array-index metadata was considered, but rejected both for the
complexity required and the additional storage necessary. It would require something along the lines of a
table to store the JSONPath of every referentialId, and has the usual partitioning and indexing challenges.

## Solution

The proposed solution is to move document reference cascading to the C# side, allowing for easier use of the
DMS ApiSchema object and providing an opportunity to pull some reference cascading behavior into core to share
between backends. This leaves the existing data model unchanged and requires no new indexing for performance.

One design goal should be for a backend to delegate the actual document modification back to Core, possibly
via an object given by Core to the backend. The backend would fetch a document from the datastore that needs a
reference update, Core would make the modification based on JsonPath information from DMS ApiSchema, and then
the backend would update the datastore with the modified document. This would prevent backends from having to
directly use the DMS ApiSchema.

## Algorithm

### Backend

Once determined this is an identity update operation, in the single transaction scope:

1. Fetch the original document from datastore, to get the old identity information.

1. Update the document and referentialIds in the datastore (as done today for an identity update).

1. Select all referencing documents from datastore by referentialId.

   1. Submit to the Core library the following:
      * Original document
      * Original document projectName and resourceName
      * Updated document (changed identity)
      * Current referencing document
      * Current referencing document projectName and resourceName

   1. Core library returns the updated referencing document, and whether this update is an identity change
      that will trigger a new cascade.

   1. If this is not an identity change, update the referencing document.

	 1. If this is an identity change, add this to the list of updates to cascade. Remember original and update
	 versions, and projectName and resourceName.

1. If there is an additional updates in the list, go back to step 2 with it.

### Core Library

The core library takes the old original document, the new original document, and the referencing document.
Using the DMS ApiSchema, it returns an updated referencing document and whether this update is itself an
identity update.

   1. Get the `identityJsonPaths` from ApiSchema for the original document.
   1. Extract the identity values from the original and the updated document.
   1. Get the `documentPathsMapping` from ApiSchema for the referring document.
   1. Get the `referenceJsonPath` JsonPaths in the referring document.
   1. Determine whether any of those JsonPaths are also in the referring document's `identityJsonPaths`. If
      so, this update is itself an identity update.
   1. Construct a JsonPath query using those JsonPaths `AND`ed together along with the original identity
      values.
   1. Using the query, update the old identity values with the new identity values.
   1. Return the updated document, and whether this was an identity update.

### ApiSchema Structure Examples

For reference in writing the Core Library, here are ApiSchema snippets for an original document and a
referencing document:

#### Original Document (ClassPeriod)
```json
  "identityJsonPaths": [
	"$.classPeriodName",
	"$.schoolReference.schoolId"
  ]
```

#### Referencing Document
```json
  "documentPathsMapping": {
	"ClassPeriod": {
	  "isDescriptor": false,
	  "isReference": true,
	  "projectName": "Ed-Fi",
	  "referenceJsonPaths": [
		{
		  "identityJsonPath": "$.classPeriodName",
		  "referenceJsonPath": "$.classPeriods[*].classPeriodReference.classPeriodName"
		},
		{
		  "identityJsonPath": "$.schoolReference.schoolId",
		  "referenceJsonPath": "$.classPeriods[*].classPeriodReference.schoolId"
		}
	  ],
	  "resourceName": "ClassPeriod"
	},
  }
```
