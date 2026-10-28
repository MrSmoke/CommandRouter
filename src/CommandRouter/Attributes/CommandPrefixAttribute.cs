namespace CommandRouter.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CommandPrefixAttribute(string command) : Attribute
{
    public string Command { get; set; } = command;
}