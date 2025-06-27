using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        
        // Load the API schema from the embedded resource in the Ed-Fi NuGet package
        public static JsonValidationService CreateFromEmbeddedResource()
        {
            try
            {
                // Load the EdFi.DataStandard52.ApiSchema assembly
                var assembly = Assembly.LoadFrom("EdFi.DataStandard52.ApiSchema.dll");
                
                // Get the embedded ApiSchema.json resource
                var resourceName = "EdFi.DataStandard52.ApiSchema.ApiSchema.json";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
                }
                
                using var reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();
                
                var apiSchema = JsonNode.Parse(jsonContent);
                if (apiSchema == null)
                {
                    throw new InvalidOperationException("Failed to parse API schema JSON from embedded resource");
                }
                
                return new JsonValidationService(apiSchema);
            }
            catch (Exception ex)
            {
                // Fall back to the local file if there's any issue with the embedded resource
                var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var apiSchemaPath = Path.Combine(assemblyDir!, "ApiSchema.json");
                
                if (!File.Exists(apiSchemaPath))
                {
                    // Try the source directory for development
                    apiSchemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiSchema.json");
                }
                
                if (!File.Exists(apiSchemaPath))
                {
                    throw new InvalidOperationException($"Could not load API schema from embedded resource ({ex.Message}) and fallback file not found at {apiSchemaPath}");
                }
                
                return CreateFromFile(apiSchemaPath);
            }
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
            try
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
            catch (Exception)
            {
                // If JsonPath evaluation fails (e.g., invalid selectors), return null
                // This will be handled as "no schema found" by the calling method
                return null;
            }
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
                        ErrorMessage = $"No schema found for resource type '{resourceType}'. Please verify the file path contains a valid Ed-Fi resource type directory."
                    };
                }
                
                var jsonContent = File.ReadAllText(jsonFilePath);
                var jsonNode = JsonNode.Parse(jsonContent);
                
                if (jsonNode == null)
                {
                    return new ValidationResult
                    {  
                        IsValid = false,
                        ErrorMessage = "Invalid JSON format - unable to parse the JSON file"
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
            
            // Check for common validation issues and provide more specific messages
            if (!validationResults.IsValid)
            {
                // Try to extract specific validation details from the results
                var details = validationResults.Details;
                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail.HasErrors)
                        {
                            foreach (var error in detail.Errors!)
                            {
                                errors.Add($"Field validation error: {error.Key} - {error.Value}");
                            }
                        }
                    }
                }
                
                if (errors.Count == 0)
                {
                    errors.Add("JSON validation failed - document does not conform to schema");
                }
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