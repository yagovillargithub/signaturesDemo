using Microsoft.AspNetCore.Mvc;
using SignaturesDemo.Api.Services;

namespace SignaturesDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PdfController(SamplePdfGenerator generator) : ControllerBase
{
    /// <summary>
    /// Raw PDF (binary). Useful for direct download / Swagger inspection.
    /// </summary>
    [HttpGet("sample")]
    public IActionResult GetSample()
    {
        var result = BuildSample();
        return File(result.PdfBytes, "application/pdf", $"documento-{Guid.NewGuid():N}.pdf"[..28]);
    }

    /// <summary>
    /// PDF + the metadata Blazor needs to drive AutoFirma: signature field name
    /// and bounding box where AutoFirma should paint the visible signature.
    /// </summary>
    [HttpGet("sample-with-box")]
    public ActionResult<PdfWithBoxResponse> GetSampleWithBox()
    {
        var documentId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var result = generator.Generate(
            documentId,
            title: "Documento de prueba para firma electrónica",
            body:
                "Este PDF se genera dinámicamente en el servidor cada vez que se solicita. " +
                "Su finalidad es servir de soporte de prueba para la integración con AutoFirma. " +
                "Una vez firmado, el documento mantendrá su contenido original y añadirá una " +
                "firma PAdES embebida que cualquier lector PDF compatible (Adobe Reader, " +
                "navegadores modernos) podrá verificar.\n\n" +
                "El identificador de documento que figura abajo es único por solicitud, lo que " +
                "permite distinguir varias firmas durante las pruebas.");

        return Ok(new PdfWithBoxResponse(
            DocumentId: documentId,
            PdfBase64: Convert.ToBase64String(result.PdfBytes),
            SignatureFieldName: result.SignatureFieldName,
            Box: result.Box));
    }

    private SamplePdfResult BuildSample()
    {
        var documentId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return generator.Generate(documentId,
            "Documento de prueba para firma electrónica",
            "Este PDF se genera dinámicamente en el servidor.");
    }
}

public sealed record PdfWithBoxResponse(
    string DocumentId,
    string PdfBase64,
    string SignatureFieldName,
    SignatureBox Box);
