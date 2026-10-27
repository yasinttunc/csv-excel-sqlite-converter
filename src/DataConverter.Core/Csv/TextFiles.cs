using System.Text;

namespace DataConverter.Core.Csv;

/// <summary>Reads text files whose encoding is not known in advance.</summary>
public static class TextFiles
{
    /// <summary>Windows code page 1254, used by Turkish versions of Excel and Windows for "CSV (comma delimited)".</summary>
    public const int TurkishCodePage = 1254;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    static TextFiles() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>
    /// Decodes the bytes using their byte order mark if there is one, otherwise as UTF-8, and if that is not valid UTF-8,
    /// as <paramref name="fallbackCodePage"/> (Turkish Windows by default).
    /// </summary>
    public static string Decode(byte[] bytes, out Encoding encoding, int fallbackCodePage = TurkishCodePage)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            encoding = Encoding.UTF8;
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            encoding = Encoding.Unicode;
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            encoding = Encoding.BigEndianUnicode;
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        try
        {
            var text = StrictUtf8.GetString(bytes);
            encoding = Encoding.UTF8;
            return text;
        }
        catch (DecoderFallbackException)
        {
            encoding = Encoding.GetEncoding(fallbackCodePage);
            return encoding.GetString(bytes);
        }
    }

    public static string Read(string path, out Encoding encoding) => Decode(File.ReadAllBytes(path), out encoding);
}
