using iText.Kernel.Pdf;
using iText.Signatures;

namespace SignaturesDemo.Api.Services;

public sealed class SignatureValidator
{
    public ValidationResult Validate(byte[] signedPdf)
    {
        using var ms = new MemoryStream(signedPdf);
        using var reader = new PdfReader(ms);
        using var doc = new PdfDocument(reader);

        var util = new SignatureUtil(doc);
        var names = util.GetSignatureNames();

        if (names.Count == 0)
        {
            return new ValidationResult
            {
                IsValid = false,
                Error = "El PDF no contiene ninguna firma electrónica.",
                Signatures = []
            };
        }

        var results = new List<SignatureInfo>();
        var allValid = true;

        foreach (var name in names)
        {
            var pkcs7 = util.ReadSignatureData(name);
            var coversWholeDoc = util.SignatureCoversWholeDocument(name);

            bool integrity;
            try
            {
                integrity = pkcs7.VerifySignatureIntegrityAndAuthenticity();
            }
            catch
            {
                integrity = false;
            }

            var cert = pkcs7.GetSigningCertificate();
            var subject = cert.GetSubjectDN()?.ToString() ?? "(desconocido)";
            var issuer = cert.GetIssuerDN()?.ToString() ?? "(desconocido)";
            var notBefore = cert.GetNotBefore();
            var notAfter = cert.GetNotAfter();
            var serial = cert.GetSerialNumber().ToString(16);
            var signedAt = pkcs7.GetSignDate();

            var info = new SignatureInfo
            {
                FieldName = name,
                IntegrityValid = integrity,
                CoversWholeDocument = coversWholeDoc,
                SignerSubject = subject,
                SignerCommonName = ExtractCn(subject),
                IssuerSubject = issuer,
                IssuerCommonName = ExtractCn(issuer),
                CertificateSerialHex = serial,
                CertificateNotBefore = notBefore,
                CertificateNotAfter = notAfter,
                SignedAt = signedAt,
                Algorithm = pkcs7.GetSignatureMechanismName(),
                Reason = pkcs7.GetReason(),
                Location = pkcs7.GetLocation()
            };

            results.Add(info);
            allValid = allValid && integrity && coversWholeDoc;
        }

        return new ValidationResult
        {
            IsValid = allValid,
            Signatures = results
        };
    }

    private static string ExtractCn(string dn)
    {
        // DN looks like: CN=NOMBRE APELLIDO - NIF, O=..., C=ES
        var parts = dn.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                return trimmed[3..];
        }
        return dn;
    }
}

public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public List<SignatureInfo> Signatures { get; init; } = [];
}

public sealed class SignatureInfo
{
    public required string FieldName { get; init; }
    public bool IntegrityValid { get; init; }
    public bool CoversWholeDocument { get; init; }
    public required string SignerSubject { get; init; }
    public required string SignerCommonName { get; init; }
    public required string IssuerSubject { get; init; }
    public required string IssuerCommonName { get; init; }
    public required string CertificateSerialHex { get; init; }
    public DateTime CertificateNotBefore { get; init; }
    public DateTime CertificateNotAfter { get; init; }
    public DateTime SignedAt { get; init; }
    public required string Algorithm { get; init; }
    public string? Reason { get; init; }
    public string? Location { get; init; }
}
