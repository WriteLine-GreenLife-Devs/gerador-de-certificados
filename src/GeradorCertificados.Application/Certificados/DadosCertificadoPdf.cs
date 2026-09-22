namespace GeradorCertificados.Application.Certificados;

public sealed record DadosCertificadoPdf(
    Guid CertificadoId,
    Guid SolicitacaoId,
    string NomeAluno,
    string NomeCurso,
    int CargaHoraria,
    DateOnly DataConclusao);
