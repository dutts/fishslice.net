using System;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace fishslice.Services
{
    public interface IRemoteWebDriverFactory
    {
        IWebDriver GetRemoteWebDriver();
    }
    
    public class RemoteWebDriverFactory : IRemoteWebDriverFactory
    {
        private readonly ICapabilities _desiredCapabilities;

        public RemoteWebDriverFactory(ILogger<RemoteWebDriverFactory> logger)
        {
            var options = new ChromeOptions();
            options.AddArgument("start-maximized"); // https://stackoverflow.com/a/26283818/1689770
            options.AddArgument("enable-automation"); // https://stackoverflow.com/a/43840128/1689770
            options.AddArgument("--headless"); // only if you are ACTUALLY running headless
            options.AddArgument("--no-sandbox"); //https://stackoverflow.com/a/50725918/1689770
            options.AddArgument("--disable-infobars"); //https://stackoverflow.com/a/43840128/1689770
            options.AddArgument("--disable-dev-shm-usage"); //https://stackoverflow.com/a/50725918/1689770
            options.AddArgument("--disable-browser-side-navigation"); //https://stackoverflow.com/a/49123152/1689770
            options.AddArgument("--disable-gpu"); //https://stackoverflow.com/questions/51959986/how-to-solve-selenium-chromedriver-timed-out-receiving-message-from-renderer-exc
            _desiredCapabilities = options.ToCapabilities();
        }
        
        public IWebDriver GetRemoteWebDriver()
        {
            return new RemoteWebDriver(new Uri("http://selenium-hub:4444/wd/hub"), _desiredCapabilities, TimeSpan.FromSeconds(600));
        }
    }
}