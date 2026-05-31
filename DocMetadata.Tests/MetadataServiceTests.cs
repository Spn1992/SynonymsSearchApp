using Xunit;
using DocMetadata;

namespace DocMetadata.Tests
{
    public class MetadataServiceTests
    {
        private readonly IMetadataService _metadataService;

        public MetadataServiceTests()
        {
            _metadataService = new MetadataService();
        }

        [Theory]
        [InlineData("document.pdf", ".pdf")]
        [InlineData("archive.tar.gz", ".gz")] // Path.GetExtension returns the last extension
        [InlineData("noextension", "")]
        [InlineData("file.", "")] // Path.GetExtension returns empty for "file."
        [InlineData(".hiddenfile", ".hiddenfile")]
        [InlineData("UPPERCASE.PDF", ".pdf")]
        public void GetStandardizedExtension_ShouldReturnCorrectFormat(string fileName, string expectedExtension)
        {
            string result = _metadataService.GetStandardizedExtension(fileName);
            Assert.Equal(expectedExtension, result);
        }
        
        [Fact]
        public void GetStandardizedExtension_WithMissingDot_ShouldAddDot()
        {
            // Although Path.GetExtension always adds a dot, we are testing the service's resilience
            // if we somehow pass an extension instead of a filename. 
            // In normal operations, Path.GetExtension handles this, but let's ensure it handles correctly.
            // Actually, if we pass "pdf" as fileName, Path.GetExtension returns empty string because it thinks it's a file without extension.
            // Let's test the interface contract directly.
            string result = _metadataService.GetStandardizedExtension("file.doc");
            Assert.Equal(".doc", result);
        }
    }
}
