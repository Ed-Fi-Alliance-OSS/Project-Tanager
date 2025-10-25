using System.Security.Cryptography;
using System.Text;

namespace JsonSchemaShredder;

public static class DbEntityName
{
  private const byte PostgreSQLMaxLength = 64;
  private const byte HashLength = 6;

  private const byte ShortenedLength = PostgreSQLMaxLength - HashLength - 1;

  private static readonly SHA256 _hasher = SHA256.Create();

  public static string Normalize(string entityName)
  {
    // Convert plural table names to singular for foreign key prefixes
    // e.g., "studentEducationOrganizationAssociations" -> "studentEducationOrganizationAssociation"
    // Note: This uses simple pluralization logic suitable for Ed-Fi resource naming conventions
    if (string.IsNullOrWhiteSpace(entityName) || entityName.Length <= 1)
    {
      return entityName;
    }

    var lower = entityName.ToLower();
    var normalized = entityName;

    if (lower.EndsWith("people"))
    {
      normalized = "person";
    }
    else if (lower.EndsWith("quizzes"))
    {
      normalized = entityName[..^3];
    }
    else if (lower.EndsWith("address") || lower.EndsWith("class"))
    {
      normalized = entityName;
    }
    else if (lower.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
    {
      normalized = entityName[..^3] + "y";
    }
    else if (
      lower.EndsWith("sses", StringComparison.OrdinalIgnoreCase)
      || lower.EndsWith("shes", StringComparison.OrdinalIgnoreCase)
      || lower.EndsWith("ches", StringComparison.OrdinalIgnoreCase)
      || lower.EndsWith("xes", StringComparison.OrdinalIgnoreCase)
      || lower.EndsWith("zes", StringComparison.OrdinalIgnoreCase)
    )
    {
      normalized = entityName[..^2];
    }
    else if (lower.EndsWith("s", StringComparison.OrdinalIgnoreCase))
    {
      normalized = entityName[..^1];
    }

    return Capitalize(normalized);
  }

  public static string Capitalize(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    return char.ToUpper(input[0]) + input[1..];
  }

  public static string Shorten(string entityName)
  {
    _ = entityName ?? throw new InvalidOperationException($"{nameof(entityName)} cannot be null");

    if (entityName.Length <= PostgreSQLMaxLength)
    {
      return entityName;
    }

    var hash = _hasher.ComputeHash(Encoding.UTF8.GetBytes(entityName));

    var truncatedHash = BitConverter.ToString(hash).Replace("-", string.Empty)[..6].ToLower();

    return $"{entityName[0..ShortenedLength]}_{truncatedHash}";
  }
}
