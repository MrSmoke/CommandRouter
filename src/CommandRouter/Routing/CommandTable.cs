namespace CommandRouter.Routing
{
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
    using Results;
    using ParameterInfo = Binding.ParameterInfo;

    public class CommandTable : ICommandTable
    {
        private readonly ICommandActivator _commandActivator;
        private readonly Dictionary<string, CommandMethod> _methodTable = new();

        public CommandTable(ICommandActivator commandActivator)
        {
            _commandActivator = commandActivator;
        }

        internal CommandTable(ICommandActivator commandActivator, ApplicationManager applicationManager) :
            this(commandActivator)
        {
            LoadCommands(applicationManager);
        }

        public void AddCommand(string command, Func<object?[], CommandContext, ICommandResult> action)
        {
            _methodTable.Add(command, new CommandMethod(command, action, []));
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

        internal void LoadCommands(ApplicationManager applicationManager)
        {
            var commandFeature = new CommandFeature();
            DefaultCommandFeatureProvider.PopulateFeature(applicationManager.Assemblies, commandFeature);

            foreach(var type in commandFeature.Commands)
                LoadCommands(type.AsType());
        }

        internal void LoadCommands(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            var methods = typeInfo.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            var prefixes = typeInfo.GetCustomAttributes<CommandPrefixAttribute>().ToArray();

            foreach (var method in methods)
            {
                foreach (var attr in method.GetCustomAttributes<CommandAttribute>())
                {
                    var action = MethodInvoker(type, method);

                    foreach (var cmdStr in GetCommandStrings(attr, prefixes))
                    {
                        if (_methodTable.ContainsKey(cmdStr))
                            throw new Exception($"Command '{cmdStr}' has already been registered");

                        _methodTable.Add(cmdStr, new CommandMethod(
                            command: cmdStr,
                            action: action,
                            parameters: method.GetParameters().Select(p => new ParameterInfo
                            {
                                Name = p.Name,
                                Type = p.ParameterType,
                                DefaultValue = p.DefaultValue,
                                HasDefaultValue = p.HasDefaultValue
                            }).ToArray()
                        ));
                    }
                }
            }
        }

        private Func<object?[], CommandContext, object?> MethodInvoker(Type classType, MethodBase methodInfo)
        {
            return (objs, context) =>
            {
                var scope = _commandActivator.CreateScope();
                object? result = null;

                try
                {
                    if (scope.CommandActivator.Create(classType) is not Command command)
                        throw new Exception("Oh o");

                    command.Context = context;

                    result = methodInfo.Invoke(command, objs);

                    return result;
                }
                finally
                {
                    // Dispose of our scope
                    if (result is Task task)
                        task.ContinueWith(_ => { scope.Dispose(); });
                    else
                        scope.Dispose();
                }
            };
        }

        private static IEnumerable<string> GetCommandStrings(CommandAttribute attribute, CommandPrefixAttribute[] prefixes)
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
            var cmd = attribute?.Command;
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
}
