using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sshanty.Serialisers
{
    public class JsonOptionalStringToListConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                    return new List<string> { reader.GetString() };
            }
            else
            {
                var value = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return value;
                    if (reader.TokenType == JsonTokenType.String)
                        value.Add(reader.GetString());
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            if (value.Count == 1)
                writer.WriteStringValue(value[0]);
            else
            {
                writer.WriteStartArray();
                foreach (var item in value)
                    writer.WriteStringValue(item);
                writer.WriteEndArray();
            }
        }
    }
}