using System.IO;

namespace MatchingEngine.Utils.Extensions
{
    public static class StreamExtension
    {
        public static byte[] ToBytes(this Stream src)
        {

            var memoryStream = src as MemoryStream;

            if (memoryStream != null)
                return memoryStream.ToArray();


            src.Position = 0;
            var result = new MemoryStream();

            src.CopyTo(result);
            return result.ToArray();
        }
    }
}