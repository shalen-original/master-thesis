using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

/*
    Custom JSON converters used when deserializing the API commands.
*/

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var array = JArray.Load(reader);      
        return new Vector3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());
    }

    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        writer.WriteValue(value.x);
        writer.WriteValue(value.y);
        writer.WriteValue(value.z);
        writer.WriteEndArray();
    }
}

public class Vector3NullableJsonConverter : JsonConverter<Vector3?>
{
    public override Vector3? ReadJson(JsonReader reader, Type objectType, Vector3? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.Undefined)
            return null;

        var array = JArray.Load(reader);
        return new Vector3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());
    }

    public override void WriteJson(JsonWriter writer, Vector3? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        writer.WriteValue(value.Value.x);
        writer.WriteValue(value.Value.y);
        writer.WriteValue(value.Value.z);
        writer.WriteEndArray();
    }
}

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var array = JArray.Load(reader);
        return new Color(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>(), array[3].Value<float>());
    }

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        writer.WriteValue(value.r);
        writer.WriteValue(value.g);
        writer.WriteValue(value.b);
        writer.WriteValue(value.a);
        writer.WriteEndArray();
    }
}