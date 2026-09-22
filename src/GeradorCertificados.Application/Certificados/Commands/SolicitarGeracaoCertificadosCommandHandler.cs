using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using MediatR;

namespace GeradorCertificados.Application.Certificados.Commands;

public sealed class SolicitarGeracaoCertificadosCommandHandler(
    IRepositorioCurso cursos,
    ISolicitacaoCertificadoRepository solicitacoes,
    IPublicadorSolicitacaoCertificados? publicador = null) : IRequestHandler<SolicitarGeracaoCertificadosCommand, SolicitacaoCertificadoResumo>
{
    public async Task<SolicitacaoCertificadoResumo> Handle(SolicitarGeracaoCertificadosCommand request, CancellationToken ct)
    {
        if (request.CursoId == Guid.Empty)
            throw new ArgumentException("O curso é obrigatório.", nameof(request.CursoId));

        if (request.Alunos is null)
            throw new ArgumentNullException(nameof(request.Alunos));

        var alunos = request.Alunos
            .Select(x => x?.Trim())
            .ToList();

        if (alunos.Count == 0 || alunos.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("A lista de alunos deve possuir ao menos um item e nomes válidos.", nameof(request.Alunos));

        if (alunos.Any(x => x!.Length > 200))
            throw new ArgumentException("O nome do aluno deve ter no máximo 200 caracteres.", nameof(request.Alunos));

        var curso = await cursos.ObterPorIdAsync(request.CursoId, ct);
        if (curso is null)
            throw new CursoInexistenteException(request.CursoId);

        var solicitacaoAtiva = await solicitacoes.ObterMaisRecentePorCursoAsync(request.CursoId, ct);
        if (solicitacaoAtiva is not null &&
            (solicitacaoAtiva.Status == StatusSolicitacaoCertificado.Pendente ||
             solicitacaoAtiva.Status == StatusSolicitacaoCertificado.GerandoCertificados ||
             solicitacaoAtiva.Status == StatusSolicitacaoCertificado.GerandoZip))
        {
            throw new SolicitacaoEmProcessamentoException(request.CursoId);
        }

        var solicitacao = SolicitacaoCertificado.Criar(request.CursoId, alunos!);
        await solicitacoes.AdicionarAsync(solicitacao, ct);

        if (publicador is not null)
            await publicador.PublicarSolicitacaoAsync(solicitacao.Id, ct);

        return new SolicitacaoCertificadoResumo(
            solicitacao.Id,
            solicitacao.CursoId,
            solicitacao.Status,
            solicitacao.DataSolicitacao,
            solicitacao.Certificados.Count);
    }
}

public sealed record SolicitacaoCertificadoResumo(Guid SolicitacaoId, Guid CursoId, StatusSolicitacaoCertificado Status, DateTimeOffset DataSolicitacao, int QuantidadeCertificados);
