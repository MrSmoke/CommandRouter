namespace CommandRouter.Routing
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Activation;
    using Attributes;
    using Commands;
    using Results;
    using ParameterInfo = Binding.ParameterInfo;

    public interface ICommandTable : IReadOnlyDictionary<string, CommandMethod>
    {
        void AddCommand(string command, Func<object[], CommandContext, ICommandResult> action);
    }

    public class CommandTable : ICommandTable
    {
        private readonly ICommandActivator _commandActivator;
        private readonly ApplicationManager _applicationManager;

        private readonly Dictionary<string, CommandMethod> _methodTable = new Dictionary<string, CommandMethod>();

        public CommandTable(ICommandActivator commandActivator, ApplicationManager applicationManager)
        {
            _commandActivator = commandActivator;
            _applicationManager = applicationManager;

            LoadCommands();
        }

        public void AddCommand(string command, Func<object[], CommandContext, ICommandResult> action)
        {
            _methodTable.Add(command, new CommandMethod
            {
                Command = command,
                Action = action,
                Parameters = new List<ParameterInfo>()
            });
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

        public bool TryGetValue(string key, out CommandMethod value)
        {
            return _methodTable.TryGetValue(key, out value);
        }

        public CommandMethod this[string key] => _methodTable[key];

        public IEnumerable<string> Keys => _methodTable.Keys;
        public IEnumerable<CommandMethod> Values => _methodTable.Values;

        internal void LoadCommands()
        {
            var commandFeature = new CommandFeature();
            _applicationManager.CommandFeatureProvider.PopulateFeature(_applicationManager.Assembiles, commandFeature);

            foreach(var type in commandFeature.Commands)
                LoadCommands(type.AsType());
        }

        internal void LoadCommands(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            var methods = typeInfo.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            var prefixes = typeInfo.GetCustomAttributes<CommandPrefixAttribute>().ToList();

            foreach (var method in methods)
            {
                foreach (var attr in method.GetCustomAttributes<CommandAttribute>())
                {
                    var action = MethodInvoker(type, method);

                    foreach (var cmdStr in GetCommandStrings(attr, prefixes))
                    {
                        if (_methodTable.ContainsKey(cmdStr))
                            throw new Exception($"Command '{cmdStr}' has already been registered");

                        _methodTable.Add(cmdStr, new CommandMethod
                        {
                            Command = cmdStr,
                            Action = action,
                            ReturnType = method.ReturnType,
                            Parameters = method.GetParameters().Select(p =>
                            new ParameterInfo
                            {
                                Name = p.Name,
                                Type = p.ParameterType,
                                DefaultValue = p.DefaultValue,
                                HasDefaultValue = p.HasDefaultValue
                            }).ToList()
                        });
                    }

                }
            }
        }

        private Func<object[], CommandContext, object> MethodInvoker(Type classType, MethodBase methodInfo)
        {
            return (objs, context) =>
            {
                var command = _commandActivator.Create(classType) as Command;

                if (command == null)
                    throw new Exception("Oh o");

                command.Context = context;

                return methodInfo.Invoke(command, objs);
            };
        }

        private static IEnumerable<string> GetCommandStrings(CommandAttribute attribute, IEnumerable<CommandPrefixAttribute> prefixes)
        {
            var cmds = new List<string>();

            //TODO: Cleanup
            if (!prefixes.Any())
            {
                var cmd = Parse(attribute, null);
                if (cmd != null)
                    cmds.Add(cmd.Trim());

                return cmds;
            }

            foreach (var prefix in prefixes)
            {
                var cmd = Parse(attribute, prefix);
                if (cmd != null)
                    cmds.Add(cmd.Trim());
            }

            return cmds;
        }

        private static string Parse(CommandAttribute attribute, CommandPrefixAttribute prefix)
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
