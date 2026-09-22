using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Mensagens;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GeradorCertificados.Infrastructure.Certificados;

public sealed class ProcessarSolicitacaoCertificadosConsumer(
    ISolicitacaoCertificadoRepository solicitacoes,
    IRepositorioCurso cursos,
    IGeradorPdfCertificado geradorPdf,
    IArmazenamentoCertificadoPdf armazenamentoPdf,
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

        if (solicitacao.Status == StatusSolicitacaoCertificado.GerandoZip ||
            solicitacao.Status == StatusSolicitacaoCertificado.Concluido ||
            solicitacao.Status == StatusSolicitacaoCertificado.Falha)
        {
            return;
        }

        if (solicitacao.Status == StatusSolicitacaoCertificado.Pendente)
        {
            solicitacao.IniciarProcessamento();
        }

        try
        {
            if (solicitacao.Status == StatusSolicitacaoCertificado.GerandoCertificados)
            {
                var curso = await cursos.ObterPorIdAsync(solicitacao.CursoId, ct);
                if (curso is null)
                {
                    solicitacao.MarcarFalha();
                    await solicitacoes.SalvarAlteracoesAsync(ct);
                    logger?.LogError("Solicitação {SolicitacaoId} referenciou curso inexistente {CursoId}.", solicitacaoId, solicitacao.CursoId);
                    return;
                }

                foreach (var certificado in solicitacao.Certificados.Where(x => x.Status == StatusCertificado.Pendente))
                {
                    try
                    {
                        var dadosPdf = new DadosCertificadoPdf(
                            certificado.Id,
                            solicitacao.Id,
                            certificado.NomeAluno,
                            curso.Nome,
                            curso.CargaHoraria,
                            curso.DataConclusao);

                        var pdf = geradorPdf.GerarPdf(dadosPdf);
                        var caminhoRelativo = armazenamentoPdf.ObterCaminhoRelativo(solicitacao.Id, certificado.Id);
                        var caminhoFisico = armazenamentoPdf.ObterCaminhoFisico(solicitacao.Id, certificado.Id);

                        if (armazenamentoPdf.ArquivoExiste(solicitacao.Id, certificado.Id))
                        {
                            logger?.LogInformation("Arquivo do certificado {CertificadoId} já existe; mantendo o arquivo atual.", certificado.Id);
                        }
                        else
                        {
                            armazenamentoPdf.Salvar(solicitacao.Id, certificado.Id, pdf);
                        }

                        certificado.MarcarComoGerado(caminhoRelativo);
                        await solicitacoes.SalvarAlteracoesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        certificado.MarcarComoFalha();
                        await solicitacoes.SalvarAlteracoesAsync(ct);
                        logger?.LogError(ex, "Falha ao processar certificado {CertificadoId} da solicitação {SolicitacaoId}.", certificado.Id, solicitacaoId);
                    }
                }

                if (solicitacao.Certificados.All(x => x.Status is StatusCertificado.Gerado or StatusCertificado.Falha))
                {
                    solicitacao.IniciarGeracaoZip();
                    await solicitacoes.SalvarAlteracoesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Falha global ao processar a solicitação {SolicitacaoId}.", solicitacaoId);
            solicitacao.MarcarFalha();
            await solicitacoes.SalvarAlteracoesAsync(ct);
        }
    }
}
