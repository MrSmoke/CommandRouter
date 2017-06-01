namespace CommandRouter.Exceptions
{
    using System;

    public class CommandRouterException : Exception
    {
        public CommandRouterException(string message) : base(message)
        {
        }

        public CommandRouterException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}