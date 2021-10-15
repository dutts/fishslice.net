using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using fishslice.Converters;

namespace fishslice.tests
{
    public class JsonTests
    {
        [Test]
        public void Test()
        {
            var jsonString =
                @"{
                    ""Url"": ""http://www.google.com"",
                    ""ResourceType"": ""PageSource"",
                    ""PreScrapeActions"": [
                    {
                        ""Type"": ""Sleep"",
                        ""Milliseconds"": 10000
                    },
                    {
                      ""Type"": ""WaitForElement"",
                      ""SelectorXPath"": ""/html/body/div[1]/div[3]/form/div[1]/div[1]/div[1]/div/div[2]/input""
                    },
                    {
                      ""Type"": ""SetInputElement"",
                      ""SelectorXPath"": ""/html/body/div[1]/div[3]/form/div[1]/div[1]/div[1]/div/div[2]/input"",
                      ""Value"": ""Richard""                    
                    },
                    {
                      ""Type"": ""WaitForElement"",
                      ""SelectorXPath"": ""/html/body/div[1]/div[3]/form/div[1]/div[1]/div[3]/center/input[1]""
                    },
                    {
                      ""Type"": ""ClickButton"",
                      ""SelectorXPath"": ""/html/body/div[1]/div[3]/form/div[1]/div[1]/div[3]/center/input[1]""
                    }]
                }";

            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            jsonSerializerOptions.Converters.Add(new PreScrapeActionConverter());
            
            var foo = JsonSerializer.Deserialize<UrlRequest>(jsonString, jsonSerializerOptions);
        }
    }
}