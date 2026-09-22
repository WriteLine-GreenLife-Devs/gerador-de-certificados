namespace GeradorCertificados.Api.Contracts.Certificados;

public sealed record SolicitarCertificadosRequest(IReadOnlyCollection<string> Alunos);
public sealed record SolicitacaoCertificadoResponse(Guid SolicitacaoId, Guid CursoId, string Status, DateTimeOffset DataSolicitacao);
public sealed record StatusSolicitacaoResponse(Guid SolicitacaoId, Guid CursoId, string Status, DateTimeOffset DataSolicitacao, int QuantidadeTotal, int QuantidadeGerada, int QuantidadeFalha);
public sealed record CertificadoItemResponse(Guid Id, string NomeAluno, string Status, DateTimeOffset? DataGeracao);
