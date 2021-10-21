library(httr)
library(jsonlite)

requestParameters <- list( 
  Url = "https://duckduckgo.com",
  ResourceType = "PageSource",
  PreScrapeActions = list(
    list(
      Type = "Sleep",
      Milliseconds = 1000
    ),
    list(
      Type = "WaitForElement",
      SelectorXPath = "//*[@id=\"search_form_input_homepage\"]"
    ),
    list(
      Type = "SetInputElement",
      SelectorXPath = "//*[@id=\"search_form_input_homepage\"]",
      Value = "Awesome people named Richard",
      WaitForMilliseconds = 10000
    ),
    list(
      Type = "WaitForElement",
      SelectorXPath = "//*[@id=\"search_button_homepage\"]"
    ),
    list(
      Type = "ClickButton",
      SelectorXPath = "//*[@id=\"search_button_homepage\"]"
    )
  )
)

requestBody <- toJSON(requestParameters, pretty=TRUE, auto_unbox=TRUE)
postResponse <- POST("http://localhost:8888/requestUrl", body = requestBody, content_type_json(), accept_json())
responseContent <- content(postResponse, "text")
responseJson <- fromJSON(responseContent)
scrapedPage <- responseJson$ResultString

cat(scrapedPage)