namespace GeradorCertificados.Domain.Certificados;

public sealed class SolicitacaoCertificado
{
    private readonly List<Certificado> _certificados = [];

    private SolicitacaoCertificado() { }

    private SolicitacaoCertificado(Guid cursoId, IEnumerable<string> nomesAlunos)
    {
        Id = Guid.NewGuid();
        CursoId = cursoId;
        Status = StatusSolicitacaoCertificado.Pendente;
        DataSolicitacao = DateTimeOffset.UtcNow;

        foreach (var nome in nomesAlunos)
            _certificados.Add(Certificado.Criar(Id, nome));
    }

    public Guid Id { get; private set; }
    public Guid CursoId { get; private set; }
    public StatusSolicitacaoCertificado Status { get; private set; }
    public DateTimeOffset DataSolicitacao { get; private set; }
    public IReadOnlyCollection<Certificado> Certificados => _certificados.AsReadOnly();

    public static SolicitacaoCertificado Criar(Guid cursoId, IEnumerable<string> nomesAlunos)
    {
        if (cursoId == Guid.Empty) throw new ArgumentException("O curso é obrigatório.", nameof(cursoId));

        if (nomesAlunos is null) throw new ArgumentNullException(nameof(nomesAlunos));

        var alunos = nomesAlunos.ToList();
        if (alunos.Count == 0) throw new ArgumentException("A lista de alunos deve possuir ao menos um item.", nameof(nomesAlunos));

        return new SolicitacaoCertificado(cursoId, alunos);
    }

    public void IniciarProcessamento()
    {
        if (Status == StatusSolicitacaoCertificado.Pendente)
        {
            Status = StatusSolicitacaoCertificado.GerandoCertificados;
            return;
        }

        throw new InvalidOperationException("Só é possível iniciar o processamento quando a solicitação está pendente.");
    }

    public void IniciarGeracaoZip()
    {
        if (Status == StatusSolicitacaoCertificado.GerandoCertificados)
        {
            Status = StatusSolicitacaoCertificado.GerandoZip;
            return;
        }

        throw new InvalidOperationException("A geração do ZIP só pode iniciar após a geração dos certificados.");
    }

    public void Concluir()
    {
        if (Status == StatusSolicitacaoCertificado.GerandoZip)
        {
            Status = StatusSolicitacaoCertificado.Concluido;
            return;
        }

        throw new InvalidOperationException("Só é possível concluir a solicitação quando a geração do ZIP foi iniciada.");
    }

    public void MarcarFalha()
    {
        if (Status == StatusSolicitacaoCertificado.Concluido || Status == StatusSolicitacaoCertificado.Falha)
            throw new InvalidOperationException("Não é possível alterar o estado final da solicitação.");

        Status = StatusSolicitacaoCertificado.Falha;
    }
}
