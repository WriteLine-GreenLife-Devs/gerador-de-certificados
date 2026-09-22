using MediatR;

namespace GeradorCertificados.Application.Certificados.Queries;

public sealed record ListarCertificadosQuery(Guid CursoId) : IRequest<IReadOnlyCollection<CertificadoResumo>>;
