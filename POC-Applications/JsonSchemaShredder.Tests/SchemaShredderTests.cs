// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using Shouldly;

namespace JsonSchemaShredder.Tests;

public partial class SchemaShredderTests
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
    result.ShouldContain("CREATE SCHEMA IF NOT EXISTS edfi;");
    result.ShouldContain("CREATE TABLE edfi.studentEducationOrganizationAssociation");
    result.ShouldContain("CREATE TABLE edfi.StudentEducationOrganizationAssociationAddress");
    result.ShouldContain("EducationOrganizationId INTEGER NOT NULL");
    result.ShouldContain("StudentUniqueId VARCHAR(32) NOT NULL");
    result.ShouldContain("BarrierToInternetAccessInResidenceDescriptor TEXT NULL");
    result.ShouldContain("AddressTypeDescriptor TEXT NOT NULL");
    result.ShouldContain("ApartmentRoomSuiteNumber VARCHAR(50) NULL");
    result.ShouldContain("StreetNumberName VARCHAR(150) NOT NULL");
    result.ShouldContain("CREATE INDEX nk_StudentEducationOrganizationAssociation");
    result.ShouldContain("CREATE INDEX nk_StudentEducationOrganizationAssociationAddress");
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
    result.ShouldNotContain("someDescriptors");
    result.ShouldContain("validResource");
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
    result.ShouldContain("stringField TEXT NOT NULL");
    result.ShouldContain("stringFieldWithLength VARCHAR(100) NULL");
    result.ShouldContain("integerField INTEGER NOT NULL");
    result.ShouldContain("booleanField BOOLEAN NULL");
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
    result.ShouldContain("CREATE TABLE");
    result.ShouldContain("clas");
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
    result.ShouldContain("birthDate DATE NOT NULL");
    result.ShouldContain("startTime TIME NOT NULL");
    result.ShouldContain("name TEXT NULL");
  }


  [Test]
  public void GeneratePostgreSqlScript_HandlesNumericFormats()
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
                                ""a"": {""type"": ""number""},
                                ""b"": {""type"": ""integer""},
                                ""c"": {""type"": ""number""}
                            },
                            ""required"": [""a"", ""b""]
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
    result.ShouldContain("a DECIMAL NOT NULL");
    result.ShouldContain("b INTEGER NOT NULL");
    result.ShouldContain("c DECIMAL NULL");
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
    var idColumnMatches = IdColumnRegex().Matches(result);
    idColumnMatches.Count.ShouldBe(1); // Only the BIGSERIAL primary key
    result.ShouldContain("Id BIGSERIAL NOT NULL PRIMARY KEY");
    result.ShouldNotContain("Id INTEGER"); // No INTEGER id columns should be added due to duplicate prevention
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
    result.ShouldContain("CREATE TABLE test.StudentEducationOrganizationAssociation");
    result.ShouldContain("CREATE TABLE test.StudentEducationOrganizationAssociationAddress");
    result.ShouldContain("CREATE TABLE test.StudentEducationOrganizationAssociationAddressPeriod");

    // Verify the nested array table has the correct columns
    result.ShouldContain("BeginDate DATE NOT NULL");
    result.ShouldContain("EndDate DATE NULL");
  }

  [System.Text.RegularExpressions.GeneratedRegex(@"Id \w+ ")]
  private static partial System.Text.RegularExpressions.Regex IdColumnRegex();
}
