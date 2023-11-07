using System.Reflection;
// ReSharper disable AssignNullToNotNullAttribute

namespace Xieyi.DistributedLock.Helper
{
    internal static class EmbeddedResourceLoader
    {
        internal static string GetEmbeddedResource(string name)
        {
            var assembly = typeof(EmbeddedResourceLoader).GetTypeInfo().Assembly;

            using (var stream = assembly.GetManifestResourceStream(name))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}