using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Mensagens;
using MassTransit;

namespace GeradorCertificados.Infrastructure.Certificados;

public sealed class MassTransitCertificadosMensagemPublisher(IPublishEndpoint publishEndpoint) : IPublicadorSolicitacaoCertificados
{
    public Task PublicarSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct)
        => publishEndpoint.Publish(new ProcessarSolicitacaoCertificados(solicitacaoId), ct);
}
