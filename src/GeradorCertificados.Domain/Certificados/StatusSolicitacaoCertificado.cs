namespace GeradorCertificados.Domain.Certificados;

public enum StatusSolicitacaoCertificado
{
    Pendente,
    GerandoCertificados,
    GerandoZip,
    Concluido,
    Falha
}
