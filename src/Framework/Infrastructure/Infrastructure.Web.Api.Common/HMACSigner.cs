using System.Security.Cryptography;
using System.Text;
using Common.Extensions;

namespace Infrastructure.Web.Api.Common;

/// <summary>
///     Provides a signer of HMAC signatures
/// </summary>
public class HMACSigner
{
    private const string SignatureFormat = @"sha256={0}";
    internal static readonly Encoding SignatureEncoding = Encoding.UTF8;
    private readonly byte[] _data;
    private readonly string _secret;

    public HMACSigner(string text, string secret) : this(SignatureEncoding.GetBytes(text), secret)
    {
        ArgumentNullException.ThrowIfNull(text);
    }

    public HMACSigner(byte[] data, string secret)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(secret);

        _data = data;
        _secret = secret;
    }

#if TESTINGONLY
    public static string GenerateKey()
    {
        var algorithm = new HMACSHA256();
        return Convert.ToBase64String(algorithm.Key);
    }
#endif

    public string Sign()
    {
        var key = SignatureEncoding.GetBytes(_secret);
        var signature = SignatureFormat.Format(SignBody(_data, key));

        return signature;
    }

    private static string SignBody(byte[] body, byte[] key)
    {
        var signature = new HMACSHA256(key)
            .ComputeHash(body);

        return ToHex(signature);
    }

    private static string ToHex(byte[] bytes)
    {
        var builder = new StringBuilder();
        bytes
            .ToList()
            .ForEach(b => { builder.Append(b.ToString("x2")); });

        return builder.ToString();
    }
}

/// <summary>
///     Provides a verifier of HMAC signatures
/// </summary>
public class HMACVerifier
{
    private readonly HMACSigner _signer;

    public HMACVerifier(HMACSigner signer)
    {
        ArgumentNullException.ThrowIfNull(signer);

        _signer = signer;
    }

    public bool Verify(string signature)
    {
        ArgumentException.ThrowIfNullOrEmpty(signature);

        var expectedSignature = _signer.Sign();

        return expectedSignature == signature;
    }
}