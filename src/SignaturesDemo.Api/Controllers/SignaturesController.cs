using Microsoft.AspNetCore.Mvc;
using SignaturesDemo.Api.Services;

namespace SignaturesDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SignaturesController(SignatureValidator validator, ILogger<SignaturesController> log)
    : ControllerBase
{
    /// <summary>
    /// Recibe un PDF firmado (en base64) y devuelve metadatos de la firma + validez.
    /// </summary>
    [HttpPost("validate")]
    public ActionResult<ValidationResult> Validate([FromBody] ValidateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SignedPdfBase64))
            return BadRequest(new { error = "signedPdfBase64 is required" });

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.SignedPdfBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "signedPdfBase64 is not valid base64" });
        }

        try
        {
            var result = validator.Validate(bytes);
            return Ok(result);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Signature validation failed");
            return StatusCode(500, new { error = "Validation error", detail = ex.Message });
        }
    }
}

public sealed class ValidateRequest
{
    public string SignedPdfBase64 { get; set; } = string.Empty;
}
