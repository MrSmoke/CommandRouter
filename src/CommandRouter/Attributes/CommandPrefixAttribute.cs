namespace CommandRouter.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CommandPrefixAttribute : Attribute
    {
        public CommandPrefixAttribute(string command)
        {
            Command = command;
        }

        public string Command { get; set; }
    }
}
