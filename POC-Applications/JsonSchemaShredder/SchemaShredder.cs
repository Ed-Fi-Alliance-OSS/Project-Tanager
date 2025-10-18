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

        var schemaName = endpointName.GetString()!;
        var scriptBuilder = new StringBuilder();
        var tables = new List<TableDefinition>();
        var indexes = new List<IndexDefinition>();

        // Create schema
        scriptBuilder.AppendLine($"-- PostgreSQL script for schema: {schemaName}");
        scriptBuilder.AppendLine($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";");
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

            // Parse the main table
            var mainTable = ParseTable(schemaName, resourceName, jsonSchema, null);
            tables.Add(mainTable);

            // Extract natural key columns once
            var mainTableNaturalKeyColumns = ExtractNaturalKeyColumns(identityPaths, mainTable.Columns);
            var naturalKeyColumnDefinitions = mainTable.Columns.Where(c => mainTableNaturalKeyColumns.Contains(c.Name)).ToList();

            // Parse nested array tables - pass only the natural key columns as foreign keys
            if (jsonSchema.TryGetProperty("properties", out var properties))
            {
                ParseNestedArrayTables(schemaName, resourceName, properties, tables, naturalKeyColumnDefinitions);
            }

            // Create natural key index for main table
            if (mainTableNaturalKeyColumns.Count > 0)
            {
                indexes.Add(new IndexDefinition(
                    $"nk_{resourceName}",
                    $"\"{schemaName}\".\"{resourceName}\"",
                    mainTableNaturalKeyColumns
                ));
            }

            // Create natural key indexes for child tables
            if (jsonSchema.TryGetProperty("properties", out var propertiesForIndex))
            {
                CreateChildTableIndexes(schemaName, resourceName, propertiesForIndex, tables, naturalKeyColumnDefinitions, indexes);
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

    private TableDefinition ParseTable(string schemaName, string tableName, JsonElement jsonSchema, List<ColumnDefinition>? parentColumns)
    {
        var columns = new List<ColumnDefinition>();
        
        // Add primary key column
        columns.Add(new ColumnDefinition("id", "BIGSERIAL", false, true));

        // Add parent foreign key columns if this is a child table
        if (parentColumns != null)
        {
            var parentTableName = ExtractParentTableName(tableName);
            // Use singular form of parent table name for foreign key prefix
            var fkPrefix = RemoveTrailingS(parentTableName);
            
            foreach (var parentColumn in parentColumns)
            {
                if (!parentColumn.IsPrimaryKey)
                {
                    // Create foreign key column that references the parent table
                    var fkColumnName = $"{fkPrefix}_{parentColumn.Name}";
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
                        var maxLength = GetMaxLength(propertyValue);
                        var dataType = maxLength.HasValue ? $"VARCHAR({maxLength})" : "TEXT";
                        columns.Add(new ColumnDefinition(propertyName, dataType, !isRequired, false));
                        break;

                    case "integer":
                        columns.Add(new ColumnDefinition(propertyName, "INTEGER", !isRequired, false));
                        break;

                    case "boolean":
                        columns.Add(new ColumnDefinition(propertyName, "BOOLEAN", !isRequired, false));
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
                                
                                if (nestedPropertyValue.TryGetProperty("type", out var nestedTypeElement))
                                {
                                    var nestedType = nestedTypeElement.GetString();
                                    var nestedIsRequired = nestedRequiredProperties.Contains(nestedPropertyName);
                                    var flattenedColumnName = $"{propertyName}_{nestedPropertyName}";

                                    switch (nestedType)
                                    {
                                        case "string":
                                            var nestedMaxLength = GetMaxLength(nestedPropertyValue);
                                            var nestedDataType = nestedMaxLength.HasValue ? $"VARCHAR({nestedMaxLength})" : "TEXT";
                                            columns.Add(new ColumnDefinition(flattenedColumnName, nestedDataType, !nestedIsRequired, false));
                                            break;

                                        case "integer":
                                            columns.Add(new ColumnDefinition(flattenedColumnName, "INTEGER", !nestedIsRequired, false));
                                            break;

                                        case "boolean":
                                            columns.Add(new ColumnDefinition(flattenedColumnName, "BOOLEAN", !nestedIsRequired, false));
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        return new TableDefinition($"\"{schemaName}\".\"{tableName}\"", columns);
    }

    private void ParseNestedArrayTables(string schemaName, string parentTableName, JsonElement properties, List<TableDefinition> tables, List<ColumnDefinition> parentColumns)
    {
        foreach (var property in properties.EnumerateObject())
        {
            var propertyName = property.Name;
            var propertyValue = property.Value;

            if (propertyValue.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "array")
            {
                if (propertyValue.TryGetProperty("items", out var items))
                {
                    var childTableName = $"{parentTableName}_{propertyName}";
                    var childTable = ParseTable(schemaName, childTableName, items, parentColumns);
                    tables.Add(childTable);
                }
            }
        }
    }

    private HashSet<string> GetRequiredProperties(JsonElement element)
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

    private int? GetMaxLength(JsonElement element)
    {
        if (element.TryGetProperty("maxLength", out var maxLengthElement))
        {
            return maxLengthElement.GetInt32();
        }
        return null;
    }

    private List<string> ExtractNaturalKeyColumns(JsonElement identityPaths, List<ColumnDefinition> columns)
    {
        var naturalKeyColumns = new List<string>();
        
        foreach (var pathElement in identityPaths.EnumerateArray())
        {
            var jsonPath = pathElement.GetString();
            if (string.IsNullOrEmpty(jsonPath)) continue;

            // Convert JSON path like "$.abc.xyz" to column name like "abc_xyz"
            var columnName = ConvertJsonPathToColumnName(jsonPath);
            
            // Find matching column (case-insensitive)
            var matchingColumn = columns.FirstOrDefault(c => 
                string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
            
            if (matchingColumn != null)
            {
                naturalKeyColumns.Add(matchingColumn.Name);
            }
        }

        return naturalKeyColumns;
    }

    private string ConvertJsonPathToColumnName(string jsonPath)
    {
        // Remove the leading "$." and convert dots to underscores
        if (jsonPath.StartsWith("$."))
        {
            return jsonPath[2..].Replace('.', '_');
        }
        return jsonPath.Replace('.', '_');
    }

    private string GenerateCreateTableStatement(TableDefinition table)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"CREATE TABLE {table.Name} (");
        
        for (int i = 0; i < table.Columns.Count; i++)
        {
            var column = table.Columns[i];
            var nullability = column.IsNullable ? "NULL" : "NOT NULL";
            var primaryKey = column.IsPrimaryKey ? " PRIMARY KEY" : "";
            
            builder.Append($"    \"{column.Name}\" {column.DataType} {nullability}{primaryKey}");
            
            if (i < table.Columns.Count - 1)
                builder.AppendLine(",");
            else
                builder.AppendLine();
        }
        
        builder.Append(");");
        return builder.ToString();
    }

    private string GenerateCreateIndexStatement(IndexDefinition index)
    {
        var columns = string.Join(", ", index.Columns.Select(c => $"\"{c}\""));
        return $"CREATE INDEX \"{index.Name}\" ON {index.TableName} ({columns});";
    }

    private string ExtractParentTableName(string childTableName)
    {
        // Extract the parent table name from a child table name like "parent_child" -> "parent"
        var lastUnderscoreIndex = childTableName.LastIndexOf('_');
        return lastUnderscoreIndex > 0 ? childTableName.Substring(0, lastUnderscoreIndex) : childTableName;
    }

    private string RemoveTrailingS(string tableName)
    {
        // Convert plural table names to singular for foreign key prefixes
        // e.g., "studentEducationOrganizationAssociations" -> "studentEducationOrganizationAssociation"
        if (tableName.EndsWith("s", StringComparison.OrdinalIgnoreCase) && tableName.Length > 1)
        {
            return tableName.Substring(0, tableName.Length - 1);
        }
        return tableName;
    }

    private void CreateChildTableIndexes(string schemaName, string parentTableName, JsonElement properties, List<TableDefinition> tables, List<ColumnDefinition> parentColumns, List<IndexDefinition> indexes)
    {
        foreach (var property in properties.EnumerateObject())
        {
            var propertyName = property.Name;
            var propertyValue = property.Value;

            if (propertyValue.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "array")
            {
                if (propertyValue.TryGetProperty("items", out var items))
                {
                    var childTableName = $"{parentTableName}_{propertyName}";
                    var childTable = tables.FirstOrDefault(t => t.Name.EndsWith($"\"{childTableName}\""));
                    
                    if (childTable != null)
                    {
                        var naturalKeyColumns = new List<string>();
                        
                        // Add parent foreign key columns to natural key
                        var fkPrefix = RemoveTrailingS(parentTableName);
                        foreach (var parentColumn in parentColumns)
                        {
                            if (!parentColumn.IsPrimaryKey)
                            {
                                naturalKeyColumns.Add($"{fkPrefix}_{parentColumn.Name}");
                            }
                        }

                        // Add required columns from the child table to natural key
                        if (items.TryGetProperty("required", out var requiredArray))
                        {
                            foreach (var requiredItem in requiredArray.EnumerateArray())
                            {
                                var requiredColumn = requiredItem.GetString();
                                if (!string.IsNullOrEmpty(requiredColumn))
                                {
                                    naturalKeyColumns.Add(requiredColumn);
                                }
                            }
                        }

                        if (naturalKeyColumns.Count > 0)
                        {
                            indexes.Add(new IndexDefinition(
                                $"nk_{childTableName}",
                                $"\"{schemaName}\".\"{childTableName}\"",
                                naturalKeyColumns
                            ));
                        }
                    }
                }
            }
        }
    }
}

public record TableDefinition(string Name, List<ColumnDefinition> Columns);

public record ColumnDefinition(string Name, string DataType, bool IsNullable, bool IsPrimaryKey);

public record IndexDefinition(string Name, string TableName, List<string> Columns);