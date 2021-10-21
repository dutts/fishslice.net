using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fishslice.Converters
{
    public class PreScrapeActionConverter : JsonConverter<PreScrapeAction>
    {
        public override PreScrapeAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // NB: This is currently the best way of performing polymorphic
            // deserialisation of types using System.Text.Json, but is very
            // brittle if our input schema changes.
            if (JsonDocument.TryParseValue(ref reader, out JsonDocument doc))
            {
                try
                {
                    if (doc.RootElement.TryGetProperty("Type", out var typeString) && 
                        Enum.TryParse<PreScrapeActionType>(typeString.GetString(), out var scrapeActionType))
                    {
                        switch (scrapeActionType)
                        {
                            case PreScrapeActionType.Sleep:
                                return JsonSerializer.Deserialize<Sleep>(doc.RootElement.GetRawText(), options);
                            case PreScrapeActionType.WaitForElement:
                                return JsonSerializer.Deserialize<WaitForElement>(doc.RootElement.GetRawText(), options);
                            case PreScrapeActionType.SetInputElement:
                                return JsonSerializer.Deserialize<SetInputElement>(doc.RootElement.GetRawText(), options);
                            case PreScrapeActionType.ClickButton:
                                return JsonSerializer.Deserialize<ClickButton>(doc.RootElement.GetRawText(), options);
                        }
                    }
                }
                finally
                {
                    doc.Dispose();
                }

            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, PreScrapeAction value, JsonSerializerOptions options)
        {
            switch (value.Type)
            {
                case PreScrapeActionType.Sleep:
                    JsonSerializer.Serialize(writer, (Sleep)value, typeof(Sleep), options);
                    break;
                case PreScrapeActionType.WaitForElement:
                    JsonSerializer.Serialize(writer, (WaitForElement)value, typeof(WaitForElement), options);
                    break;
                case PreScrapeActionType.SetInputElement:
                    JsonSerializer.Serialize(writer, (SetInputElement)value, typeof(SetInputElement), options);
                    break;
                case PreScrapeActionType.ClickButton:
                    JsonSerializer.Serialize(writer, (ClickButton)value, typeof(ClickButton), options);
                    break;
            }
        }
    }
}
