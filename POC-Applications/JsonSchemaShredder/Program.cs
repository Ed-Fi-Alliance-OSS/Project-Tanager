// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using JsonSchemaShredder;
using Npgsql;

class Program
{
  static int Main(string[] args)
  {
    var jsonFilePath =
      args.Length > 0
        ? args[0]
        : throw new ArgumentException("JSON schema file path is required as the first argument.");

    string? connectionString = null;
    if (args.Length > 1)
    {
      connectionString = args[1];
    }

    if (!File.Exists(jsonFilePath))
    {
      Console.WriteLine($"Error: File '{jsonFilePath}' not found.");
      Console.WriteLine(
        "Usage: JsonSchemaShredder [schema-file.json] [PostgreSQL-connection-string]"
      );
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
      Console.WriteLine(new string('=', 50));

      if (!string.IsNullOrEmpty(connectionString))
      {
        Console.WriteLine("\nAttempting to execute script on PostgreSQL database...");
        try
        {
          using var conn = new NpgsqlConnection(connectionString);
          conn.Open();
          using var cmd = new NpgsqlCommand(postgresScript, conn);
          cmd.ExecuteNonQuery();
          Console.WriteLine("Script executed successfully on the database.");
        }
        catch (Exception dbEx)
        {
          Console.WriteLine($"Database execution error: {dbEx.Message}");
          return 2;
        }
      }

      return 0;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
      return 1;
    }
  }
}
