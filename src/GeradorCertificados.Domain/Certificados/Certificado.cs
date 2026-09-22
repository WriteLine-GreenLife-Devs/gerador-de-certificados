namespace GeradorCertificados.Domain.Certificados;

public sealed class Certificado
{
    private Certificado() { NomeAluno = null!; }

    private Certificado(Guid solicitacaoId, string nomeAluno)
    {
        Id = Guid.NewGuid();
        SolicitacaoId = solicitacaoId;
        NomeAluno = nomeAluno;
        Status = StatusCertificado.Pendente;
        CaminhoArquivo = null;
        DataGeracao = null;
    }

    public Guid Id { get; private set; }
    public Guid SolicitacaoId { get; private set; }
    public string NomeAluno { get; private set; }
    public StatusCertificado Status { get; private set; }
    public string? CaminhoArquivo { get; private set; }
    public DateTimeOffset? DataGeracao { get; private set; }

    public static Certificado Criar(Guid solicitacaoId, string nomeAluno)
    {
        if (solicitacaoId == Guid.Empty) throw new ArgumentException("A solicitação é obrigatória.", nameof(solicitacaoId));

        var nomeNormalizado = NormalizarNomeAluno(nomeAluno);

        return new Certificado(solicitacaoId, nomeNormalizado);
    }

    public void MarcarComoGerado(string caminhoArquivo)
    {
        if (Status == StatusCertificado.Gerado || Status == StatusCertificado.Falha)
            throw new InvalidOperationException("Este certificado já está em um estado final.");

        if (string.IsNullOrWhiteSpace(caminhoArquivo))
            throw new ArgumentException("O caminho do arquivo é obrigatório.", nameof(caminhoArquivo));

        CaminhoArquivo = caminhoArquivo.Trim();
        DataGeracao = DateTimeOffset.UtcNow;
        Status = StatusCertificado.Gerado;
    }

    public void MarcarComoFalha()
    {
        if (Status == StatusCertificado.Gerado || Status == StatusCertificado.Falha)
            throw new InvalidOperationException("Este certificado já está em um estado final.");

        Status = StatusCertificado.Falha;
        CaminhoArquivo = null;
        DataGeracao = null;
    }

    private static string NormalizarNomeAluno(string? nomeAluno)
    {
        var valor = nomeAluno?.Trim();
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("O nome do aluno é obrigatório.", nameof(nomeAluno));
        if (valor.Length > 200) throw new ArgumentException("O nome do aluno deve ter no máximo 200 caracteres.", nameof(nomeAluno));
        return valor;
    }
}
