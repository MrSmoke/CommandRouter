namespace CommandRouter.Routing;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Activation;
using Attributes;
using Commands;
using Exceptions;
using Results;
using ParameterInfo = Binding.ParameterInfo;

public class CommandTable(ICommandActivator commandActivator, ICommandTokenizer commandTokenizer) : ICommandTable
{
    private readonly Dictionary<string, CommandMethod> _methodTable = new();

    public CommandTable(ICommandActivator commandActivator, ICommandTokenizer commandTokenizer,
        ApplicationManager applicationManager) : this(commandActivator, commandTokenizer)
    {
        LoadCommands(applicationManager);
    }

    /// <summary>
    /// Adds the commands declared on <typeparamref name="T"/>. Not thread-safe, so only call this at startup before
    /// any commands run
    /// </summary>
    internal void RegisterCommands<T>() where T : Command
    {
        LoadCommands(typeof(T));
    }

    public IEnumerator<KeyValuePair<string, CommandMethod>> GetEnumerator()
    {
        return _methodTable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _methodTable.Count;

    public bool ContainsKey(string key)
    {
        return _methodTable.ContainsKey(key);
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out CommandMethod? value)
    {
        return _methodTable.TryGetValue(key, out value);
    }

    public CommandMethod this[string key] => _methodTable[key];

    public IEnumerable<string> Keys => _methodTable.Keys;
    public IEnumerable<CommandMethod> Values => _methodTable.Values;

    private void LoadCommands(ApplicationManager applicationManager)
    {
        foreach (var type in applicationManager.GetCommandFeature().Commands)
            LoadCommands(type.AsType());
    }

    private void LoadCommands(Type type)
    {
        var typeInfo = type.GetTypeInfo();

        var methods = typeInfo.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var prefixes = typeInfo.GetCustomAttributes<CommandPrefixAttribute>().ToArray();
        var nullabilityContext = new NullabilityInfoContext();

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes<CommandAttribute>().ToArray();
            if (attributes.Length == 0)
                continue;

            // Shared by every command string on this method
            var resultAdapter = CreateResultAdapter(method.ReturnType);
            var parameters = CreateParameters(method, nullabilityContext);
            var action = resultAdapter == null ? null : CreateAction(type, method, resultAdapter);

            foreach (var attr in attributes)
            {
                foreach (var cmdStr in GetCommandStrings(attr, prefixes))
                {
                    if (action == null)
                        throw new UnsupportedReturnTypeException(cmdStr, method.ReturnType);

                    AddMethod(new CommandMethod(cmdStr, action, parameters));
                }
            }
        }
    }

    private void AddMethod(CommandMethod method)
    {
        if (!_methodTable.TryAdd(method.Command, method))
            throw new CommandRouterException($"Command '{method.Command}' has already been registered");
    }

    /// <summary>
    /// Splits <paramref name="command"/> with the same tokenizer as the input, so names and input always match
    /// </summary>
    private string ToCommandName(string command)
    {
        if (!commandTokenizer.TryTokenize(command, out var tokenized, out _))
            throw new CommandRouterException($"Command '{command}' has an unterminated quote");

        // Quoted tokens are always arguments, so a name containing one could never match
        foreach (var token in tokenized.Tokens)
        {
            if (token.IsQuoted)
                throw new CommandRouterException($"Command '{command}' can't contain quotes");
        }

        return tokenized.GetCommandName(tokenized.Tokens.Count);
    }

    private static ParameterInfo[] CreateParameters(MethodInfo method, NullabilityInfoContext nullabilityContext)
    {
        var reflectionParameters = method.GetParameters();
        var parameters = new ParameterInfo[reflectionParameters.Length];

        for (var i = 0; i < reflectionParameters.Length; i++)
        {
            var p = reflectionParameters[i];
            var nullableInfo = nullabilityContext.Create(p);

            parameters[i] = new ParameterInfo
            {
                Name = p.Name,
                Type = p.ParameterType,
                DefaultValue = p.DefaultValue,
                HasDefaultValue = p.HasDefaultValue,
                IsNullable = nullableInfo.WriteState
                    is NullabilityState.Nullable
                    or NullabilityState.Unknown
            };
        }

        return parameters;
    }

    private Func<object?[], CommandContext, ValueTask<ICommandResult>> CreateAction(Type classType,
        MethodInfo method, Func<object?, ValueTask<ICommandResult>> resultAdapter)
    {
        var invoker = MethodInvoker.Create(method);

        return async (args, context) =>
        {
            var scope = commandActivator.CreateScope();

            // Disposed after async work completes
            await using var _ = scope.ConfigureAwait(false);

            if (scope.CommandActivator.Create(classType) is not Command command)
            {
                throw new CommandRouterException(
                    $"Unable to resolve command class '{classType.FullName}'. " +
                    "The activator must return an instance that derives from Command.");
            }

            command.Context = context;

            var result = invoker.Invoke(command, args.AsSpan());

            return await resultAdapter(result).ConfigureAwait(false);
        };
    }

    private static Func<object?, ValueTask<ICommandResult>>? CreateResultAdapter(Type returnType)
    {
        if (returnType == typeof(void))
            return static _ => new ValueTask<ICommandResult>(EmptyResult.Empty);

        if (typeof(ICommandResult).IsAssignableFrom(returnType))
            return static result => new ValueTask<ICommandResult>((ICommandResult?) result ?? EmptyResult.Empty);

        if (returnType == typeof(Task))
        {
            return static async result =>
            {
                await ((Task) result!).ConfigureAwait(false);
                return EmptyResult.Empty;
            };
        }

        if (returnType == typeof(ValueTask))
        {
            return static async result =>
            {
                await ((ValueTask) result!).ConfigureAwait(false);
                return EmptyResult.Empty;
            };
        }

        if (returnType.IsGenericType)
        {
            var definition = returnType.GetGenericTypeDefinition();
            var resultType = returnType.GetGenericArguments()[0];

            if (resultType.IsValueType || !typeof(ICommandResult).IsAssignableFrom(resultType))
                return null;

            if (definition == typeof(Task<>))
                return CreateGenericAdapter(nameof(AwaitTask), resultType);

            if (definition == typeof(ValueTask<>))
                return CreateGenericAdapter(nameof(AwaitValueTask), resultType);
        }

        return null;
    }

    private static Func<object?, ValueTask<ICommandResult>> CreateGenericAdapter(string methodName, Type resultType)
    {
        return typeof(CommandTable)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType)
            .CreateDelegate<Func<object?, ValueTask<ICommandResult>>>();
    }

    private static async ValueTask<ICommandResult> AwaitTask<T>(object? task) where T : class, ICommandResult
    {
        ICommandResult? result = await ((Task<T>) task!).ConfigureAwait(false);
        return result ?? EmptyResult.Empty;
    }

    private static async ValueTask<ICommandResult> AwaitValueTask<T>(object? task) where T : class, ICommandResult
    {
        ICommandResult? result = await ((ValueTask<T>) task!).ConfigureAwait(false);
        return result ?? EmptyResult.Empty;
    }

    private IEnumerable<string> GetCommandStrings(CommandAttribute attribute, CommandPrefixAttribute[] prefixes)
    {
        var command = attribute.Command ?? "";

        if (prefixes.Length == 0)
            return [ToCommandName(command)];

        return prefixes.Select(prefix => ToCommandName(prefix.Command + " " + command));
    }
}
