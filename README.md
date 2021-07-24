# scrapy

scrapy is a simple web scraper, built on .NET, docker and Selenium.

## Installation

TODO

## Usage

# POST
[/request](#post-request) <br/>


### POST /request
Requests a URI to scrape

**Parameters**

|          Name | Required |  Type   | Description                                                                                                                                                           |
| -------------:|:--------:|:-------:| --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
|     `uri`     | required | string  | The valid URI string for the website to scrape


**Response**

HTTP 202 if URI is valid and accepted to scrape.
HTTP 400 if URI is invalid or any other error has occured.
