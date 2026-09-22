using GeradorCertificados.Application.Certificados;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GeradorCertificados.Infrastructure.Certificados;

public sealed class QuestPdfCertificadoGenerator : IGeradorPdfCertificado
{
    static QuestPdfCertificadoGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GerarPdf(DadosCertificadoPdf dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(14).FontFamily(Fonts.Arial));

                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().AlignCenter().Text("CERTIFICADO")
                        .SemiBold().FontSize(26);

                    column.Item().AlignCenter().Text("Certificamos que").FontSize(18);
                    column.Item().AlignCenter().Text(dados.NomeAluno)
                        .Bold().FontSize(28);

                    column.Item().AlignCenter().Text("concluiu o curso").FontSize(18);
                    column.Item().AlignCenter().Text(dados.NomeCurso)
                        .Bold().FontSize(24);

                    column.Item().AlignCenter().Text($"com carga horária de {dados.CargaHoraria} horas,")
                        .FontSize(16);
                    column.Item().AlignCenter().Text($"concluído em {dados.DataConclusao:dd/MM/yyyy}.")
                        .FontSize(16);
                });
            });
        });

        return document.GeneratePdf();
    }
}
