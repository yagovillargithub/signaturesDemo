using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;

namespace SignaturesDemo.Api.Services;

/// <summary>
/// Decorates the last page of a PDF with a "Firma electrónica" label and a
/// thin rectangle in the bottom-right corner, then adds an empty signature
/// widget covering that same rectangle. AutoFirma fills the widget with its
/// auto-generated appearance (signer name, date) when the user signs, while
/// the surrounding label and border remain part of the page content.
/// </summary>
public sealed class SignatureFieldInjector
{
    private const float BoxWidth = 240f;
    private const float BoxHeight = 100f;
    private const float MarginRight = 30f;
    private const float MarginBottom = 40f;
    private const float LabelOffset = 8f;

    public InjectResult AddSignatureField(byte[] pdfBytes, string fieldName)
    {
        using var input = new MemoryStream(pdfBytes);
        using var output = new MemoryStream();
        using var reader = new PdfReader(input);
        using var writer = new PdfWriter(output);
        using var doc = new PdfDocument(reader, writer);

        var pageNumber = doc.GetNumberOfPages();
        var page = doc.GetPage(pageNumber);
        var pageSize = page.GetPageSize();

        var llx = pageSize.GetRight() - MarginRight - BoxWidth;
        var lly = pageSize.GetBottom() + MarginBottom;
        var bbox = new Rectangle(llx, lly, BoxWidth, BoxHeight);

        DrawDecoration(page, bbox);

        var form = PdfAcroForm.GetAcroForm(doc, createIfNotExist: true);
        var field = new SignatureFormFieldBuilder(doc, fieldName)
            .SetPage(pageNumber)
            .SetWidgetRectangle(bbox)
            .CreateSignature();
        form.AddField(field);

        doc.Close();

        var box = new SignatureBox(
            LowerLeftX: bbox.GetLeft(),
            LowerLeftY: bbox.GetBottom(),
            UpperRightX: bbox.GetRight(),
            UpperRightY: bbox.GetTop(),
            PageNumber: pageNumber);

        return new InjectResult(output.ToArray(), box);
    }

    private static void DrawDecoration(PdfPage page, Rectangle bbox)
    {
        var canvas = new PdfCanvas(page);
        var helv = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var helvBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        var labelColor = new DeviceRgb(70, 90, 130);  // soft navy
        var borderColor = new DeviceRgb(170, 180, 200);

        // Label "Firma electrónica" above the box, left-aligned with the box.
        canvas.SaveState()
            .BeginText()
            .SetFontAndSize(helvBold, 8.5f)
            .SetFillColor(labelColor)
            .MoveText(bbox.GetLeft(), bbox.GetTop() + LabelOffset)
            .ShowText("FIRMA ELECTRÓNICA")
            .EndText()
            .RestoreState();

        // Subtle rectangle around the signature widget so the placeholder is
        // visible before signing, and frames the AutoFirma appearance after.
        canvas.SaveState()
            .SetStrokeColor(borderColor)
            .SetLineWidth(0.7f)
            .Rectangle(bbox.GetLeft(), bbox.GetBottom(), bbox.GetWidth(), bbox.GetHeight())
            .Stroke()
            .RestoreState();

        _ = helv; // reserved for future use
    }
}

public sealed record SignatureBox(
    float LowerLeftX,
    float LowerLeftY,
    float UpperRightX,
    float UpperRightY,
    int PageNumber);

public sealed record InjectResult(byte[] PdfBytes, SignatureBox Box);
