using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogProcessor.Core
{
    public static class ObjectSerializer
    {
        static JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public static string Serialize<T>(T input)
        {
            return JsonSerializer.Serialize(input, options);
        }

        public static T Deserialize<T>(string input)
        {
            return JsonSerializer.Deserialize<T>(input, options);
        }
    }
}
