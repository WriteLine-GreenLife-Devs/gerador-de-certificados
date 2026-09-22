namespace GeradorCertificados.Application.Certificados;

public sealed class CursoInexistenteException(Guid cursoId) : Exception($"O curso '{cursoId}' não foi encontrado.");

public sealed class SolicitacaoEmProcessamentoException(Guid cursoId) : Exception($"Já existe uma solicitação ativa para o curso '{cursoId}'.");

public sealed class SolicitacaoNaoEncontradaException(Guid cursoId) : Exception($"Não existe solicitação para o curso '{cursoId}'.");
