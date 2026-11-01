# Command Router
[![NuGet](https://img.shields.io/nuget/v/CommandRouter.svg)](https://www.nuget.org/packages/CommandRouter)

Routes text commands such as `user add bob` to methods, similar to routing in ASP.NET Core.


## Installation

```shell
dotnet add package CommandRouter
```

For ASP.NET Core integration:

```shell
dotnet add package CommandRouter.Integration.AspNetCore
```


## Getting started

Register the router:

```c#
using CommandRouter.Extensions;

builder.Services.AddCommandRouter();
```

Create a command:

```c#
using CommandRouter.Attributes;
using CommandRouter.Commands;
using CommandRouter.Results;

public class HelloCommand : Command
{
    [Command("hello")]
    public ICommandResult Hello(string name = "world")
    {
        return StringResult($"Hello {name}!");
    }
}
```

Run it with `ICommandRunner`:

```c#
var result = await commandRunner.RunAsync("hello bob");

await result.ExecuteAsync(stream);   // writes "Hello bob!"
```


## Registration

`AddCommandRouter()` scans every assembly that references CommandRouter, found from the entry assembly's
`.deps.json`. Without one, only the entry assembly is scanned. To start from a different assembly, pass it in:

```c#
services.AddCommandRouter(typeof(Program).Assembly);
```

`AddCommandRouter` can be called more than once. Each call adds its assemblies and options to the same router.

Commands are loaded on first use. To catch duplicate commands and unsupported return types at startup instead:

```c#
app.Services.ValidateCommandRouter();
```


## Commands

A class is a command class when it is:

- a top-level `public` class (not nested)
- not abstract or an open generic
- derived from `Command`
- named with a `Command` suffix, for example `UserCommand`

Every public instance method with a `[Command]` attribute is a command, including methods inherited from a base class.

Command classes don't need to be registered with dependency injection. Each run creates one in a new service scope,
with its constructor dependencies resolved from that scope. A registered command class uses its registration.
Otherwise the class needs a single public constructor, or one marked `[ActivatorUtilitiesConstructor]`.

```c#
public class UserCommand(IUserService users) : Command
{
    [Command("user add")]
    public async Task<ICommandResult> Add(string name)
    {
        await users.AddAsync(name, Context.CancellationToken);
        return StringResult($"Added {name}");
    }
}
```

### Names and prefixes

`[CommandPrefix]` is put in front of every command name in the class. Use it more than once for several prefixes:

```c#
[CommandPrefix("user")]
[CommandPrefix("u")]
public class UserCommand : Command
{
    [Command("add")]   // "user add" and "u add"
    public void Add(string name) { }

    [Command]          // "user" and "u"
    public void List() { }
}
```

- `[Command]` with no name is the same as `[Command("")]`: the prefix itself, or the root command without a prefix.
- The root command (`""`) runs when nothing else matches, with all the input as arguments.
- Whitespace around and between words doesn't matter, so `[CommandPrefix("user ")]` with `[Command(" add")]`
  registers `user add`.
- Names are case-sensitive and can't contain quotes.
- When several commands match, the longest wins. With `user` and `user add` registered, `user add bob` runs
  `user add` with the argument `bob`.

### Inherited commands

Commands on a base class are registered for each derived command class, with the derived class's prefixes and any
on the base class.

```c#
public abstract class StatusCommandBase : Command
{
    [Command("status")]
    public ICommandResult Status() => StringResult("OK");
}

[CommandPrefix("admin")]
public class AdminCommand : StatusCommandBase;   // "admin status"

[CommandPrefix("ops")]
public class OpsCommand : StatusCommandBase;     // "ops status"
```

Make shared base classes abstract. A concrete base class is a command class too, so its commands are registered
twice and fail as duplicates when the prefixes match. Hiding a command method with `new` and adding `[Command]` again
is also a duplicate.

### Return types

A command method can return:

- `void`, `Task` or `ValueTask`
- `ICommandResult`, `Task<ICommandResult>` or `ValueTask<ICommandResult>`, including types derived from
  `ICommandResult` such as `Task<StringResult>`

Returning `null` gives an empty result. Any other return type throws `UnsupportedReturnTypeException` when commands
are loaded.


## Results

`ICommandResult` writes its output to a stream:

```c#
public interface ICommandResult
{
    Task ExecuteAsync(Stream resultStream, CancellationToken cancellationToken = default);
}
```

`StringResult` writes a string, as UTF-8 by default. `Command` has `StringResult(...)` helpers for creating one.

A result is written after the command's service scope has been disposed, so it shouldn't use scoped services.


## Running commands

```c#
await commandRunner.RunAsync("user add bob");
await commandRunner.RunAsync("user add bob", cancellationToken);
await commandRunner.RunAsync("user add bob", contextItems);
await commandRunner.RunAsync("user add bob", contextItems, cancellationToken);
```

### Context

Commands can read the current run from `Context`:

- `Context.Arguments`: the arguments after the command name
- `Context.Items`: the `contextItems` passed to `RunAsync`
- `Context.CancellationToken`: the token passed to `RunAsync`

```c#
await commandRunner.RunAsync(command, new Dictionary<string, object>
{
    { "user", currentUser }
});

[Command("whoami")]
public ICommandResult WhoAmI()
{
    var user = (User)Context.Items["user"];
    return StringResult(user.Name);
}
```


## Arguments

Arguments are separated by whitespace and passed to the method's parameters in order. With `[Command("add")]` on
`Add(int a, int b)`, the command `add 1 2` calls `Add(1, 2)`.

- Use double quotes for an argument containing spaces: `say "hello world"`. Text inside quotes is kept exactly as
  written. Quotes can't be escaped.
- Quoted text is always an argument, never part of the command name. With `user` and `user add` registered,
  `user "add"` runs `user` with the argument `add`.
- Spaces, tabs and newlines all separate arguments, except inside quotes.
- A missing argument uses the parameter's default value, or `null` for a nullable parameter. Otherwise
  `ParameterBindingException` is thrown. In code without nullable reference types enabled, every reference type
  parameter counts as nullable.
- Extra arguments are ignored by the method, but are still in `Context.Arguments`.

### Supported types

Built in:

- `string`, `bool`, `char` and the numeric types
- `object`, which receives the argument as a string
- `DateTime`, `DateTimeOffset`, `TimeSpan` and `Guid`
- enums, by name (ignoring case) or by a defined number. Combined values are allowed for `[Flags]` enums.
- nullable versions of all of these

Values are parsed with the invariant culture.

### Custom converters

Implement `IPropertyConverter` to support other types:

```c#
// "move 3,4" binds Move(Point to) to new Point(3, 4)
public class PointConverter : IPropertyConverter
{
    public bool CanConvert(Type propertyType, string value) => propertyType == typeof(Point);

    public object Convert(Type propertyType, string value)
    {
        var parts = value.Split(',');
        return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
```

Add converters when registering the router or with the options pattern:

```c#
services.AddCommandRouter(options => options.PropertyConverters.Add(new PointConverter()));

services.Configure<CommandRouterOptions>(options => options.PropertyConverters.Add(new PointConverter()));
```

Custom converters are tried in order, before the built-in ones.


## Errors

Problems with the input or the commands are reported as `CommandRouterException` or one of its subclasses.
Exceptions thrown by a command itself are passed through unchanged.

| Exception | Thrown when |
|---|---|
| `CommandNotFoundException` | No command matches the input |
| `InvalidTokenException` | The input is missing a closing quote. `Position` is where the quoted argument starts |
| `ParameterBindingException` | An argument is missing or can't be converted. See `ParameterName`, `Value` and `InnerException` |
| `UnsupportedReturnTypeException` | A command method has an unsupported return type (when commands are loaded) |
| `CommandRouterException` | A command name is registered twice (when commands are loaded) |

Some errors come from .NET rather than the router:

- `OperationCanceledException` if the cancellation token is already cancelled when `RunAsync` is called
- `InvalidOperationException` from dependency injection when a command runs, if its class can't be created

```c#
try
{
    var result = await commandRunner.RunAsync(input);
}
catch (CommandNotFoundException)
{
    // unknown command
}
catch (ParameterBindingException e)
{
    // bad input, for example show usage for e.ParameterName
}
```


## Customisation

Register your own implementation **before** calling `AddCommandRouter`. Defaults are only added for services that
aren't already registered.

- `ICommandActivator`: creates command classes
- `ICommandTokeniser`: splits the input and command names into words
- `ICommandSelector`: picks the command for the tokens

### Custom command activation

For a dependency injection container that doesn't populate `IServiceProvider`, implement `ICommandActivator` and
`ICommandActivatorScope`. A scope is created for each command run and disposed when the command finishes.

```c#
public class SimpleInjectorCommandActivator(Container container) : ICommandActivator
{
    public object? Create(Type type) => container.GetInstance(type);

    public ICommandActivatorScope CreateScope() =>
        new SimpleInjectorScope(container, AsyncScopedLifestyle.BeginScope(container));
}

public sealed class SimpleInjectorScope(Container container, Scope scope) : ICommandActivatorScope, ICommandActivator
{
    public ICommandActivator CommandActivator => this;

    public object? Create(Type type) => scope.GetInstance(type);

    public ICommandActivatorScope CreateScope() =>
        new SimpleInjectorScope(container, AsyncScopedLifestyle.BeginScope(container));

    public void Dispose() => scope.Dispose();

    public ValueTask DisposeAsync() => scope.DisposeAsync();
}
```


## ASP.NET Core

`CommandRouterResult` writes a command result to the HTTP response:

```c#
[HttpPost("{command}")]
public async Task<IActionResult> Run(string command)
{
    var result = await _commandRunner.RunAsync(command, HttpContext.RequestAborted);

    Response.ContentType = "text/plain; charset=utf-8";
    return new CommandRouterResult(result);
}
```

`CommandRouterResult` doesn't set `Content-Type`, so set it on the response before returning the result.
`RequestAborted` is passed to the result, so `StringResult` stops writing if the client disconnects.
