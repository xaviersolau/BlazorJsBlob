# BlazorJsBlob

This project provides a convenient and efficient way to use JavaScript Blob storage from your Blazor application
regardless of whether it's hosted on WebAssembly or Server-side.

Don't hesitate to post issues, pull requests on the project or to fork and improve the project.

## Project dashboard

[![Build - CI](https://github.com/xaviersolau/BlazorJsBlob/actions/workflows/build-ci.yml/badge.svg)](https://github.com/xaviersolau/BlazorJsBlob/actions/workflows/build-ci.yml)
[![Coverage Status](https://coveralls.io/repos/github/xaviersolau/BlazorJsBlob/badge.svg?branch=main)](https://coveralls.io/github/xaviersolau/BlazorJsBlob?branch=main)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

| Package                     | Nuget.org | Pre-release |
|-----------------------------|-----------|-----------|
|**SoloX.BlazorJsBlob**       |[![NuGet Beta](https://img.shields.io/nuget/v/SoloX.BlazorJsBlob.svg)](https://www.nuget.org/packages/SoloX.BlazorJsBlob)|[![NuGet Beta](https://img.shields.io/nuget/vpre/SoloX.BlazorJsBlob.svg)](https://www.nuget.org/packages/SoloX.BlazorJsBlob)|

## License and credits

BlazorJsBlob project is written by Xavier Solau. It's licensed under the MIT license.

 * * *

## Installation

You can checkout this Github repository or you can use the NuGet packages:

**Install using the command line from the Package Manager:**
```bash
Install-Package SoloX.BlazorJsBlob -version 1.0.0-alpha.1
```

**Install using the .Net CLI:**
```bash
dotnet add package SoloX.BlazorJsBlob --version 1.0.0-alpha.1
```

**Install editing your project file (csproj):**
```xml
<PackageReference Include="SoloX.BlazorJsBlob" Version="1.0.0-alpha.1" />
```

## How to use it

Note that you can find code examples in this repository at this location: `src/examples`.

### Set up the dependency injection

A few lines of code are actually needed to setup the BlazorJsBlob services.
You just need to use the name space `SoloX.BlazorJsBlob` to get access to
the right extension methods and to add the services in your `ServiceCollection` :

* For Blazor WebAssembly:

Update your `Program.cs` file (in `Main` method if using .Net 5 way):

```csharp
// Add BlazorJsBlob services.
builder.Services.AddJsBlob();
```

You can find an example in the project repository in `src/examples/SoloX.BlazorJsBlob.Example.WebAssembly`
(or a .Net5 example `src/examples/SoloX.BlazorJsBlob.Example.Net5.WebAssembly`).

* For Blazor Server Side:

First add the `using SoloX.BlazorJsBlob` directives then
- .Net 5 way: update your `ConfigureServices` method in the `Startup.cs` file
```csharp
// Add BlazorJsBlob services.
services.AddJsBlob();
```

- .Net 6 way: update your `Program.cs` file
```csharp
// Add BlazorJsBlob services.
builder.Services.AddJsBlob();
```

You can find an example in the project repository in `src/examples/SoloX.BlazorJsBlob.Example.ServerSide`
(or a .Net5 example `src/examples/SoloX.BlazorJsBlob.Example.Net5.ServerSide`).

### Create a JavaScript Blob

First you need to inject the `IBlobService`. Once you have an instance of the service, you can call the `CreateBlobAsync` method with
the data stream you need to store.

```csharp
// Let's say that we get the data stream from a HTTP hosted file:
var stream = await HttpClient.GetStreamAsync(@"some_file.jpg");

// Create a JavaScript Blob in your browser
// (The created IBlob object implements IAsyncDisposable).
await using var blob = await BlobService.CreateBlobAsync(stream, "image/jpeg");
```

> Note that the Blob implements IAsyncDisposable interface so you need to properly dispose it once you don't need it any more.

### Use the JavaScript Blob

Once you have a Blob, you may need to use it to display the data in you Razor page. Let's say that in our case we want to use the
Blob data in a `embed` html element.

The Blob object provides two useful property:
* `Uri` : The JavaScript Blob URL that you can use as source in your HTML elements like `embed` or `img` for example;
* `Type` : The media type the Blob have been created with;
 
```html
<div style="margin:10px">
    <embed src="@blob.Uri" width="500" height="500" type="@blob.Type">
</div>

@code{
    // with blob being a field of the page.
    private IBlob blob = ...;
}
```

### Save the JavaScript Blob as a file

In the case where you need to save the data stored in your Blob, here is a really easy way: you can just use the `IBlobService`
and call the method `SaveAsFileAsync`.

```csharp
// Let's say that we have a Blob created.
await using var blob = await BlobService.CreateBlobAsync(/*...*/);

// Nothing more to do than calling the SaveAsFileAsync method.
await BlobService.SaveAsFileAsync(blob, "some_file_name.jpg");
```
