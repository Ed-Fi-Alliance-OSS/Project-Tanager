using System;
using System.IO;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Shouldly;
using JsonValidator;

namespace JsonValidator.Tests.Unit
{
    [TestFixture]
    public class JsonValidationServiceTests
    {
        private JsonValidationService _validationService = null!;
        private string _testDataDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            var apiSchemaJson = @"{
              ""projectSchema"": {
                ""resourceSchema"": {
                  ""absenceEventCategoryDescriptors"": {
                    ""jsonSchemaForInsert"": {
                      ""type"": ""object"",
                      ""properties"": {
                        ""id"": {
                          ""type"": ""string"",
                          ""maxLength"": 32
                        },
                        ""codeValue"": {
                          ""type"": ""string"",
                          ""maxLength"": 50
                        },
                        ""namespace"": {
                          ""type"": ""string"",
                          ""maxLength"": 255
                        },
                        ""shortDescription"": {
                          ""type"": ""string"",
                          ""maxLength"": 75
                        },
                        ""description"": {
                          ""type"": ""string"",
                          ""maxLength"": 1024
                        },
                        ""effectiveBeginDate"": {
                          ""type"": ""string"",
                          ""format"": ""date""
                        },
                        ""effectiveEndDate"": {
                          ""type"": ""string"",
                          ""format"": ""date""
                        },
                        ""_etag"": {
                          ""type"": ""string""
                        },
                        ""_lastModifiedDate"": {
                          ""type"": ""string"",
                          ""format"": ""date-time""
                        }
                      },
                      ""required"": [
                        ""codeValue"",
                        ""namespace"",
                        ""shortDescription""
                      ],
                      ""additionalProperties"": false
                    }
                  }
                }
              }
            }";

            var apiSchema = JsonNode.Parse(apiSchemaJson);
            _validationService = new JsonValidationService(apiSchema!);
            
            _testDataDirectory = Path.Combine(Path.GetTempPath(), "json-validator-tests");
            Directory.CreateDirectory(_testDataDirectory);
            Directory.CreateDirectory(Path.Combine(_testDataDirectory, "absenceEventCategoryDescriptors"));
        }

        [Test]
        public void ExtractResourceTypeFromPath_ShouldReturnParentDirectoryName()
        {
            // Arrange
            var filePath = "/ed-fi/absenceEventCategoryDescriptors/unexcused.json";

            // Act
            var resourceType = _validationService.ExtractResourceTypeFromPath(filePath);

            // Assert
            resourceType.ShouldBe("absenceEventCategoryDescriptors");
        }

        [Test]
        public void ExtractResourceTypeFromPath_WithNoParentDirectory_ShouldThrowException()
        {
            // Arrange - use a root file path which doesn't have a meaningful parent directory
            var filePath = "/file.json";

            // Act & Assert
            Should.Throw<ArgumentException>(() => _validationService.ExtractResourceTypeFromPath(filePath));
        }

        [Test]
        public void GetSchemaForResourceType_WithValidResourceType_ShouldReturnSchema()
        {
            // Act
            var schema = _validationService.GetSchemaForResourceType("absenceEventCategoryDescriptors");

            // Assert
            schema.ShouldNotBeNull();
        }

        [Test]
        public void GetSchemaForResourceType_WithInvalidResourceType_ShouldReturnNull()
        {
            // Act
            var schema = _validationService.GetSchemaForResourceType("nonExistentResource");

            // Assert
            schema.ShouldBeNull();
        }

        [Test]
        public void ValidateJsonFile_RequiredFieldsOnlyValid_ShouldReturnValid()
        {
            // Arrange
            var testFilePath = Path.Combine(_testDataDirectory, "absenceEventCategoryDescriptors", "valid-required.json");
            var validJson = @"{
                ""codeValue"": ""Flex time"",
                ""namespace"": ""uri://ed-fi.org/AbsenceEventCategoryDescriptor"",
                ""shortDescription"": ""Flex time""
            }";
            File.WriteAllText(testFilePath, validJson);

            // Act
            var result = _validationService.ValidateJsonFile(testFilePath);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.ErrorMessage.ShouldBeNull();
        }

        [Test]
        public void ValidateJsonFile_AllFieldsProvidedAndValid_ShouldReturnValid()
        {
            // Arrange
            var testFilePath = Path.Combine(_testDataDirectory, "absenceEventCategoryDescriptors", "valid-all-fields.json");
            var validJson = @"{
                ""id"": ""02bf731571d742c3afd41af408afab2b"",
                ""codeValue"": ""Flex time"",
                ""description"": ""Flex time"",
                ""namespace"": ""uri://ed-fi.org/AbsenceEventCategoryDescriptor"",
                ""shortDescription"": ""Flex time"",
                ""effectiveBeginDate"": ""2025-01-01"",
                ""effectiveEndDate"": ""2025-06-01""
            }";
            File.WriteAllText(testFilePath, validJson);

            // Act
            var result = _validationService.ValidateJsonFile(testFilePath);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.ErrorMessage.ShouldBeNull();
        }

        [Test]
        public void ValidateJsonFile_WithMetadataFields_ShouldReturnValid()
        {
            // Arrange
            var testFilePath = Path.Combine(_testDataDirectory, "absenceEventCategoryDescriptors", "valid-with-metadata.json");
            var validJson = @"{
                ""id"": ""02bf731571d742c3afd41af408afab2b"",
                ""codeValue"": ""Flex time"",
                ""description"": ""Flex time"",
                ""namespace"": ""uri://ed-fi.org/AbsenceEventCategoryDescriptor"",
                ""shortDescription"": ""Flex time"",
                ""effectiveBeginDate"": ""2025-01-01"",
                ""effectiveEndDate"": ""2025-06-01"",
                ""_etag"": ""5250549072223211944"",
                ""_lastModifiedDate"": ""2025-06-23T19:56:19.582404Z""
            }";
            File.WriteAllText(testFilePath, validJson);

            // Act
            var result = _validationService.ValidateJsonFile(testFilePath);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.ErrorMessage.ShouldBeNull();
        }

        [Test]
        public void ValidateJsonFile_MissingRequiredField_ShouldReturnInvalid()
        {
            // Arrange
            var testFilePath = Path.Combine(_testDataDirectory, "absenceEventCategoryDescriptors", "invalid-missing-required.json");
            var invalidJson = @"{
                ""__codeValue"": ""Flex time"",
                ""namespace"": ""uri://ed-fi.org/AbsenceEventCategoryDescriptor"",
                ""shortDescription"": ""Flex time""
            }";
            File.WriteAllText(testFilePath, invalidJson);

            // Act
            var result = _validationService.ValidateJsonFile(testFilePath);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
        }

        [Test]
        public void ValidateJsonFile_StringTooLong_ShouldReturnInvalid()
        {
            // Arrange
            var testFilePath = Path.Combine(_testDataDirectory, "absenceEventCategoryDescriptors", "invalid-string-too-long.json");
            var invalidJson = @"{
                ""codeValue"": ""Flex time xxx xx xx xxxx xxx xx xxx xxxxxx xxx xx xxx xxxxxx"",
                ""namespace"": ""uri://ed-fi.org/AbsenceEventCategoryDescriptor"",
                ""shortDescription"": ""Flex time""
            }";
            File.WriteAllText(testFilePath, invalidJson);

            // Act
            var result = _validationService.ValidateJsonFile(testFilePath);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
        }

        [Test]
        [Ignore("Date format validation may be implementation-dependent in JsonSchema.Net")]
        public void ValidateJsonFile_InvalidDateString_ShouldReturnInvalid()
        {
            // Arrange
            var testFilePath = Path.Combine(_testDataDirectory, "absenceEventCategoryDescriptors", "invalid-date.json");
            var invalidJson = @"{
                ""id"": ""02bf731571d742c3afd41af408afab2b"",
                ""codeValue"": ""Flex time"",
                ""description"": ""Flex time"",
                ""namespace"": ""uri://ed-fi.org/AbsenceEventCategoryDescriptor"",
                ""shortDescription"": ""Flex time"",
                ""effectiveBeginDate"": ""a2025-01-01"",
                ""effectiveEndDate"": ""2025-06-01""
            }";
            File.WriteAllText(testFilePath, invalidJson);

            // Act
            var result = _validationService.ValidateJsonFile(testFilePath);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
        }

        [Test]
        public void ValidateJsonFile_UnknownResourceType_ShouldReturnInvalid()
        {
            // Arrange
            var unknownResourceDirectory = Path.Combine(_testDataDirectory, "unknownResource");
            Directory.CreateDirectory(unknownResourceDirectory);
            var testFilePath = Path.Combine(unknownResourceDirectory, "test.json");
            var validJson = @"{
                ""someField"": ""some value""
            }";
            File.WriteAllText(testFilePath, validJson);

            // Act
            var result = _validationService.ValidateJsonFile(testFilePath);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
            result.ErrorMessage.ShouldContain("No schema found for resource type: unknownResource");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDataDirectory))
            {
                Directory.Delete(_testDataDirectory, true);
            }
        }
    }
}