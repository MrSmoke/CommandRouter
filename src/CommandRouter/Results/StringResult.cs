namespace CommandRouter.Results
{
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class StringResult : ICommandResult
    {
        private readonly string _content;
        private readonly Encoding _encoding;

        public StringResult(string content) : this(content, Encoding.UTF8)
        {
        }

        public StringResult(string content, Encoding encoding)
        {
            _content = content;
            _encoding = encoding;
        }

        public Task ExecuteAsync(Stream resultStream)
        {
            var bytes = _encoding.GetBytes(_content);
            return resultStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
