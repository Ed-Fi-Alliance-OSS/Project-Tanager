using System;
using System.IO;
using JsonValidator;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: JsonValidator <path-to-json-file>");
            Console.WriteLine("Example: JsonValidator /ed-fi/absenceEventCategoryDescriptors/unexcused.json");
            Environment.Exit(1);
        }

        var jsonFilePath = args[0];

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"Error: File not found: {jsonFilePath}");
            Environment.Exit(1);
        }

        try
        {
            var validationService = JsonValidationService.CreateFromEmbeddedResource();
            var result = validationService.ValidateJsonFile(jsonFilePath);
            
            if (result.IsValid)
            {
                Console.WriteLine("JSON file is valid.");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine($"JSON file is invalid: {result.ErrorMessage}");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}