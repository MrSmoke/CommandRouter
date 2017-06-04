# Command Router
[![Build status](https://ci.appveyor.com/api/projects/status/w91d4ch2ekaffagd?svg=true)](https://ci.appveyor.com/project/MrSmoke/commandrouter)

Simple package to help route certain commands to functions, similar to routing in ASP.


## Installation

```PowerShell
PM> Install-Package CommandRouter
```

Or for integration with MVC

```PowerShell
PM> Install-Package CommandRouter.Integration.AspNetCore
```


Usage
---


### Setup

Add `services.AddCommandRouter()` to your `Startup.cs`.

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();

    services.AddCommandRouter();
}
```


Next create a command. All commands must extend the `Command` class.

```c#
public class TestCommand : Command
{
    [Command("hello")]
    public ICommandResult Test()
    {
        return StringResult("Hello world!");
    }
}
```

All commands must be decorated with the `Command` attribute and must also return either `ICommandResult` or `void` (`async` is also supported).

To run a command, call `RunAsync` on `ICommandRunner`

```c#
var result = await _commandRunner.RunAsync(command);
```


### ICommandResult

The command result is similar to the IActionResult. It has only one method on it 

```c#
Task ExecuteAsync(Stream resultStream)
```

`resultStream` will be written to when this function is called.


### Context items

Request level context items can also be passed in the second parameter of `RunAsync`

```c#
await _commandRunner.RunAsync(command, new Dictionary<string, object>
{
    {"content", "Hello world!" }
});
```

These will be available from within the `Command`

```c#
[Command("hello")]
public ICommandResult Test()
{
    return StringResult(Context.Items["content"].ToString());
}
```

### Parameters

By default, parameters are tokenized based on space. Eg `command parameter`. Parameters are passed based on the order in the Command action.

The below will return `The id is: 88` for the command `hello 88`

```c#
[Command("hello")]
public ICommandResult Test(int id)
{
    return StringResult("The id is: " + id);
}
```


### Custom parameter converters

You can add your own parameter converters to support more complex models by implementing `IPropertyConverter`

```c#
public class JsonConverter : IPropertyConverter
{
    public bool CanConvert(Type propertyType, object value)
    {
        throw new NotImplementedException();
    }

    public object Convert(Type propertyType, object value)
    {
        throw new NotImplementedException();
    }
}
```

Additional converters can be added to your CommandRouter in the `Startup.cs` file

```c#
services.AddCommandRouter(options =>
{
    options.PropertyConverters.Add(new JsonConverter());
});
```

## Examples
### MVC

```c#
[Route("{command}")]
public async Task<IActionResult> Index(string command)
{
    var result = await _commandRunner.RunAsync(command, new Dictionary<string, object>
    {
        {"content", "Hello world!" }
    });

    return new CommandRouterResult(result);
}
```