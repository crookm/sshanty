using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sshanty.Serialisers
{
    public class JsonOptionalIntToListConverter : JsonConverter<List<int>>
    {
        public override List<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                if (reader.TokenType == JsonTokenType.Number)
                    return new List<int> { reader.GetInt32() };
            }
            else
            {
                var value = new List<int>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return value;
                    if (reader.TokenType == JsonTokenType.Number)
                        value.Add(reader.GetInt32());
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
        {
            if (value.Count == 1)
                writer.WriteNumberValue(value[0]);
            else
            {
                writer.WriteStartArray();
                foreach (var item in value)
                    writer.WriteNumberValue(item);
                writer.WriteEndArray();
            }
        }
    }
}