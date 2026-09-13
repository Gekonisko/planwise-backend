using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanWise.Common.Domain;

// PATCH endpoints need to tell "the client didn't mention this field" apart from "the client
// explicitly sent null to clear it". A plain `Guid?` collapses both onto null, which is why
// un-assigning a task from a sprint (or clearing an assignee/due date) used to be a silent no-op.
//
// System.Text.Json gives us the distinction for free: for an absent property the converter is never
// invoked, so the parameter keeps its `default` value (IsSet = false); for an explicit `null` the
// converter *is* invoked and records IsSet = true with a null Value.
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly record struct Optional<T>
{
    private Optional(T? value)
    {
        IsSet = true;
        Value = value;
    }

    /// <summary>Whether the property was present in the request body at all.</summary>
    public bool IsSet { get; }

    /// <summary>The supplied value. Only meaningful when <see cref="IsSet"/> is true.</summary>
    public T? Value { get; }

    public static Optional<T> Unset => default;

    public static Optional<T> Of(T? value) => new(value);

    public static implicit operator Optional<T>(T? value) => Of(value);

    /// <summary>Applies <paramref name="apply"/> only when the caller actually supplied the field.</summary>
    public bool TryGet(out T? value)
    {
        value = Value;
        return IsSet;
    }
}

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(OptionalJsonConverter<>).MakeGenericType(valueType))!;
    }

    private sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
    {
        // Reached only when the property is physically present in the JSON — including when its
        // value is `null`, which is exactly the case a plain nullable can't express.
        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Optional<T>.Of(JsonSerializer.Deserialize<T>(ref reader, options));

        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
        {
            if (!value.IsSet)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
