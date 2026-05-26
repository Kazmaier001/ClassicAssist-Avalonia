using System;
using Avalonia.Media;
using Newtonsoft.Json;

namespace ClassicAssist.Misc
{
    /// <summary>
    ///   WPF's System.Windows.Media.Color round-trips through Newtonsoft via its TypeConverter
    ///   (string &lt;-&gt; "#AARRGGBB"). Avalonia.Media.Color has no such converter, so existing
    ///   profile JSON written by the WPF build fails to deserialize ("Could not cast or convert
    ///   from System.String to Avalonia.Media.Color"). Register this converter globally before
    ///   any profile load so existing user settings.json files still work.
    /// </summary>
    public class AvaloniaColorJsonConverter : JsonConverter<Color>
    {
        public override void WriteJson( JsonWriter writer, Color value, JsonSerializer serializer )
        {
            writer.WriteValue( value.ToString() );
        }

        public override Color ReadJson( JsonReader reader, Type objectType, Color existingValue,
            bool hasExistingValue, JsonSerializer serializer )
        {
            if ( reader.TokenType == JsonToken.Null )
            {
                return default;
            }

            if ( reader.TokenType == JsonToken.String )
            {
                string text = (string) reader.Value;
                return string.IsNullOrEmpty( text ) ? default : Color.Parse( text );
            }

            throw new JsonSerializationException(
                $"Unexpected token {reader.TokenType} when deserializing Avalonia.Media.Color." );
        }
    }
}
