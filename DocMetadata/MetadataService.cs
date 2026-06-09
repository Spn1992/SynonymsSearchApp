using System.IO;

namespace DocMetadata
{
    public class MetadataService : IMetadataService
    {
        public string GetStandardizedExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            string extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                return string.Empty;

            // Ensure the extension has a leading dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            return extension.ToLowerInvariant();
        }
    }
}
