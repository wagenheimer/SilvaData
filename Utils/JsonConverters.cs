using Newtonsoft.Json;

namespace SilvaData.Utils
{
    // Trata string vazia como null ao desserializar int? (API retorna excluido: "")
    public class EmptyStringToNullableIntConverter : JsonConverter<int?>
    {
        public override int? ReadJson(JsonReader reader, Type objectType, int? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType == JsonToken.String)
            {
                var s = reader.Value?.ToString();
                if (string.IsNullOrEmpty(s)) return null;
                if (int.TryParse(s, out var parsed)) return parsed;
                return null;
            }
            if (reader.TokenType == JsonToken.Integer)
                return Convert.ToInt32(reader.Value);
            return null;
        }

        public override void WriteJson(JsonWriter writer, int? value, JsonSerializer serializer)
        {
            if (value.HasValue) writer.WriteValue(value.Value);
            else writer.WriteNull();
        }
    }
}
