// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using Xunit;

namespace JsonSchemaShredder.Tests;

public class SchemaShredderTests
{
    [Fact]
    public void GeneratePostgreSqlScript_WithExampleSchema_CreatesCorrectTables()
    {
        // Arrange
        var jsonContent = @"{
            ""projectSchema"": {
                ""projectEndpointName"": ""ed-fi"",
                ""resourceSchemas"": {
                    ""studentEducationOrganizationAssociations"": {
                        ""identityJsonPaths"": [
                            ""$.educationOrganizationReference.educationOrganizationId"",
                            ""$.studentReference.studentUniqueId""
                        ],
                        ""jsonSchemaForInsert"": {
                            ""properties"": {
                                ""addresses"": {
                                    ""items"": {
                                        ""properties"": {
                                            ""addressTypeDescriptor"": {
                                                ""type"": ""string""
                                            },
                                            ""apartmentRoomSuiteNumber"": {
                                                ""maxLength"": 50,
                                                ""type"": ""string""
                                            },
                                            ""streetNumberName"": {
                                                ""maxLength"": 150,
                                                ""type"": ""string""
                                            }
                                        },
                                        ""required"": [
                                            ""streetNumberName"",
                                            ""addressTypeDescriptor""
                                        ],
                                        ""type"": ""object""
                                    },
                                    ""type"": ""array""
                                },
                                ""barrierToInternetAccessInResidenceDescriptor"": {
                                    ""type"": ""string""
                                },
                                ""educationOrganizationReference"": {
                                    ""properties"": {
                                        ""educationOrganizationId"": {
                                            ""type"": ""integer""
                                        }
                                    },
                                    ""required"": [
                                        ""educationOrganizationId""
                                    ],
                                    ""type"": ""object""
                                },
                                ""studentReference"": {
                                    ""properties"": {
                                        ""studentUniqueId"": {
                                            ""maxLength"": 32,
                                            ""type"": ""string""
                                        }
                                    },
                                    ""required"": [
                                        ""studentUniqueId""
                                    ],
                                    ""type"": ""object""
                                }
                            },
                            ""required"": [
                                ""studentReference"",
                                ""educationOrganizationReference""
                            ]
                        }
                    }
                }
            }
        }";

        var jsonDocument = JsonDocument.Parse(jsonContent);
        var shredder = new SchemaShredder();

        // Act
        var result = shredder.GeneratePostgreSqlScript(jsonDocument);

        // Assert
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS \"ed-fi\";", result);
        Assert.Contains("CREATE TABLE \"ed-fi\".\"studentEducationOrganizationAssociations\"", result);
        Assert.Contains("CREATE TABLE \"ed-fi\".\"studentEducationOrganizationAssociations_addresses\"", result);
        Assert.Contains("educationOrganizationReference_educationOrganizationId\" INTEGER NOT NULL", result);
        Assert.Contains("studentReference_studentUniqueId\" VARCHAR(32) NOT NULL", result);
        Assert.Contains("barrierToInternetAccessInResidenceDescriptor\" TEXT NULL", result);
        Assert.Contains("addressTypeDescriptor\" TEXT NOT NULL", result);
        Assert.Contains("apartmentRoomSuiteNumber\" VARCHAR(50) NULL", result);
        Assert.Contains("streetNumberName\" VARCHAR(150) NOT NULL", result);
        Assert.Contains("CREATE INDEX \"nk_studentEducationOrganizationAssociations\"", result);
        Assert.Contains("CREATE INDEX \"nk_studentEducationOrganizationAssociations_addresses\"", result);
    }

    [Fact]
    public void GeneratePostgreSqlScript_SkipsDescriptorResourceSchemas()
    {
        // Arrange
        var jsonContent = @"{
            ""projectSchema"": {
                ""projectEndpointName"": ""test"",
                ""resourceSchemas"": {
                    ""someDescriptors"": {
                        ""identityJsonPaths"": [""$.id""],
                        ""jsonSchemaForInsert"": {
                            ""properties"": {
                                ""name"": {""type"": ""string""}
                            }
                        }
                    },
                    ""validResource"": {
                        ""identityJsonPaths"": [""$.id""],
                        ""jsonSchemaForInsert"": {
                            ""properties"": {
                                ""name"": {""type"": ""string""}
                            },
                            ""required"": [""name""]
                        }
                    }
                }
            }
        }";

        var jsonDocument = JsonDocument.Parse(jsonContent);
        var shredder = new SchemaShredder();

        // Act
        var result = shredder.GeneratePostgreSqlScript(jsonDocument);

        // Assert
        Assert.DoesNotContain("someDescriptors", result);
        Assert.Contains("validResource", result);
    }

    [Fact]
    public void GeneratePostgreSqlScript_HandlesVariousDataTypes()
    {
        // Arrange
        var jsonContent = @"{
            ""projectSchema"": {
                ""projectEndpointName"": ""test"",
                ""resourceSchemas"": {
                    ""testResource"": {
                        ""identityJsonPaths"": [""$.id""],
                        ""jsonSchemaForInsert"": {
                            ""properties"": {
                                ""stringField"": {""type"": ""string""},
                                ""stringFieldWithLength"": {""type"": ""string"", ""maxLength"": 100},
                                ""integerField"": {""type"": ""integer""},
                                ""booleanField"": {""type"": ""boolean""}
                            },
                            ""required"": [""stringField"", ""integerField""]
                        }
                    }
                }
            }
        }";

        var jsonDocument = JsonDocument.Parse(jsonContent);
        var shredder = new SchemaShredder();

        // Act
        var result = shredder.GeneratePostgreSqlScript(jsonDocument);

        // Assert
        Assert.Contains("stringField\" TEXT NOT NULL", result);
        Assert.Contains("stringFieldWithLength\" VARCHAR(100) NULL", result);
        Assert.Contains("integerField\" INTEGER NOT NULL", result);
        Assert.Contains("booleanField\" BOOLEAN NULL", result);
    }
}