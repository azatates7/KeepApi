namespace KeepApi.Services
{
    /// <summary>
    /// Yüklenen dosyanın gerçek baytlarını (magic number / file signature) kontrol eder.
    /// Content-Type header'ı istemci tarafından belirlendiği ve kolayca sahtelenebildiği
    /// için (örn. bir .exe'yi "image/png" diye etiketleyip yüklemek), AttachmentSummaryService
    /// bu kontrolden GEÇMEYEN bir dosyayı asla LLM'e göndermemeli.
    /// </summary>
    public static class FileSignatureValidator
    {
        /// <summary>
        /// Dosya baytlarının, istemcinin bildirdiği Content-Type ile gerçekten uyuşup
        /// uyuşmadığını kontrol eder. text/plain için kesin bir imza olmadığından,
        /// bilinen ikili (binary) formatların imzalarını TAŞIMADIĞINI ve NUL byte
        /// içermediğini doğrulayarak dolaylı kontrol yapılır.
        /// </summary>
        public static bool MatchesClaimedType(byte[] bytes, string claimedMimeType)
        {
            if (bytes.Length == 0)
            {
                return false;
            }

            return claimedMimeType.ToLowerInvariant() switch
            {
                "image/jpeg" => StartsWith(bytes, 0xFF, 0xD8, 0xFF),
                "image/png" => StartsWith(bytes, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
                "image/gif" => IsGif(bytes),
                "image/webp" => IsWebp(bytes),
                "image/heic" or "image/heif" => IsHeif(bytes),
                "application/pdf" => StartsWith(bytes, 0x25, 0x50, 0x44, 0x46), // %PDF
                "text/plain" => LooksLikePlainText(bytes),
                _ => false // AllowedMimeTypes'ta olmayan bir tür buraya hiç gelmemeli
            };
        }

        private static bool StartsWith(byte[] bytes, params byte[] signature)
        {
            if (bytes.Length < signature.Length)
            {
                return false;
            }

            for (var i = 0; i < signature.Length; i++)
            {
                if (bytes[i] != signature[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsGif(byte[] bytes)
        {
            // "GIF87a" veya "GIF89a"
            return StartsWith(bytes, 0x47, 0x49, 0x46, 0x38, 0x37, 0x61)
                || StartsWith(bytes, 0x47, 0x49, 0x46, 0x38, 0x39, 0x61);
        }

        private static bool IsWebp(byte[] bytes)
        {
            // "RIFF" + 4 byte boyut + "WEBP"
            if (bytes.Length < 12)
            {
                return false;
            }

            var riff = StartsWith(bytes, 0x52, 0x49, 0x46, 0x46);
            var webp = bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;
            return riff && webp;
        }

        private static bool IsHeif(byte[] bytes)
        {
            // ISO base media file format: byte 4-7 "ftyp", ardından marka (heic/heif/mif1/msf1/heix vb.)
            if (bytes.Length < 12)
            {
                return false;
            }

            var hasFtyp = bytes[4] == 'f' && bytes[5] == 't' && bytes[6] == 'y' && bytes[7] == 'p';
            if (!hasFtyp)
            {
                return false;
            }

            var brand = System.Text.Encoding.ASCII.GetString(bytes, 8, 4);
            return brand is "heic" or "heix" or "heif" or "mif1" or "msf1" or "hevc" or "hevx";
        }

        private static bool LooksLikePlainText(byte[] bytes)
        {
            var sampleLength = Math.Min(bytes.Length, 8000);

            // Bilinen ikili formatların imzasını taşıyorsa (biri text/plain diye etiketlenmiş bir görsel/PDF/exe göndermeye çalışıyordur) reddet.
            if (StartsWith(bytes, 0xFF, 0xD8, 0xFF) ||                                  // JPEG
                StartsWith(bytes, 0x89, 0x50, 0x4E, 0x47) ||                            // PNG
                StartsWith(bytes, 0x25, 0x50, 0x44, 0x46) ||                            // PDF
                StartsWith(bytes, 0x4D, 0x5A) ||                                        // MZ (Windows .exe/.dll)
                StartsWith(bytes, 0x7F, 0x45, 0x4C, 0x46) ||                            // ELF (Linux binary)
                StartsWith(bytes, 0x50, 0x4B, 0x03, 0x04))                              // ZIP (docx/xlsx/apk/jar dahil)
            {
                return false;
            }

            // Gerçek metin dosyalarında NUL byte bulunmaz; ikili dosyalarda sık görülür.
            for (var i = 0; i < sampleLength; i++)
            {
                if (bytes[i] == 0x00)
                {
                    return false;
                }
            }

            return true;
        }
    }
}