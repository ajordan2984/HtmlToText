[DEPRECATED]

![Project frozen](https://img.shields.io/badge/status-frozen-blue.png) ![Project unmaintained](https://img.shields.io/badge/project-unmaintained-red.svg)

# HtmlToText

## Version
1.0

## Summary
A compact library written in C# to parse out all the text from news articles.

## Support
Text from the follow tags can be accessed:
* p
* div
* h1 - h6
* meta
    * og:site_name
    * og:url
    * og:title
    * og:description
    * og:image
    * og:image:alt
    * article:author
    * article:section
    * article:tag
    * article:published_time
    * article:modified_time
* script
    * application/ld+json

## Example

This is how you would make your request.

```cs
    HtmlParser hp = new HtmlParser();
    hp.ParseUrl(@"SAMPLE URL HERE");

    foreach(var item in hp.AllExceptions)
        Console.WriteLine(item);

    foreach(var item in hp.Paragraph)
        Console.WriteLine(item);
    
    foreach(var item in hp.Div)
        Console.WriteLine(item);
```
