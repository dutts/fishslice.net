# fishslice

fishslice is a simple web scraper api, built on .NET, docker and Selenium.

## Schema

```json
{
  "openapi": "3.0.1",
  "info": {
    "title": "fishslice",
    "version": "v1"
  },
  "paths": {
    "/requestUrl": {
      "post": {
        "tags": [
          "Scrape"
        ],
        "requestBody": {
          "content": {
            "application/json": {
              "schema": {
                "$ref": "#/components/schemas/UrlRequest"
              }
            },
            "text/json": {
              "schema": {
                "$ref": "#/components/schemas/UrlRequest"
              }
            },
            "application/*+json": {
              "schema": {
                "$ref": "#/components/schemas/UrlRequest"
              }
            }
          }
        },
        "responses": {
          "200": {
            "description": "Success"
          },
          "204": {
            "description": "Success"
          },
          "400": {
            "description": "Bad Request",
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/ProblemDetails"
                }
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "PreScrapeAction": {
        "type": "object",
        "properties": {
          "type": {
            "$ref": "#/components/schemas/PreScrapeActionType"
          }
        },
        "additionalProperties": false
      },
      "PreScrapeActionType": {
        "enum": [
          "WaitForElement",
          "Sleep",
          "SetInputElement",
          "ClickButton"
        ],
        "type": "string"
      },
      "ProblemDetails": {
        "type": "object",
        "properties": {
          "type": {
            "type": "string",
            "nullable": true
          },
          "title": {
            "type": "string",
            "nullable": true
          },
          "status": {
            "type": "integer",
            "format": "int32",
            "nullable": true
          },
          "detail": {
            "type": "string",
            "nullable": true
          },
          "instance": {
            "type": "string",
            "nullable": true
          }
        },
        "additionalProperties": { }
      },
      "ResourceType": {
        "enum": [
          "PageSource",
          "Screenshot"
        ],
        "type": "string"
      },
      "UrlRequest": {
        "type": "object",
        "properties": {
          "url": {
            "type": "string",
            "format": "uri",
            "nullable": true
          },
          "resourceType": {
            "$ref": "#/components/schemas/ResourceType"
          },
          "preScrapeActions": {
            "type": "array",
            "items": {
              "$ref": "#/components/schemas/PreScrapeAction"
            },
            "nullable": true
          }
        },
        "additionalProperties": false
      }
    }
  }
}
```