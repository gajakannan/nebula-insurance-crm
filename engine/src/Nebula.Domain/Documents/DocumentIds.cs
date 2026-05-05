using System.Security.Cryptography;
using System.Text;

namespace Nebula.Domain.Documents;

public static class DocumentIds
{
    private const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string NewDocumentId() => $"doc_{NewUlidLikeString()}";

    public static string NewUploadId() => $"upl_{NewUlidLikeString()}";

    public static bool IsDocumentId(string value)
    {
        if (!value.StartsWith("doc_", StringComparison.Ordinal) || value.Length != 30)
            return false;

        return value.AsSpan(4).ToArray().All(c => Crockford.Contains(c));
    }

    public static Guid StableGuid(string documentId)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(documentId));
        return new Guid(hash);
    }

    private static string NewUlidLikeString()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;

        var value = new StringBuilder(26);
        var bitBuffer = 0;
        var bitCount = 0;
        foreach (var b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5 && value.Length < 26)
            {
                bitCount -= 5;
                value.Append(Crockford[(bitBuffer >> bitCount) & 31]);
            }
        }

        if (value.Length < 26 && bitCount > 0)
            value.Append(Crockford[(bitBuffer << (5 - bitCount)) & 31]);

        return value.ToString().PadRight(26, '0')[..26];
    }
}
