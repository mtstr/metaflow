using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metaflow
{
    public class JsonConverterForMaybeOfT<T> : JsonConverter<Maybe<T>> where T : struct
    {
        public override Maybe<T> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T? t = JsonSerializer.Deserialize<T?>(ref reader, options);

            return t.Maybe();
        }

        public override void Write(
            Utf8JsonWriter writer, Maybe<T> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Match(new object(), t => t), options);
        }
    }
}
