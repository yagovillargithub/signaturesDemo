using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SignaturesDemo.Api.Services;

public sealed class SamplePdfGenerator(SignatureFieldInjector injector)
{
    public const string SignatureFieldName = "FirmaUsuario";

    public SamplePdfResult Generate(string documentId, string title, string body)
    {
        var generatedAt = DateTimeOffset.UtcNow;

        var rawPdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontFamily(Fonts.Calibri).FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("SignaturesDemo")
                        .FontSize(9).FontColor(Colors.Grey.Darken1).LetterSpacing(0.1f);
                    col.Item().PaddingTop(4).Text(title)
                        .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken3);
                    col.Item().PaddingTop(2).LineHorizontal(0.6f).LineColor(Colors.Blue.Darken2);
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text(text =>
                    {
                        text.Span("Documento ID: ").SemiBold();
                        text.Span(documentId).FontColor(Colors.Grey.Darken2);
                    });

                    col.Item().Text(text =>
                    {
                        text.Span("Generado: ").SemiBold();
                        text.Span(generatedAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
                    });

                    col.Item().PaddingTop(10).Text(body).LineHeight(1.4f);

                    col.Item().PaddingTop(20).Background(Colors.Grey.Lighten4).Padding(12).Column(c =>
                    {
                        c.Item().Text("Cláusula de firma").SemiBold().FontColor(Colors.Blue.Darken3);
                        c.Item().PaddingTop(4).Text(
                            "Al firmar electrónicamente este documento, el firmante manifiesta su " +
                            "conformidad con su contenido. La firma se realizará en formato PAdES " +
                            "(PDF Advanced Electronic Signatures) y quedará embebida en el propio PDF. " +
                            "La representación visual de la firma se dibujará en la esquina inferior " +
                            "derecha de esta página.");
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Darken1));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        var injected = injector.AddSignatureField(rawPdf, SignatureFieldName);
        return new SamplePdfResult(injected.PdfBytes, SignatureFieldName, injected.Box);
    }
}

public sealed record SamplePdfResult(
    byte[] PdfBytes,
    string SignatureFieldName,
    SignatureBox Box);
