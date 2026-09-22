namespace GeradorCertificados.Application.Certificados;

public interface IGeradorPdfCertificado
{
    byte[] GerarPdf(DadosCertificadoPdf dados);
}
