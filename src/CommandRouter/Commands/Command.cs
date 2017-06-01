namespace CommandRouter.Commands
{
    using System.Text;
    using Results;

    public abstract class Command
    {
        public CommandContext Context { get; internal set; }

        protected ICommandResult StringResult(string str)
        {
            return new StringResult(str);
        }

        protected ICommandResult StringResult(string str, Encoding encoding)
        {
            return new StringResult(str, encoding);
        }
    }
}
