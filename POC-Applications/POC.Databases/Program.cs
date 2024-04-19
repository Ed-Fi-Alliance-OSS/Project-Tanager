// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

TestDMSDataBase();

void TestDMSDataBase()
{
  try
  {
    string masterConnectionString =
      "Data Source=(local);Initial Catalog=master;Integrated Security=True; Encrypt=false";

    string sqlDbQuery = File.ReadAllText("Artifacts\\MsSql\\00001-CreateDMSDatabase.sql");
    ExecuteSqlQuery(sqlDbQuery, masterConnectionString);

    string sqlTablesQuery = File.ReadAllText("Artifacts\\MsSql\\00002-CreateDMSTables.sql");
    ExecuteSqlQuery(sqlTablesQuery, masterConnectionString);
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error: {ex.Message}");
  }

  void ExecuteSqlQuery(string query, string connectionstring)
  {
    using (SqlConnection connection = new SqlConnection(connectionstring))
    {
      connection.Open();

      using SqlCommand command = new SqlCommand(query, connection);
      command.ExecuteNonQuery();
      Console.WriteLine("SQL query executed successfully.");
    }
  }

  string connectionString =
    $"Data Source=(local);Integrated Security=True;Initial Catalog=EdFi_DataManagementService;Encrypt=false";
  try
  {
    string studentSchoolAssoJson = File.ReadAllText("TestData\\StudentsSchoolAssociation.json");
    var studentSchoolAssoItems = JsonSerializer.Deserialize<JsonNode>(studentSchoolAssoJson);

    string studentSectionAssoJson = File.ReadAllText("TestData\\StudentsSectionAssociation.json");
    var studentSectionAssoItems = JsonSerializer.Deserialize<JsonNode>(studentSectionAssoJson);

    string schoolJson = File.ReadAllText("TestData\\Schools.json");
    var schoolItems = JsonSerializer.Deserialize<JsonNode>(schoolJson);

    string sectionJson = File.ReadAllText("TestData\\Sections.json");
    var sectionItems = JsonSerializer.Deserialize<JsonNode>(sectionJson);

    using (SqlConnection connection = new SqlConnection(connectionString))
    {
      connection.Open();
      SeedData(connection);
      var documents = GetDocuments(connection);
      UpdateDocuments(connection, documents);
    }

    void SeedData(SqlConnection connection)
    {
      var schoolReference = new ReferencedItem(0, null);
      var sectionReference = new ReferencedItem(0, null);

      // Add school document
      if (schoolItems != null)
      {
        schoolReference = InsertDocuments(schoolItems.AsArray(), "School", connection, true, null);
      }

      // Add Section document
      if (sectionItems != null)
      {
        sectionReference = InsertDocuments(
          sectionItems.AsArray(),
          "Section",
          connection,
          true,
          null
        );
      }

      // Add StudentSchoolAssociation document
      if (studentSchoolAssoItems != null)
      {
        // Bulk insert
        InsertDocuments(
          studentSchoolAssoItems.AsArray(),
          "StudentSchoolAssociation",
          connection,
          false,
          [schoolReference!],
          1000000
        );
      }

      // Add StudentSectionAssociation document
      if (studentSectionAssoItems != null)
      {
        var noReference = InsertDocuments(
          studentSectionAssoItems.AsArray(),
          "StudentSectionAssociation",
          connection,
          false,
          [sectionReference!],
          10000
        );
      }
    }

    ReferencedItem? InsertDocuments(
      JsonArray items,
      string resourceName,
      SqlConnection connection,
      bool isSubClass,
      ReferencedItem[]? referencedItems,
      int numberofiterations = 1
    )
    {
      ReferencedItem? referencedResource = null;
      long insertedAliasId = 0;

      string documentInsertQuery =
        $"INSERT INTO dbo.[Documents] (partition_key, document_uuid, resource_name, edfi_doc) output INSERTED.ID VALUES (@partition_key, @document_uuid, @resource_name, @edfi_doc)";

      string aliasesInsertQuery =
        $"INSERT INTO dbo.[Aliases] (partition_key, referential_id, document_id, document_partition_key) output INSERTED.ID VALUES (@partition_key, @referential_id, @document_id, @document_partition_key)";

      string referenceInsertQuery =
        $"INSERT INTO dbo.[References] (partition_key, parent_alias_id, parent_partition_key, referenced_alias_id, referenced_partition_key) VALUES (@partition_key, @parent_alias_id, @parent_partition_key, @referenced_alias_id, @referenced_partition_key)";

      for (int i = 0; i < numberofiterations; i++)
      {
        foreach (var item in items)
        {
          var documentUUID = Guid.NewGuid();

          long insertedDocId = 0;
          var docPartitionKey = PartitionKey(documentUUID);

          using (SqlCommand command = new SqlCommand(documentInsertQuery, connection))
          {
            command.Parameters.AddWithValue("@partition_key", docPartitionKey);
            command.Parameters.AddWithValue("@document_uuid", documentUUID);
            command.Parameters.AddWithValue("@resource_name", resourceName);
            var byteArray = Encoding.ASCII.GetBytes(item?.ToJsonString()!);
            var doc = command.Parameters.Add("@edfi_doc", SqlDbType.VarBinary, byteArray.Length);
            doc.Value = byteArray;
            insertedDocId = (long)command.ExecuteScalar();
          }

          var referential_id = Guid.NewGuid();

          var referential_id_partitionkey = PartitionKey(referential_id);

          using (SqlCommand command = new SqlCommand(aliasesInsertQuery, connection))
          {
            command.Parameters.AddWithValue("@partition_key", referential_id_partitionkey);
            command.Parameters.AddWithValue("@referential_id", referential_id);
            command.Parameters.AddWithValue("@document_id", insertedDocId);
            command.Parameters.AddWithValue("@document_partition_key", docPartitionKey);
            insertedAliasId = (long)command.ExecuteScalar();
            referencedResource = new ReferencedItem(insertedAliasId, referential_id_partitionkey);
          }

          if (isSubClass)
          {
            var superClass_referential_id = Guid.NewGuid();

            var superClass_referential_id_partitionkey = PartitionKey(superClass_referential_id);

            using (SqlCommand command = new SqlCommand(aliasesInsertQuery, connection))
            {
              command.Parameters.AddWithValue(
                "@partition_key",
                superClass_referential_id_partitionkey
              );
              command.Parameters.AddWithValue("@referential_id", superClass_referential_id);
              command.Parameters.AddWithValue("@document_id", insertedDocId);
              command.Parameters.AddWithValue("@document_partition_key", docPartitionKey);
              command.ExecuteNonQuery();
            }
          }

          if (referencedItems != null && referencedItems.Any())
          {
            foreach (var referencedItem in referencedItems)
            {
              using (SqlCommand command = new SqlCommand(referenceInsertQuery, connection))
              {
                command.Parameters.AddWithValue("@partition_key", referential_id_partitionkey);
                command.Parameters.AddWithValue("@parent_alias_id", insertedAliasId);
                command.Parameters.AddWithValue(
                  "@parent_partition_key",
                  referential_id_partitionkey
                );
                command.Parameters.AddWithValue("@referenced_alias_id", referencedItem.itemId);
                command.Parameters.AddWithValue(
                  "@referenced_partition_key",
                  referencedItem.partitionKey
                );
                command.ExecuteNonQuery();
              }
            }
          }
        }
      }

      return referencedResource;
    }

    Dictionary<Guid, JsonNode> GetDocuments(SqlConnection connection)
    {
      var seletQuery = "SELECT TOP (1000) document_uuid, edfi_doc from dbo.[Documents]";

      using SqlCommand command = new SqlCommand(seletQuery, connection);

      var records = new Dictionary<Guid, JsonNode>();

      try
      {
        using (SqlDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            Guid uuid = (Guid)reader["document_uuid"];
            byte[] varBinaryData = (byte[])reader["edfi_doc"];
            string varBinaryString = Encoding.UTF8.GetString(varBinaryData);
            var json = JsonNode.Parse(varBinaryString);
            records.Add(uuid, json);
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("An error occurred: " + ex.Message);
      }

      return records;
    }

    void UpdateDocuments(SqlConnection connection, Dictionary<Guid, JsonNode> documents)
    {
      string updateQuery =
        "UPDATE dbo.[Documents] SET edfi_doc = @json WHERE document_uuid = @id and partition_key = @partitionKey";
      try
      {
        foreach (var item in documents)
        {
          using SqlCommand command = new SqlCommand(updateQuery, connection);
          var byteArray = Encoding.ASCII.GetBytes(item.Value?.ToJsonString()!);
          command.Parameters.AddWithValue("@json", byteArray);
          command.Parameters.AddWithValue("@id", item.Key);

          var partitionKey = PartitionKey(item.Key);
          command.Parameters.AddWithValue("@partitionKey", partitionKey);

          int rowsAffected = command.ExecuteNonQuery();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("An error occurred: " + ex.Message);
      }
    }

    int PartitionKey(Guid id)
    {
      // Calculate partition key
      byte[] bytes = id.ToByteArray();

      // Extract the last byte and perform a bitwise AND operation to get the 4-bit partition ID
      int partitionKey = bytes[bytes.Length - 1] & 15;

      return partitionKey;
    }
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error: {ex.Message}");
  }
}

public record DocumentItem(JsonNode content, int partitionKey);

public record ReferencedItem(long itemId, int? partitionKey);
