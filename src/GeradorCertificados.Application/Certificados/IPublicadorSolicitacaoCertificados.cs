namespace GeradorCertificados.Application.Certificados;

public interface IPublicadorSolicitacaoCertificados
{
    Task PublicarSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct);
}
