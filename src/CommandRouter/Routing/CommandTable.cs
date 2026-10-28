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

public class CommandTable(ICommandActivator commandActivator) : ICommandTable, ICommandMatcher
{
    private readonly Dictionary<string, CommandMethod> _methodTable = new();
    private readonly TrieNode _root = new();

    public CommandTable(ICommandActivator commandActivator, ApplicationManager applicationManager) :
        this(commandActivator)
    {
        LoadCommands(applicationManager);
    }

    public void AddCommand(string command, Func<object?[], CommandContext, ICommandResult> action)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(action);

        AddMethod(new CommandMethod(command,
            (args, context) => new ValueTask<ICommandResult>(action(args, context)),
            []));
    }

    public void RegisterCommands<T>() where T : Command
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

        var methods = typeInfo.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
        var prefixes = typeInfo.GetCustomAttributes<CommandPrefixAttribute>().ToArray();
        var nullabilityContext = new NullabilityInfoContext();

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes<CommandAttribute>().ToArray();
            if (attributes.Length == 0)
                continue;

            // Everything reflection based is done once per method, not once per command string
            var resultAdapter = CreateResultAdapter(method.ReturnType);
            var parameters = CreateParameters(method, nullabilityContext);
            var action = resultAdapter == null ? null : CreateAction(type, method, resultAdapter);

            foreach (var attr in attributes)
            {
                foreach (var cmdStr in GetCommandStrings(attr, prefixes))
                {
                    if (action == null)
                    {
                        throw new CommandRouterException(
                            $"Command '{cmdStr}' ({type.FullName}.{method.Name}) has an unsupported return type '{method.ReturnType}'. " +
                            "Supported return types are void, ICommandResult, Task, Task<ICommandResult>, ValueTask and ValueTask<ICommandResult>, " +
                            "including types derived from ICommandResult.");
                    }

                    AddMethod(new CommandMethod(cmdStr, action, parameters));
                }
            }
        }
    }

    private void AddMethod(CommandMethod method)
    {
        var node = GetOrAddNode(method.Command);

        if (node.Method != null || !_methodTable.TryAdd(method.Command, method))
            throw new CommandRouterException($"Command '{method.Command}' has already been registered");

        node.Method = method;
    }

    private TrieNode GetOrAddNode(string command)
    {
        return command
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(_root, (current, word) => current.GetOrAddChild(word));
    }

    // Walks the command words once and remembers the deepest command found. No allocations
    public bool TryMatch(IReadOnlyList<CommandToken> tokens, out CommandMatch match)
    {
        var node = _root;
        var best = node.Method;
        var argumentStart = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            if (!node.TryGetChild(tokens[i].ValueSpan, out var next))
                break;

            node = next;

            if (node.Method == null)
                continue;

            best = node.Method;
            argumentStart = i + 1;
        }

        if (best == null)
        {
            match = default;
            return false;
        }

        match = new CommandMatch(best, argumentStart);
        return true;
    }

    private sealed class TrieNode
    {
        // Children are kept sorted by key (ordinal) so they can be found from a span without allocating
        private string[] _keys = [];
        private TrieNode[] _nodes = [];

        public CommandMethod? Method { get; set; }

        public bool TryGetChild(ReadOnlySpan<char> key, [NotNullWhen(true)] out TrieNode? child)
        {
            var low = 0;
            var high = _keys.Length - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var comparison = key.CompareTo(_keys[mid], StringComparison.Ordinal);

                if (comparison == 0)
                {
                    child = _nodes[mid];
                    return true;
                }

                if (comparison < 0)
                    high = mid - 1;
                else
                    low = mid + 1;
            }

            child = null;
            return false;
        }

        public TrieNode GetOrAddChild(string key)
        {
            if (TryGetChild(key, out var existing))
                return existing;

            var index = Array.BinarySearch(_keys, key, StringComparer.Ordinal);
            index = ~index;

            var keys = new string[_keys.Length + 1];
            var nodes = new TrieNode[_nodes.Length + 1];

            Array.Copy(_keys, 0, keys, 0, index);
            Array.Copy(_nodes, 0, nodes, 0, index);
            Array.Copy(_keys, index, keys, index + 1, _keys.Length - index);
            Array.Copy(_nodes, index, nodes, index + 1, _nodes.Length - index);

            var child = new TrieNode();
            keys[index] = key;
            nodes[index] = child;

            _keys = keys;
            _nodes = nodes;

            return child;
        }
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
            // The scope lives until the command (including any async work) has finished
            using var scope = commandActivator.CreateScope();

            if (scope.CommandActivator.Create(classType) is not Command command)
            {
                throw new CommandRouterException(
                    $"Unable to resolve command class '{classType.FullName}'. " +
                    "Make sure it is registered with the service provider and derives from Command.");
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

    private static IEnumerable<string> GetCommandStrings(CommandAttribute attribute,
        CommandPrefixAttribute[] prefixes)
    {
        //TODO: Cleanup
        if (prefixes.Length == 0)
        {
            var cmd = Parse(attribute, null);
            if (cmd != null)
                yield return cmd.Trim();
        }
        else
        {
            foreach (var prefix in prefixes)
            {
                var cmd = Parse(attribute, prefix);
                if (cmd != null)
                    yield return cmd.Trim();
            }
        }
    }

    private static string? Parse(CommandAttribute attribute, CommandPrefixAttribute? prefix)
    {
        var cmd = attribute.Command;
        var pre = prefix?.Command;

        if (cmd == null && pre == null)
            return null;

        if (cmd == null)
            return pre;

        if (pre == null)
            return cmd;

        return pre + " " + cmd;
    }
}
