namespace CommandRouter
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Routing;

    internal partial class DefaultCommandSelector : ICommandSelector
    {
        public bool TrySelectCommand(string str, ICommandTable commandTable,
            [NotNullWhen(true)] out CommandMethod? method,
            [NotNullWhen(true)] out object[]? extra)
        {
            var tokens = Tokenizer.Tokenize(str);
            var index = tokens.Count;

            while (index > -1)
            {
                //get the command string
                string cmd;
                if (index == 0)
                {
                    //check the root/default route
                    cmd = "";
                }
                else
                {
                    cmd = string.Join(" ", tokens.Take(index));
                }

                if (commandTable.TryGetValue(cmd, out method))
                {
                    extra = tokens.Skip(index).ToArray();
                    return true;
                }

                --index;
            }

            method = null;
            extra = null;
            return false;
        }

        private static partial class Tokenizer
        {
            public static List<string> Tokenize(string command)
            {
                command = command.Trim();
                var tokens = new List<string>();

                var index = 0;

                while (index < command.Length)
                {
                    switch (command[index])
                    {
                        case '\"':
                            ++index;
                            tokens.Add(GetStringInQuotation(command, ref index));
                            break;
                        default:
                            tokens.Add(GetNextWord(command, ref index));
                            break;
                    }

                    IgnoreWhiteSpaces(command, ref index);
                }

                return tokens;
            }

            private static string GetNextWord(string command, ref int index)
            {
                var match = TokenizerRegex().Match(command[index..]);

                var delimitersIndex = match.Success ? match.Index + index : -1;

                string retVal;

                if (delimitersIndex == -1)
                {
                    retVal = command[index..];
                    index = command.Length;
                    return retVal;
                }

                retVal = command.Substring(index, delimitersIndex - index);

                index = delimitersIndex;

                return retVal;
            }

            private static string GetStringInQuotation(string command, ref int index)
            {
                var words = new List<string>();
                IgnoreWhiteSpaces(command, ref index);

                while (index < command.Length)
                {
                    words.Add(GetNextWord(command, ref index));
                    IgnoreWhiteSpaces(command, ref index);

                    if (index < command.Length && command[index] == '\"')
                        break;
                }

                ++index;
                return string.Join(" ", words);
            }

            private static void IgnoreWhiteSpaces(string command, ref int index)
            {
                while (index != command.Length)
                {
                    if (command[index] != ' ')
                        break;

                    ++index;
                }
            }

            [GeneratedRegex("( |\")")]
            private static partial Regex TokenizerRegex();
        }
    }
}
