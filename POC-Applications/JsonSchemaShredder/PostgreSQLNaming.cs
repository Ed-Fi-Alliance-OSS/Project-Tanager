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
    // Upper case first letter
    entityName = $"{entityName[0].ToString().ToUpper()}{entityName[1..]}";

    // Convert plural table names to singular for foreign key prefixes
    // e.g., "studentEducationOrganizationAssociations" -> "studentEducationOrganizationAssociation"
    // Note: This uses simple pluralization logic suitable for Ed-Fi resource naming conventions
    if (string.IsNullOrWhiteSpace(entityName) || entityName.Length <= 1)
    {
      return entityName;
    }

    // Hard-coded exceptions
    switch (entityName)
    {
      case "people":
        return "person";
    }
    if (entityName.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
    {
      return entityName[..^2] + "y";
    }
    if (entityName.EndsWith("dates", StringComparison.OrdinalIgnoreCase))
    {
      return entityName[..^1];
    }
    if (entityName.EndsWith("es", StringComparison.OrdinalIgnoreCase))
    {
      return entityName[..^2];
    }

    if (entityName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
    {
      // Avoid removing 's' from words that would become meaningless
      var withoutS = entityName[..^1];

      // Basic validation to ensure we don't create obviously incorrect results
      if (withoutS.Length > 2 && !withoutS.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
      {
        return withoutS;
      }
    }
    return entityName;
  }

  public static string Shorten(string entityName, bool preserveUnderscores = false)
  {
    _ = entityName ?? throw new InvalidOperationException($"{nameof(entityName)} cannot be null");

    if (!preserveUnderscores)
    {
      entityName = entityName.Replace("-", string.Empty);
    }

    if (entityName.Length < PostgreSQLMaxLength)
    {
      return entityName;
    }

    var hash = _hasher.ComputeHash(Encoding.UTF8.GetBytes(entityName));

    var truncatedHash = BitConverter.ToString(hash).Replace("-", string.Empty)[..6].ToLower();

    return $"{entityName[0..ShortenedLength]}_{truncatedHash}";
  }
}
