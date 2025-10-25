// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;

namespace JsonSchemaShredder.Tests;

public class SchemaShredderTests
{
  [Test]
  public void GeneratePostgreSqlScript_WithExampleSchema_CreatesCorrectTables()
  {
    // Arrange
    var jsonContent =
      @"{
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
    Assert.That(result, Does.Contain("CREATE SCHEMA IF NOT EXISTS edfi;"));
    Assert.That(result, Does.Contain("CREATE TABLE edfi.studentEducationOrganizationAssociations"));
    Assert.That(
      result,
      Does.Contain("CREATE TABLE edfi.StudentEducationOrganizationAssociation_Address")
    );
    Assert.That(result, Does.Contain("educationOrganizationId INTEGER NOT NULL"));
    Assert.That(result, Does.Contain("studentUniqueId VARCHAR(32) NOT NULL"));
    Assert.That(result, Does.Contain("barrierToInternetAccessInResidenceDescriptor TEXT NULL"));
    Assert.That(result, Does.Contain("addressTypeDescriptor TEXT NOT NULL"));
    Assert.That(result, Does.Contain("apartmentRoomSuiteNumber VARCHAR(50) NULL"));
    Assert.That(result, Does.Contain("streetNumberName VARCHAR(150) NOT NULL"));
    Assert.That(result, Does.Contain("CREATE INDEX nk_studentEducationOrganizationAssociations"));
    Assert.That(
      result,
      Does.Contain("CREATE INDEX nk_StudentEducationOrganizationAssociation_Address")
    );
  }

  [Test]
  public void GeneratePostgreSqlScript_SkipsDescriptorResourceSchemas()
  {
    // Arrange
    var jsonContent =
      @"{
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
    Assert.That(result, Does.Not.Contain("someDescriptors"));
    Assert.That(result, Does.Contain("validResource"));
  }

  [Test]
  public void GeneratePostgreSqlScript_HandlesVariousDataTypes()
  {
    // Arrange
    var jsonContent =
      @"{
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
    Assert.That(result, Does.Contain("stringField TEXT NOT NULL"));
    Assert.That(result, Does.Contain("stringFieldWithLength VARCHAR(100) NULL"));
    Assert.That(result, Does.Contain("integerField INTEGER NOT NULL"));
    Assert.That(result, Does.Contain("booleanField BOOLEAN NULL"));
  }

  [Test]
  public void GeneratePostgreSqlScript_HandlesEdgeCases()
  {
    // Arrange
    var jsonContent =
      @"{
            ""projectSchema"": {
                ""projectEndpointName"": ""test"",
                ""resourceSchemas"": {
                    ""class"": {
                        ""identityJsonPaths"": [""$"", ""$.a""],
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

    // Act & Assert - Should not throw exceptions with edge case inputs
    var result = shredder.GeneratePostgreSqlScript(jsonDocument);
    Assert.That(result, Does.Contain("CREATE TABLE"));
    Assert.That(result, Does.Contain("clas"));
  }

  [Test]
  public void GeneratePostgreSqlScript_HandlesDateAndTimeFormats()
  {
    // Arrange
    var jsonContent =
      @"{
            ""projectSchema"": {
                ""projectEndpointName"": ""test"",
                ""resourceSchemas"": {
                    ""testResource"": {
                        ""identityJsonPaths"": [""$.id""],
                        ""jsonSchemaForInsert"": {
                            ""properties"": {
                                ""birthDate"": {""type"": ""string"", ""format"": ""date""},
                                ""startTime"": {""type"": ""string"", ""format"": ""time""},
                                ""name"": {""type"": ""string""}
                            },
                            ""required"": [""birthDate"", ""startTime""]
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
    Assert.That(result, Does.Contain("birthDate DATE NOT NULL"));
    Assert.That(result, Does.Contain("startTime TIME NOT NULL"));
    Assert.That(result, Does.Contain("name TEXT NULL"));
  }

  [Test]
  public void GeneratePostgreSqlScript_HandlesDuplicateColumnNames()
  {
    // Arrange
    var jsonContent =
      @"{
            ""projectSchema"": {
                ""projectEndpointName"": ""test"",
                ""resourceSchemas"": {
                    ""testResource"": {
                        ""identityJsonPaths"": [""$.id""],
                        ""jsonSchemaForInsert"": {
                            ""properties"": {
                                ""id"": {""type"": ""integer""},
                                ""personRef1"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""id"": {""type"": ""integer""}
                                    },
                                    ""required"": [""id""]
                                },
                                ""personRef2"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""id"": {""type"": ""integer""}
                                    },
                                    ""required"": [""id""]
                                }
                            },
                            ""required"": [""id""]
                        }
                    }
                }
            }
        }";

    var jsonDocument = JsonDocument.Parse(jsonContent);
    var shredder = new SchemaShredder();

    // Act
    var result = shredder.GeneratePostgreSqlScript(jsonDocument);

    // Assert - Should only have the primary key id column due to duplicate prevention
    var idColumnMatches = System.Text.RegularExpressions.Regex.Matches(result, @"id \w+ ");
    Assert.That(idColumnMatches.Count, Is.EqualTo(1)); // Only the BIGSERIAL primary key
    Assert.That(result, Does.Contain("id BIGSERIAL NOT NULL PRIMARY KEY"));
    Assert.That(result, Does.Not.Contain("id INTEGER")); // No INTEGER id columns should be added due to duplicate prevention
  }

  [Test]
  public void GeneratePostgreSqlScript_HandlesNestedArrays()
  {
    // Arrange
    var jsonContent =
      @"{
            ""projectSchema"": {
                ""projectEndpointName"": ""test"",
                ""resourceSchemas"": {
                    ""studentEducationOrganizationAssociations"": {
                        ""identityJsonPaths"": [""$.studentId""],
                        ""jsonSchemaForInsert"": {
                            ""properties"": {
                                ""studentId"": {""type"": ""integer""},
                                ""addresses"": {
                                    ""type"": ""array"",
                                    ""items"": {
                                        ""type"": ""object"",
                                        ""properties"": {
                                            ""addressTypeDescriptor"": {""type"": ""string""},
                                            ""periods"": {
                                                ""type"": ""array"",
                                                ""items"": {
                                                    ""type"": ""object"",
                                                    ""properties"": {
                                                        ""beginDate"": {""type"": ""string"", ""format"": ""date""},
                                                        ""endDate"": {""type"": ""string"", ""format"": ""date""}
                                                    },
                                                    ""required"": [""beginDate""]
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            ""required"": [""studentId""]
                        }
                    }
                }
            }
        }";

    var jsonDocument = JsonDocument.Parse(jsonContent);
    var shredder = new SchemaShredder();

    // Act
    var result = shredder.GeneratePostgreSqlScript(jsonDocument);

    // Assert - Should create three tables: main, addresses, and periods
    Assert.That(result, Does.Contain("CREATE TABLE test.studentEducationOrganizationAssociations"));
    Assert.That(
      result,
      Does.Contain("CREATE TABLE test.StudentEducationOrganizationAssociation_Address")
    );
    Assert.That(
      result,
      Does.Contain("CREATE TABLE test.StudentEducationOrganizationAssociation_Addres_Period")
    );

    // Verify the nested array table has the correct columns
    Assert.That(result, Does.Contain("beginDate DATE NOT NULL"));
    Assert.That(result, Does.Contain("endDate DATE NULL"));

    // Verify foreign key relationships
    Assert.That(result, Does.Contain("StudentEducationOrganizationAssociation_studentId"));
  }
}
