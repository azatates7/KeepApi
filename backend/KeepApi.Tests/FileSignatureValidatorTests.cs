using KeepApi.Services;

namespace KeepApi.Tests
{
    public class FileSignatureValidatorTests
    {
        [Fact]
        public void MatchesClaimedType_ValidPngBytes_ReturnsTrue()
        {
            byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

            var result = FileSignatureValidator.MatchesClaimedType(pngHeader, "image/png");

            Assert.True(result);
        }

        [Fact]
        public void MatchesClaimedType_ExeDisguisedAsPng_ReturnsFalse()
        {
            // "MZ" imzası (Windows exe) ama Content-Type olarak PNG iddia ediliyor
            byte[] exeHeader = [0x4D, 0x5A, 0x90, 0x00];

            var result = FileSignatureValidator.MatchesClaimedType(exeHeader, "image/png");

            Assert.False(result);
        }

        [Theory]
        [InlineData("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)]
        [InlineData("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }, true)]
        [InlineData("application/pdf", new byte[] { 0x00, 0x01, 0x02 }, false)]
        public void MatchesClaimedType_VariousTypes(string mimeType, byte[] bytes, bool expected)
        {
            var result = FileSignatureValidator.MatchesClaimedType(bytes, mimeType);

            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> MimeTypeCases()
        {
            yield return new object[] { "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true };
            yield return new object[] { "application/pdf", new byte[] { 0x00, 0x01, 0x02 }, false };
        }

        [Theory]
        [MemberData(nameof(MimeTypeCases))]
        public void MatchesClaimedType_VariousTypes2(string mimeType, byte[] bytes, bool expected)
        {
            Assert.Equal(expected, FileSignatureValidator.MatchesClaimedType(bytes, mimeType));
        }
    }
}