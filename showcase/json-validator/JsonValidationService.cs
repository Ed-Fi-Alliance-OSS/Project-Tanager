using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using Json.Schema;

namespace JsonValidator
{
    public class JsonValidationService
    {
        private readonly JsonNode _apiSchema;
        
        public JsonValidationService(JsonNode apiSchema)
        {
            _apiSchema = apiSchema;
        }
        
        public static JsonValidationService CreateFromFile(string apiSchemaPath)
        {
            var jsonContent = File.ReadAllText(apiSchemaPath);
            var apiSchema = JsonNode.Parse(jsonContent);
            if (apiSchema == null)
            {
                throw new InvalidOperationException("Failed to parse API schema JSON");
            }
            return new JsonValidationService(apiSchema);
        }
        
        // This method would normally load from the embedded resource in the NuGet package
        public static JsonValidationService CreateFromEmbeddedResource()
        {
            // For now, fall back to the local file since we can't access the NuGet package
            var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var apiSchemaPath = Path.Combine(assemblyDir!, "ApiSchema.json");
            
            if (!File.Exists(apiSchemaPath))
            {
                // Try the source directory for development
                apiSchemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiSchema.json");
            }
            
            return CreateFromFile(apiSchemaPath);
        }
        
        public string ExtractResourceTypeFromPath(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var parentDirectory = fileInfo.Directory?.Name;
            
            // Check if the parent directory is meaningful for an Ed-Fi resource type
            if (string.IsNullOrEmpty(parentDirectory))
            {
                throw new ArgumentException($"Cannot extract resource type from path: {filePath}");
            }
            
            // Check for root directory or other system directories that aren't resource types
            var parentFullPath = fileInfo.Directory?.FullName;
            if (parentFullPath == "/" || parentFullPath == "\\" || 
                parentDirectory.Length <= 2) // Likely drive letters or very short names
            {
                throw new ArgumentException($"Cannot extract resource type from path: {filePath}");
            }
            
            return parentDirectory;
        }
        
        public JsonSchema? GetSchemaForResourceType(string resourceType)
        {
            var jsonPath = $"$.projectSchema.resourceSchema.{resourceType}.jsonSchemaForInsert";
            var path = JsonPath.Parse(jsonPath);
            
            var matches = path.Evaluate(_apiSchema);
            if (matches.Matches.Count == 0)
            {
                return null;
            }
            
            var schemaNode = matches.Matches[0].Value;
            if (schemaNode == null)
            {
                return null;
            }
            
            var schemaJson = schemaNode.ToJsonString();
            
            return JsonSchema.FromText(schemaJson);
        }
        
        public ValidationResult ValidateJsonFile(string jsonFilePath)
        {
            try
            {
                var resourceType = ExtractResourceTypeFromPath(jsonFilePath);
                var schema = GetSchemaForResourceType(resourceType);
                
                if (schema == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"No schema found for resource type: {resourceType}"
                    };
                }
                
                var jsonContent = File.ReadAllText(jsonFilePath);
                var jsonNode = JsonNode.Parse(jsonContent);
                
                if (jsonNode == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid JSON format"
                    };
                }
                
                var validationResults = schema.Evaluate(jsonNode);
                
                if (validationResults.IsValid)
                {
                    return new ValidationResult { IsValid = true };
                }
                
                var errorMessage = GetValidationErrorMessage(validationResults);
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = errorMessage
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Validation failed: {ex.Message}"
                };
            }
        }
        
        private static string GetValidationErrorMessage(EvaluationResults validationResults)
        {
            var errors = new List<string>();
            
            if (validationResults.HasErrors)
            {
                foreach (var error in validationResults.Errors!)
                {
                    var message = $"Validation error at {error.Key}: {error.Value}";
                    errors.Add(message);
                }
            }
            
            if (!validationResults.IsValid && errors.Count == 0)
            {
                errors.Add("JSON validation failed");
            }
            
            return string.Join("; ", errors);
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}