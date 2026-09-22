using MediatR;

namespace GeradorCertificados.Application.Certificados.Queries;

public sealed record ObterDownloadCertificadosQuery(Guid CursoId) : IRequest<ArquivoCertificadoDownload?>;

public sealed record ArquivoCertificadoDownload(
    byte[] Conteudo,
    string NomeArquivo,
    string ContentType);
