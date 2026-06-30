using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace EvStorionX.Infrastructure.Persistence;

internal static class SnakeCaseNamingExtensions
{
    // Handles sequences like "UTCDate" → "utc_date" and simple "MyField" → "my_field".
    private static readonly Regex UpperSequence = new(@"([A-Z]+)([A-Z][a-z])", RegexOptions.Compiled);
    private static readonly Regex UpperChar = new(@"(?<=[a-z0-9])([A-Z])", RegexOptions.Compiled);

    internal static string ToSnakeCase(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
        name = UpperSequence.Replace(name, "$1_$2");
        name = UpperChar.Replace(name, "_$1");
        return name.ToLowerInvariant();
    }

    /// <summary>
    /// Renames all tables, columns, keys, indexes, and FK constraints to snake_case.
    /// Call this AFTER <c>ApplyConfigurationsFromAssembly</c> in <c>OnModelCreating</c>.
    /// </summary>
    internal static ModelBuilder UseSnakeCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } table)
                entity.SetTableName(ToSnakeCase(table));

            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(ToSnakeCase(prop.GetColumnName()));

            foreach (var key in entity.GetKeys())
                if (key.GetName() is { } kn) key.SetName(ToSnakeCase(kn));

            foreach (var idx in entity.GetIndexes())
                if (idx.GetDatabaseName() is { } idn) idx.SetDatabaseName(ToSnakeCase(idn));

            foreach (var fk in entity.GetForeignKeys())
                if (fk.GetConstraintName() is { } cn) fk.SetConstraintName(ToSnakeCase(cn));
        }

        return modelBuilder;
    }
}
