# Profiles

## Requirements

> [!TIP]
> This requirements section is lifted straight from the ODS/API documentation.

### Feature Description

An API Profile enables the creation of a data policy for a particular set of
API Resources, generally in support of a specific usage scenario (such as for
Nutrition or Special Education specialty applications).

The policy is expressed as a set of rules for explicit inclusion or exclusion
of properties, references, collections, and/or collection items (based on Type
or Ed-Fi Descriptor values) at all levels of a Resource.

The API evaluates each request to determine if the resource requested by the
API consumer is covered by a single assigned Profile, and if so, it will
implicitly process the request using that Profile. However, if multiple
Profiles are assigned, or the API consumer is simply choosing to use a
particular Profile, the API consumer must specify which Profile is to be used
by adding the appropriate HTTP header to the request (i.e. Accept for GET
requests, and Content-Type for PUT/POST requests).

### Include-Only vs. Exclude-Only Strategies

Platform implementers can choose to configure access by inclusion or exclusion,
but it's useful to understand the implications of using IncludeOnly vs.
ExcludeOnly member selection modes when defining Profiles.

If the IncludeOnly value is used exclusively, a very rigid Profile definition
will result. As new elements are added to the data model (either through an
implementer extending the data model or when upgrading to a new version of the
Ed-Fi Data Standard), none of these added data elements will be included.
However, if the Profile is defined using ExcludeOnly then these other elements
will be automatically included, resulting in a more flexible definition that
will not necessarily require adjustments over time.

### Profile Definition

> [!NOTE]
> This section describes how the ODS/API operates. The DMS might not use the
> same mechanism, but it does need to support the same concepts.

The Profile Definition is expressed in XML in terms of the Resource model's
members (not to be confused with the JSON representation).

A Profile Definition can consist of multiple Resources (e.g., School and
Student):

```xml
<!-- Multiple resources -->
<Profile name="Test-Profile-Student-and-School-Include-All">
    <Resource name="School">
        <ReadContentType memberSelection="IncludeAll" />
        <WriteContentType memberSelection="IncludeAll" />
    </Resource>
    <Resource name="Student">
        <ReadContentType memberSelection="IncludeAll" />
        <WriteContentType memberSelection="IncludeAll" />
    </Resource>
</Profile>
```

Resources can be readable or writable only:

```xml
<!-- Readable Only Profile-->
<Profile name="Test-Profile-Resource-ReadOnly">
    <Resource name="School">
        <ReadContentType memberSelection="IncludeAll" />
    </Resource>
</Profile>

<!-- Writable Only Profile-->
<Profile name="Test-Profile-Resource-WriteOnly">
    <Resource name="School">
        <WriteContentType memberSelection="IncludeAll" />
    </Resource>
</Profile>
```

Resource members can be explicitly included based on the member selection:

```xml
<!-- Resource-level IncludeOnly -->
<Profile name="Test-Profile-Resource-IncludeOnly">
    <Resource name="School">
        <ReadContentType memberSelection="IncludeOnly">
            <Property name="NameOfInstitution" />                               <!-- Inherited property -->
            <Property name="OperationalStatusDescriptor" />                     <!-- Inherited Type property -->
            <Property name="CharterApprovalSchoolYearTypeReference" />          <!-- Property -->
            <Property name="SchoolType" />                                      <!-- Type property -->
            <Property name="AdministrativeFundingControlDescriptor" />          <!-- Descriptor property -->
            <Collection name="EducationOrganizationAddresses" memberSelection="IncludeAll"/> <!-- Inherited Collection -->
            <Collection name="SchoolCategories" memberSelection="IncludeAll" /> <!-- Collection -->
        </ReadContentType>
        <WriteContentType memberSelection="IncludeOnly">
            <Property name="ShortNameOfInstitution" />                          <!-- Inherited property -->
            <Property name="OperationalStatusDescriptor" />                     <!-- Inherited Type property -->
            <Property name="WebSite" />                                         <!-- Property -->
            <Property name="CharterStatusType" />                               <!-- Type property -->
            <Property name="AdministrativeFundingControlDescriptor" />          <!-- Descriptor property -->
            <Collection name="EducationOrganizationInternationalAddresses" memberSelection="IncludeAll" /> <!-- Inherited Collection -->
            <Collection name="SchoolGradeLevels" memberSelection="IncludeAll" /> <!-- Collection -->
        </WriteContentType>
    </Resource>
</Profile>
```

Resource members can be explicitly excluded based on the member selection:

```xml
<Profile name="Test-Profile-Resource-ExcludeOnly">
    <Resource name="School">
        <ReadContentType memberSelection="ExcludeOnly">
            <Property name="NameOfInstitution" />                               <!-- Inherited property -->
            <Property name="OperationalStatusDescriptor" />                     <!-- Inherited Type property -->
            <Property name="CharterApprovalSchoolYearTypeReference" />          <!-- Property -->
            <Property name="SchoolType" />                                      <!-- Type property -->
            <Property name="AdministrativeFundingControlDescriptor" />          <!-- Descriptor property -->
            <Collection name="EducationOrganizationAddresses" memberSelection="IncludeAll" /> <!-- Inherited Collection -->
            <Collection name="SchoolCategories" memberSelection="IncludeAll" /> <!-- Collection -->
        </ReadContentType>
        <WriteContentType memberSelection="ExcludeOnly">
            <Property name="ShortNameOfInstitution" />                          <!-- Inherited property -->
            <Property name="OperationalStatusDescriptor" />                     <!-- Inherited Type property -->
            <Property name="WebSite" />                                         <!-- Property -->
            <Property name="CharterStatusType" />                               <!-- Type property -->
            <Property name="AdministrativeFundingControlDescriptor" />          <!-- Descriptor property -->
            <Collection name="EducationOrganizationInternationalAddresses" memberSelection="IncludeAll" /> <!-- Inherited Collection -->
            <Collection name="SchoolGradeLevels" memberSelection="IncludeAll" /> <!-- Collection -->
        </WriteContentType>
    </Resource>
</Profile>
```

The same inclusion/exclusion rules apply to child collections (e.g., the
School's addresses):

```xml
<!-- Child collection IncludeOnly/ExcludeOnly profiles -->
<Profile name="Test-Profile-Resource-BaseClass-Child-Collection-IncludeOnly">
    <Resource name="School">
        <ReadContentType memberSelection="IncludeOnly">
            <Collection name="EducationOrganizationAddresses" memberSelection="IncludeOnly">
                <Property name="City" />
                <Property name="StateAbbreviationDescriptor" />
                <Property name="PostalCode" />
            </Collection>
        </ReadContentType>
        <WriteContentType memberSelection="IncludeOnly">
            <Collection name="EducationOrganizationAddresses" memberSelection="IncludeOnly">
                <Property name="Latitude" />
                <Property name="Longitude" />
            </Collection>
        </WriteContentType>
    </Resource>
</Profile>
```

The data policy can contain filters on child collection items (e.g., only include Physical and Shipping addresses):

```xml
<!-- Child collection filtering on types and descriptors -->
<Profile name="Test-Profile-Resource-Child-Collection-Filtered-To-IncludeOnly-Specific-Types-and-Descriptors">
    <Resource name="School">
        <ReadContentType memberSelection="IncludeOnly">
            <Collection name="EducationOrganizationAddresses" memberSelection="IncludeOnly">
                <Property name="StreetNumberName" />
                <Property name="City" />
                <Property name="StateAbbreviationDescriptor" />
                <Filter propertyName="AddressTypeDescriptor" filterMode="IncludeOnly">
                    <Value>Physical</Value>
                    <Value>Shipping</Value>
                </Filter>
            </Collection>
        </ReadContentType>
    </Resource>
</Profile>
```

In the example above, GET requests will only return Physical and Shipping
addresses. If also applied to the WriteContentType, the caller will receive an
error response if they attempt to write anything other than Physical or Shipping
addresses.

## DMS Design

### Filtering Properties on Documents

> [!WARNING]
> First pass brainstorming ahead.

The Data Management Service uses JSON Schema definitions to validate the
incoming JSON payloads before storing them. A custom mechanism has been added to
allow "overposting" - inclusion of properties that are not part of the Ed-Fi
Data Model - by stripping out the extraneous properties before storing the
document. Perhaps this same system could be leveraged to support API Profiles.

JSON Schema has a [composition] concept that allows joining together multiple
sub-schemas. The profile(s) definitions could be expressed simply in XML, JSON,
or YML, and then interpolated into a JSON schema definition. For the more strict
_Include Only_ mode, use the `allOf` operator ("Must be valid against all of the
subschemas") to incorporate the properties that should be available. For the
_Exclude Only_ mode, use the `not` operator () to define the properties must be
excluded ("Must not be valid against the given schema"). Then leverage the
overposting functionality to remove any properties that should not be either
writable or readable by the API client.

### Accessing Profile Assignments

TBD

> [!NOTE]
> Will depend on the [client authorization](../AUTH.md) and the
> [Configuration Service](../CS/) in some way. Presumably claims in JWT.

## Implementation

TBD

> [!NOTE]
>
> * Separate read and write middleware?
> * Load dynamic profiles from disk or database? If disk, simple to mount in
>    Docker. But what about deployments in Azure App Services, for example?
> * Use of JSON schema itself for the profiles might be overdoing it, since the
>   profile definition does not need to describe type information. Maybe adopt a
>   simpler standard and then interpret as JSON schema? In building the
>   sub-schema for profiles, each element may need to be set to allow all of the
>   data types, if we wish to avoid looking up the actual data type.
