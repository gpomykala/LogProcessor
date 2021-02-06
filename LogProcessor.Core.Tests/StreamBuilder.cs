using LogProcessor.Common;
using System.IO;
using System.Text;

namespace LogProcessor.Core.Tests
{
    internal class StreamBuilder
    {
        StringBuilder sBuilder = new StringBuilder();
        internal StreamBuilder AppendText(string text)
        {
            sBuilder.AppendLine(text);
            return this;
        }
        internal StreamBuilder AppendEvent(LogEvent evt)
        {
            var text = ObjectSerializer.Serialize(evt);
            sBuilder.AppendLine(text);
            return this;
        }

        internal Stream Build()
        {
            return GenerateStreamFromString(sBuilder.ToString());
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
