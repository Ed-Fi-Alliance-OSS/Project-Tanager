// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text;
using System.Text.Json;

namespace JsonSchemaShredder;

public class SchemaShredder
{
  private const string DescriptorSuffix = "Descriptors";

  public string GeneratePostgreSqlScript(JsonDocument jsonDocument)
  {
    var root = jsonDocument.RootElement;

    if (!root.TryGetProperty("projectSchema", out var projectSchema))
      throw new InvalidOperationException("Missing 'projectSchema' property");

    if (!projectSchema.TryGetProperty("projectEndpointName", out var endpointName))
      throw new InvalidOperationException("Missing 'projectEndpointName' property");

    if (!projectSchema.TryGetProperty("resourceSchemas", out var resourceSchemas))
      throw new InvalidOperationException("Missing 'resourceSchemas' property");

    var schemaName = endpointName.GetString()!.Replace("-", string.Empty);
    var scriptBuilder = new StringBuilder();
    var tables = new List<TableDefinition>();
    var indexes = new List<IndexDefinition>();

    // Create schema
    scriptBuilder.AppendLine($"-- PostgreSQL script for schema: {schemaName}");
    scriptBuilder.AppendLine($"CREATE SCHEMA IF NOT EXISTS {schemaName};");
    scriptBuilder.AppendLine();

    // Process each resource schema
    foreach (var resourceProperty in resourceSchemas.EnumerateObject())
    {
      var resourceName = resourceProperty.Name;

      // Skip descriptors
      if (resourceName.EndsWith(DescriptorSuffix, StringComparison.OrdinalIgnoreCase))
        continue;

      var resourceValue = resourceProperty.Value;

      if (!resourceValue.TryGetProperty("jsonSchemaForInsert", out var jsonSchema))
        continue;

      if (!resourceValue.TryGetProperty("identityJsonPaths", out var identityPaths))
        continue;

      var normalizedName = DbEntityName.Normalize(resourceName);
      var shortenedTableName = DbEntityName.Shorten(normalizedName);

      // Parse the main table
      var mainTable = TranslateSchemaToTable(
        schemaName,
        normalizedName,
        shortenedTableName,
        jsonSchema,
        null
      );
      tables.Add(mainTable);

      // Extract natural key columns once
      var mainTableNaturalKeyColumns = ExtractNaturalKeyColumns(identityPaths, mainTable.Columns);
      var naturalKeyColumnDefinitions = mainTable
        .Columns.Where(c => mainTableNaturalKeyColumns.Contains(c.Name))
        .ToList();

      // Parse nested array tables - pass only the natural key columns as foreign keys
      if (jsonSchema.TryGetProperty("properties", out var properties))
      {
        TranslateNestedArraySchema(
          schemaName,
          resourceName,
          properties,
          tables,
          naturalKeyColumnDefinitions
        );
      }

      // Create natural key index for main table
      if (naturalKeyColumnDefinitions.Count > 0)
      {
        indexes.Add(
          new IndexDefinition(
            $"nk_{resourceName}",
            $"{schemaName}.{shortenedTableName}",
            [.. naturalKeyColumnDefinitions.Select(c => DbEntityName.Normalize(c.Name))]
          )
        );
      }

      // Create natural key indexes for child tables
      if (jsonSchema.TryGetProperty("properties", out var propertiesForIndex))
      {
        CreateChildTableIndexes(
          schemaName,
          resourceName,
          propertiesForIndex,
          tables,
          naturalKeyColumnDefinitions,
          indexes
        );
      }
    }

    // Generate CREATE TABLE statements
    foreach (var table in tables)
    {
      scriptBuilder.AppendLine(GenerateCreateTableStatement(table));
      scriptBuilder.AppendLine();
    }

    // Generate CREATE INDEX statements
    foreach (var index in indexes)
    {
      scriptBuilder.AppendLine(GenerateCreateIndexStatement(index));
    }

    return scriptBuilder.ToString();
  }

  private static TableDefinition TranslateSchemaToTable(
    string schemaName,
    string fullTableName,
    string shortenedTableName,
    JsonElement jsonSchema,
    List<ColumnDefinition>? parentColumns
  )
  {
    var columns = new List<ColumnDefinition>
    {
      // Add primary key column
      new("id", "BIGSERIAL", false, true),
    };

    // Add parent foreign key columns if this is a child table
    if (parentColumns != null)
    {
      foreach (var parentColumn in parentColumns)
      {
        if (!parentColumn.IsPrimaryKey)
        {
          // Create foreign key column that references the parent table
          var fkColumnName = DbEntityName.Capitalize(parentColumn.Name);
          columns.Add(new ColumnDefinition(fkColumnName, parentColumn.DataType, false, false));
        }
      }
    }

    if (jsonSchema.TryGetProperty("properties", out var properties))
    {
      var requiredProperties = GetRequiredProperties(jsonSchema);

      foreach (var property in properties.EnumerateObject())
      {
        var propertyName = property.Name;
        var propertyValue = property.Value;

        if (!propertyValue.TryGetProperty("type", out var typeElement))
          continue;

        var type = typeElement.GetString();
        var isRequired = requiredProperties.Contains(propertyName);

        switch (type)
        {
          case "string":
            var dataType = GetDataTypeForProperty(propertyValue, isRequired);
            // columns.Add(new ColumnDefinition(propertyName, dataType, !isRequired, false));
            SafelyAdd(columns, propertyName, isRequired, dataType);
            break;

          case "integer":
            // columns.Add(new ColumnDefinition(propertyName, "INTEGER", !isRequired, false));
            SafelyAdd(columns, propertyName, isRequired, "INTEGER");
            break;

          case "boolean":
            // columns.Add(new ColumnDefinition(propertyName, "BOOLEAN", !isRequired, false));
            SafelyAdd(columns, propertyName, isRequired, "BOOLEAN");
            break;

          case "array":
            // Arrays are handled as separate tables - skip here
            break;

          case "object":
            // Flatten object properties
            if (propertyValue.TryGetProperty("properties", out var nestedProperties))
            {
              var nestedRequiredProperties = GetRequiredProperties(propertyValue);
              foreach (var nestedProperty in nestedProperties.EnumerateObject())
              {
                var nestedPropertyName = nestedProperty.Name;
                var nestedPropertyValue = nestedProperty.Value;
                var nestedIsRequired = nestedRequiredProperties.Contains(nestedPropertyName);

                // Use recursive approach for consistent type handling
                var nestedDataType = GetDataTypeForProperty(nestedPropertyValue, nestedIsRequired);

                // Check if column already exists to avoid duplicates
                SafelyAdd(columns, nestedPropertyName, nestedIsRequired, nestedDataType);
              }
            }
            break;
        }
      }
    }

    return new TableDefinition(fullTableName, $"{schemaName}.{shortenedTableName}", columns);

    static void SafelyAdd(
      List<ColumnDefinition> columns,
      string columnName,
      bool isRequired,
      string dataType
    )
    {
      if (!columns.Any(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase)))
      {
        columns.Add(new ColumnDefinition(columnName, dataType, !isRequired, false));
      }
    }
  }

  private static void TranslateNestedArraySchema(
    string schemaName,
    string parentTableName,
    JsonElement properties,
    List<TableDefinition> tables,
    List<ColumnDefinition> parentColumns
  )
  {
    foreach (var property in properties.EnumerateObject())
    {
      var propertyName = property.Name;
      var propertyValue = property.Value;

      if (
        propertyValue.TryGetProperty("type", out var typeElement)
        && typeElement.GetString() == "array"
      )
      {
        if (propertyValue.TryGetProperty("items", out var items))
        {
          var fullChildTableName =
            $"{DbEntityName.Normalize(parentTableName)}{DbEntityName.Normalize(propertyName)}";
          var shortenedChildTableName = DbEntityName.Shorten(fullChildTableName);
          var childTable = TranslateSchemaToTable(
            schemaName,
            fullChildTableName,
            shortenedChildTableName,
            items,
            parentColumns
          );
          tables.Add(childTable);

          // Recursively process nested arrays within the child table
          if (items.TryGetProperty("properties", out var childProperties))
          {
            // Use the child table's natural key columns as foreign keys for grandchild tables
            var childNaturalKeyColumns = childTable.Columns.Where(c => !c.IsPrimaryKey).ToList();
            TranslateNestedArraySchema(
              schemaName,
              shortenedChildTableName,
              childProperties,
              tables,
              childNaturalKeyColumns
            );
          }
        }
      }
    }
  }

  private static HashSet<string> GetRequiredProperties(JsonElement element)
  {
    var required = new HashSet<string>();
    if (element.TryGetProperty("required", out var requiredArray))
    {
      foreach (var item in requiredArray.EnumerateArray())
      {
        var value = item.GetString();
        if (!string.IsNullOrEmpty(value))
          required.Add(value);
      }
    }
    return required;
  }

  private static int? GetMaxLength(JsonElement element)
  {
    if (element.TryGetProperty("maxLength", out var maxLengthElement))
    {
      return maxLengthElement.GetInt32();
    }
    return null;
  }

  private static string GetDataTypeForProperty(JsonElement propertyValue, bool isRequired)
  {
    if (!propertyValue.TryGetProperty("type", out var typeElement))
      return "TEXT";

    var type = typeElement.GetString();

    switch (type)
    {
      case "string":
        // Check for format property for date/time handling
        if (propertyValue.TryGetProperty("format", out var formatElement))
        {
          var format = formatElement.GetString();
          switch (format)
          {
            case "date":
              return "DATE";
            case "time":
              return "TIME";
          }
        }

        // Handle maxLength for string types
        var maxLength = GetMaxLength(propertyValue);
        return maxLength.HasValue ? $"VARCHAR({maxLength})" : "TEXT";

      case "integer":
        return "INTEGER";

      case "boolean":
        return "BOOLEAN";

      default:
        return "TEXT";
    }
  }

  private List<string> ExtractNaturalKeyColumns(
    JsonElement identityPaths,
    List<ColumnDefinition> columns
  )
  {
    var naturalKeyColumns = new List<string>();

    foreach (var pathElement in identityPaths.EnumerateArray())
    {
      var jsonPath = pathElement.GetString();
      if (string.IsNullOrEmpty(jsonPath))
      {
        continue;
      }

      var columnName = ConvertJsonPathToColumnName(jsonPath);

      // Find matching column (case-insensitive)
      var matchingColumn = columns.FirstOrDefault(c =>
        string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase)
      );

      if (matchingColumn != null)
      {
        naturalKeyColumns.Add(matchingColumn.Name);
      }
    }

    return naturalKeyColumns;
  }

  private static string ConvertJsonPathToColumnName(string jsonPath)
  {
    // Remove the leading "$." and get just the last part (nested property name)
    if (jsonPath.StartsWith("$.") && jsonPath.Length > 2)
    {
      var pathWithoutPrefix = jsonPath[2..];
      // Get the last part after the final dot
      var lastDotIndex = pathWithoutPrefix.LastIndexOf('.');
      if (lastDotIndex >= 0)
      {
        return pathWithoutPrefix[(lastDotIndex + 1)..];
      }
      return pathWithoutPrefix;
    }

    // For paths without $. prefix, get the last part after the final dot
    var dotIndex = jsonPath.LastIndexOf('.');
    if (dotIndex >= 0)
    {
      return jsonPath[(dotIndex + 1)..];
    }
    return jsonPath;
  }

  private static string GenerateCreateTableStatement(TableDefinition table)
  {
    var builder = new StringBuilder();
    builder.AppendLine($"-- {table.FullName}");
    builder.AppendLine($"CREATE TABLE {table.ShortenedName} (");

    for (int i = 0; i < table.Columns.Count; i++)
    {
      var column = table.Columns[i];
      var nullability = column.IsNullable ? "NULL" : "NOT NULL";
      var primaryKey = column.IsPrimaryKey ? " PRIMARY KEY" : "";

      builder.Append(
        $"    {DbEntityName.Shorten(DbEntityName.Capitalize(column.Name))} {column.DataType} {nullability}{primaryKey}"
      );

      if (i < table.Columns.Count - 1)
        builder.AppendLine(",");
      else
        builder.AppendLine();
    }

    builder.Append(");");
    return builder.ToString();
  }

  private static string GenerateCreateIndexStatement(IndexDefinition index)
  {
    var finalTableName = DbEntityName.Shorten(index.TableName);
    var finalIndexName = DbEntityName.Shorten(index.Name);

    var columns = string.Join(", ", index.Columns.Select(c => $"{DbEntityName.Shorten(c)}"));
    return $"CREATE INDEX {finalIndexName} ON {finalTableName} ({columns});";
  }

  private static void CreateChildTableIndexes(
    string schemaName,
    string parentTableName,
    JsonElement properties,
    List<TableDefinition> tables,
    List<ColumnDefinition> parentColumns,
    List<IndexDefinition> indexes
  )
  {
    foreach (var property in properties.EnumerateObject())
    {
      var propertyName = property.Name;
      var propertyValue = property.Value;

      if (
        propertyValue.TryGetProperty("type", out var typeElement)
        && typeElement.GetString() == "array"
      )
      {
        if (propertyValue.TryGetProperty("items", out var items))
        {
          var childTableName =
            $"{DbEntityName.Normalize(parentTableName)}{DbEntityName.Normalize(propertyName)}";
          var childTable = tables.FirstOrDefault(t =>
            t.ShortenedName.EndsWith($"{childTableName}")
          );

          if (childTable != null)
          {
            var naturalKeyColumns = new List<string>();

            // Add parent foreign key columns to natural key
            var fkPrefix = DbEntityName.Normalize(parentTableName);
            foreach (var parentColumn in parentColumns)
            {
              if (!parentColumn.IsPrimaryKey)
              {
                naturalKeyColumns.Add(DbEntityName.Capitalize(parentColumn.Name));
              }
            }

            // Add required columns from the child table to natural key
            if (items.TryGetProperty("required", out var requiredArray))
            {
              foreach (var requiredItem in requiredArray.EnumerateArray())
              {
                var requiredColumn = requiredItem.GetString();
                if (string.IsNullOrEmpty(requiredColumn))
                  continue;

                if (requiredColumn.EndsWith("Reference"))
                {
                  foreach (
                    var referenceRequired in items
                      .GetProperty("properties")
                      .GetProperty(requiredColumn)
                      .GetProperty("properties")
                      .EnumerateObject()
                  )
                  {
                    naturalKeyColumns.Add(DbEntityName.Capitalize(referenceRequired.Name));
                  }
                }
                else
                {
                  naturalKeyColumns.Add(DbEntityName.Capitalize(requiredColumn));
                }
              }
            }

            if (naturalKeyColumns.Count > 0)
            {
              indexes.Add(
                new IndexDefinition(
                  $"nk_{childTableName}",
                  $"{schemaName}.{childTableName}",
                  naturalKeyColumns
                )
              );
            }
          }
        }
      }
    }
  }
}

public record TableDefinition(
  string FullName,
  string ShortenedName,
  List<ColumnDefinition> Columns
);

public record ColumnDefinition(string Name, string DataType, bool IsNullable, bool IsPrimaryKey);

public record IndexDefinition(string Name, string TableName, List<string> Columns);
