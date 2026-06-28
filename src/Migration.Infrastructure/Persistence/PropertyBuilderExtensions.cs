using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Migration.Infrastructure.Persistence;

internal static class PropertyBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    /// <summary>
    /// Maps an <see cref="IReadOnlyList{String}"/> property to a MySQL JSON column,
    /// serialising with <c>System.Text.Json</c>.
    /// </summary>
    internal static PropertyBuilder<IReadOnlyList<string>> AsJsonList(
        this PropertyBuilder<IReadOnlyList<string>> builder)
    {
        builder.HasConversion(
            v => JsonSerializer.Serialize(v, JsonOpts),
            v => (IReadOnlyList<string>)(
                JsonSerializer.Deserialize<List<string>>(v, JsonOpts) ?? new List<string>()));
        builder.HasColumnType("json");
        return builder;
    }
}
