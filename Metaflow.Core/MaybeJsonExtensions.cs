using System.Text.Json;

namespace Metaflow
{
    public static class MaybeJsonExtensions
    {
        public static JsonSerializerOptions AddMaybeConverter(this JsonSerializerOptions options)
        {
            options.Converters.Add(new JsonConverterFactoryForMaybeOfT());
            return options;
        }
    }
}
