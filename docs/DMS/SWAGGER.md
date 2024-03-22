# DMS Feature: Generate Swagger/ Open API documentation

Generating Swagger or OpenAPI documentation for the Data Management Service (DMS) can be accomplished through two methods.

The first method involves implementing a translator capable of converting the ApiSchema.json file into a Swagger or OpenAPI specification. This translator can be developed using .NET technologies and leveraging the capabilities provided by the Microsoft.OpenApi library. The translator can be integrated directly into the DMS application or developed as a standalone tool.

The second method imply generating OpenAPI specification file(s) alongside the generation of the ApiSchema.json file within the META-ED application.

For the purpose of this document, we will explain the details of method one:

The Swagger or OpenAPI documentation translator will be developed using .NET 8 and the C# programming language. The necessary models for this task are available within the "Microsoft.OpenApi.Models" namespace.

This approach involves parsing and reading the contents of the ApiSchema.json file to extract the pertinent details required for generating OpenAPI models.

 OpenAPI models, such as "OpenApiPaths", "OpenApiComponents", "OpenApiOperation", "OpenApiPathItem", and "OpenApiResponses", serve as representations of different aspects of the API.

Once all the necessary instances of these models are generated, they are assembled into an "OpenApiDocument" object.

Subsequently, the resulting OpenAPI document object can be serialized into JSON format, adhering to the specific OpenAPI version, which in this case is 3.0.

Sample translator implementation can be found in this branch [DMS-38](https://github.com/Ed-Fi-Alliance-OSS/Data-Management-Service/tree/DMS-38)

To fully implement the tool for generating Swagger or OpenAPI documentation for the Data Management Service (DMS), several additional tasks need to be considered.

1. Refactor the sample Code:

    * Break down the sample code into multiple methods and classes to improve readability, maintainability, and reusability.
    * Consider organizing the code into logical components such as parsers, generators, serializers, etc., as needed.

2. Implement Paths and Operations for all HTTP actions:

    * Expand the functionality to cover all HTTP actions (GET, POST, PUT, DELETE, etc.).
    * Implement logic to generate OpenAPI paths and operations for each HTTP action, handling parameters, request, and responses accordingly.

3. Handle Ref Types, Descriptors, and Links:

    * Include logic to handle reference types, descriptors, and links specified in the ApiSchema.json file.
    * Ensure that referenced types and descriptors are properly resolved as schemas and included in the generated OpenAPI documentation.

4. Define appropriate responses:

    * Define and include appropriate responses for each HTTP action based on the expected behavior and outcomes of the API endpoints.
    * Ensure that responses include relevant content and status codes, reflecting the status of the request.

5. Add Security Schema Definition:

    * Define security schemas and requirements for accessing the API endpoints.


