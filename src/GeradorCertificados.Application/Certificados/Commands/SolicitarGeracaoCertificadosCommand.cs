using MediatR;

namespace GeradorCertificados.Application.Certificados.Commands;

public sealed record SolicitarGeracaoCertificadosCommand(Guid CursoId, IReadOnlyCollection<string> Alunos) : IRequest<SolicitacaoCertificadoResumo>;
