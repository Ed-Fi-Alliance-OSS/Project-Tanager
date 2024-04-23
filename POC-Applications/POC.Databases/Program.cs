// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

TestDMSDataBase(includeQueryTables: true);

void TestDMSDataBase(bool includeQueryTables)
{
  try
  {
    string masterConnectionString =
      "Data Source=(local);Initial Catalog=master;Integrated Security=True; Encrypt=false";

    string sqlDbQuery = File.ReadAllText("Artifacts\\MsSql\\00001-CreateDMSDatabase.sql");
    ExecuteSqlQuery(sqlDbQuery, masterConnectionString);

    string sqlTablesQuery = File.ReadAllText("Artifacts\\MsSql\\00002-CreateDMSTables.sql");
    ExecuteSqlQuery(sqlTablesQuery, masterConnectionString);

    if (includeQueryTables)
    {
      string sqlQueryTablesQuery = File.ReadAllText("Artifacts\\MsSql\\00003-QueryTables.sql");
      ExecuteSqlQuery(sqlQueryTablesQuery, masterConnectionString);
    }
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

    string studentJson = File.ReadAllText("TestData\\Student.json");
    var studentItems = JsonSerializer.Deserialize<JsonNode>(studentJson);

    using (SqlConnection connection = new SqlConnection(connectionString))
    {
      connection.Open();
      SeedData(connection);
      var documents = GetDocuments(connection);
      UpdateDocuments(connection, documents);
    }

    void SeedData(SqlConnection connection)
    {
      ReferencedItem[]? schoolReferences = null;
      ReferencedItem[]? sectionReferences = null;

      // Add school document
      if (schoolItems != null)
      {
        Console.WriteLine($"School Inserts StartTime:" + DateTime.Now.ToString());
        schoolReferences = InsertDocuments(
          schoolItems.AsArray(),
          "School",
          connection,
          true,
          null,
          5
        );
        Console.WriteLine($"School Inserts EndTime:" + DateTime.Now.ToString());
      }

      // Add Section document
      if (sectionItems != null)
      {
        Console.WriteLine($"Section Inserts StartTime:" + DateTime.Now.ToString());
        sectionReferences = InsertDocuments(
          sectionItems.AsArray(),
          "Section",
          connection,
          true,
          null,
          5
        );
        Console.WriteLine($"Section Inserts EndTime:" + DateTime.Now.ToString());
      }

      // Add student documents
      if (studentItems != null)
      {
        Console.WriteLine($"Student Inserts StartTime:" + DateTime.Now.ToString());
        InsertDocuments(
          studentItems.AsArray(),
          "Student",
          connection,
          false,
          schoolReferences,
          10000
        );
        Console.WriteLine($"Student Inserts EndTime:" + DateTime.Now.ToString());
      }

      // Add StudentSchoolAssociation document
      if (studentSchoolAssoItems != null)
      {
        Console.WriteLine($"StudentSchoolAssociation Inserts StartTime:" + DateTime.Now.ToString());
        InsertDocuments(
          studentSchoolAssoItems.AsArray(),
          "StudentSchoolAssociation",
          connection,
          false,
          schoolReferences,
          10000
        );
        Console.WriteLine($"StudentSchoolAssociation Inserts EndTime:" + DateTime.Now.ToString());
      }

      // Add StudentSectionAssociation document
      if (studentSectionAssoItems != null)
      {
        Console.WriteLine(
          $"StudentSectionAssociation Inserts StartTime:" + DateTime.Now.ToString()
        );
        var noReference = InsertDocuments(
          studentSectionAssoItems.AsArray(),
          "StudentSectionAssociation",
          connection,
          false,
          sectionReferences,
          10000
        );
        Console.WriteLine($"StudentSectionAssociation Inserts EndTime:" + DateTime.Now.ToString());
      }
    }

    ReferencedItem[]? InsertDocuments(
      JsonArray items,
      string resourceName,
      SqlConnection connection,
      bool isSubClass,
      ReferencedItem[]? referencedItems,
      int numberofiterations = 1
    )
    {
      long insertedAliasId = 0;

      string documentInsertQuery =
        $"INSERT INTO dbo.[Documents] (document_partition_key, document_uuid, resource_name, edfi_doc) output INSERTED.ID VALUES (@document_partition_key, @document_uuid, @resource_name, @edfi_doc)";

      string aliasesInsertQuery =
        $"INSERT INTO dbo.[Aliases] (referential_partition_key, referential_id, document_id, document_partition_key) output INSERTED.ID VALUES (@referential_partition_key, @referential_id, @document_id, @document_partition_key)";

      string referenceInsertQuery =
        $"INSERT INTO dbo.[References] (document_id, document_partition_key, referenced_alias_id, referenced_partition_key) VALUES (@document_id, @document_partition_key, @referenced_alias_id, @referenced_partition_key)";

      string queryTableInsertQuery =
        $"INSERT INTO [dbo].[QueryStudentSchoolAssociation] (document_partition_key, document_id, entryDate, schoolId, studentUniqueId) output INSERTED.ID VALUES (@doc_partition_key, @document_id, @entryDate, @schoolId, @studentUniqueId)";

      var referencedResources = new List<ReferencedItem>();

      for (int i = 0; i < numberofiterations; i++)
      {
        foreach (var item in items)
        {
          var documentUUID = Guid.NewGuid();

          long insertedDocId = 0;
          var docPartitionKey = PartitionKey(documentUUID);

          using (SqlCommand command = new SqlCommand(documentInsertQuery, connection))
          {
            command.Parameters.AddWithValue("@document_partition_key", docPartitionKey);
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
            command.Parameters.AddWithValue(
              "@referential_partition_key",
              referential_id_partitionkey
            );
            command.Parameters.AddWithValue("@referential_id", referential_id);
            command.Parameters.AddWithValue("@document_id", insertedDocId);
            command.Parameters.AddWithValue("@document_partition_key", docPartitionKey);
            insertedAliasId = (long)command.ExecuteScalar();
            referencedResources.Add(
              new ReferencedItem(insertedAliasId, referential_id_partitionkey)
            );
          }

          if (isSubClass)
          {
            var superClass_referential_id = Guid.NewGuid();

            var superClass_referential_id_partitionkey = PartitionKey(superClass_referential_id);

            using (SqlCommand command = new SqlCommand(aliasesInsertQuery, connection))
            {
              command.Parameters.AddWithValue(
                "@referential_partition_key",
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
                command.Parameters.AddWithValue("@document_id", insertedDocId);
                command.Parameters.AddWithValue("@document_partition_key", docPartitionKey);
                command.Parameters.AddWithValue("@referenced_alias_id", referencedItem.itemId);
                command.Parameters.AddWithValue(
                  "@referenced_partition_key",
                  referencedItem.partitionKey
                );
                command.ExecuteNonQuery();
              }
            }
          }
          if (includeQueryTables)
          {
            using SqlCommand command = new SqlCommand(queryTableInsertQuery, connection);
            command.Parameters.AddWithValue("@doc_partition_key", docPartitionKey);
            command.Parameters.AddWithValue("@document_id", insertedDocId);
            command.Parameters.AddWithValue("@entryDate", DateTime.Now);
            command.Parameters.AddWithValue("@schoolId", "255901001");
            command.Parameters.AddWithValue(
              "@studentUniqueId",
              documentUUID.ToString("N").Substring(0, 5)
            );
            command.ExecuteNonQuery();
          }
        }
      }

      return referencedResources.ToArray();
    }

    Dictionary<Guid, JsonNode> GetDocuments(SqlConnection connection)
    {
      Console.WriteLine($"Getting 1000 Documents");
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
      Console.WriteLine($"Updating 1000 Documents");
      string updateQuery =
        "UPDATE dbo.[Documents] SET edfi_doc = @json WHERE document_uuid = @id and document_partition_key = @partitionKey";
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
