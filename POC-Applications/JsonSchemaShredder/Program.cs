// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using JsonSchemaShredder;
using System.Text.Json;

// Read JSON schema document
var jsonFilePath = args.Length > 0 ? args[0] : "example-schema.json";
if (!File.Exists(jsonFilePath))
{
    Console.WriteLine($"Error: File '{jsonFilePath}' not found.");
    Console.WriteLine("Usage: JsonSchemaShredder [schema-file.json]");
    return 1;
}

try
{
    var jsonContent = File.ReadAllText(jsonFilePath);
    var jsonDocument = JsonDocument.Parse(jsonContent);
    
    var shredder = new SchemaShredder();
    var postgresScript = shredder.GeneratePostgreSqlScript(jsonDocument);
    
    // Write the script to a file
    var outputFilePath = Path.ChangeExtension(jsonFilePath, ".sql");
    File.WriteAllText(outputFilePath, postgresScript);
    
    Console.WriteLine($"PostgreSQL script generated successfully: {outputFilePath}");
    Console.WriteLine();
    Console.WriteLine("Generated SQL:");
    Console.WriteLine(new string('=', 50));
    Console.WriteLine(postgresScript);
    
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
