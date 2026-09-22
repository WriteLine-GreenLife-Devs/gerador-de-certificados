using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Mensagens;
using GeradorCertificados.Domain.Certificados;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GeradorCertificados.Infrastructure.Certificados;

public sealed class ProcessarSolicitacaoCertificadosConsumer(
    ISolicitacaoCertificadoRepository solicitacoes,
    ILogger<ProcessarSolicitacaoCertificadosConsumer>? logger = null) : IConsumer<ProcessarSolicitacaoCertificados>
{
    public async Task Consume(ConsumeContext<ProcessarSolicitacaoCertificados> context)
    {
        await ProcessarAsync(context.Message.SolicitacaoId, context.CancellationToken);
    }

    public async Task ProcessarAsync(Guid solicitacaoId, CancellationToken ct)
    {
        var solicitacao = await solicitacoes.ObterPorIdAsync(solicitacaoId, ct);
        if (solicitacao is null)
        {
            logger?.LogWarning("Mensagem de processamento recebida para solicitação inexistente: {SolicitacaoId}", solicitacaoId);
            return;
        }

        if (solicitacao.Status == StatusSolicitacaoCertificado.GerandoCertificados ||
            solicitacao.Status == StatusSolicitacaoCertificado.GerandoZip ||
            solicitacao.Status == StatusSolicitacaoCertificado.Concluido ||
            solicitacao.Status == StatusSolicitacaoCertificado.Falha)
        {
            return;
        }

        if (solicitacao.Status == StatusSolicitacaoCertificado.Pendente)
        {
            solicitacao.IniciarProcessamento();
            await solicitacoes.SalvarAlteracoesAsync(ct);
            return;
        }
    }
}
