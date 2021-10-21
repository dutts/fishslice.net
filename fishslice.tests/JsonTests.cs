using System;
using System.IO;
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
            var jsonString = File.ReadAllText("../../../JsonTests.Request.json");
            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            jsonSerializerOptions.Converters.Add(new PreScrapeActionConverter());
            
            var request = JsonSerializer.Deserialize<UrlRequest>(jsonString, jsonSerializerOptions);

            Assert.Multiple(() =>
            {
                Assert.NotNull(request);
                Assert.AreEqual(new Uri("https://duckduckgo.com"), request.Url);
                Assert.AreEqual(ResourceType.PageSource, request.ResourceType);
                Assert.AreEqual(5, request.PreScrapeActions.Count);

                var sleepAction = new Sleep(5000);
                Assert.AreEqual(sleepAction, request.PreScrapeActions[0]);

                var waitForElementAction1 =
                    new WaitForElement("//*[@id=\"search_form_input_homepage\"]");
                Assert.AreEqual(waitForElementAction1, request.PreScrapeActions[1]);

                var setInputElementAction =
                    new SetInputElement("//*[@id=\"search_form_input_homepage\"]",
                        "Awesome people named Richard",
                        10000);
                Assert.AreEqual(setInputElementAction, request.PreScrapeActions[2]);

                var waitForElementAction2 =
                    new WaitForElement("//*[@id=\"search_button_homepage\"]");
                Assert.AreEqual(waitForElementAction2, request.PreScrapeActions[3]);

                var clickButtonAction =
                    new ClickButton("//*[@id=\"search_button_homepage\"]");
                Assert.AreEqual(clickButtonAction, request.PreScrapeActions[4]);

            });
        }
    }
}