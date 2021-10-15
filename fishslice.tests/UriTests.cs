using System;
using System.Text.Json;
using NUnit.Framework;

namespace fishslice.tests
{
    public class UriTests
    {
        [TestCase("www.google.com")]
        [TestCase("google.com")]
        [TestCase("google")]
        public void TestUriFormatExceptions(string uriString)
        {
            Assert.Throws<UriFormatException>(() =>
            {
                var _ = new Uri(uriString);
            });
        }

        record TestType(Uri Uri);
        
        [TestCase("http://www.google.com", false)]
        [TestCase("www.google.com", true)]
        [TestCase("google.com", true)]
        [TestCase("google", true)]
        public void TestUriFormatExceptionFromJson(string uriString, bool expectThrows)
        {
            var json = $"{{\"Uri\" : \"{uriString}\"}}";

            var testType = JsonSerializer.Deserialize<TestType>(json);
            
            Assert.DoesNotThrow(() =>
            {
                var _ = testType.Uri;
            });

            try
            {
                var __ = testType.Uri.AbsoluteUri;
            }
            catch (Exception e)
            {
                if (expectThrows) Assert.AreEqual(e.GetType(), typeof(InvalidOperationException));
            }
        }
    }
}