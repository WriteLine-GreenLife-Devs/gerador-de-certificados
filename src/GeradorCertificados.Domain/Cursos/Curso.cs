namespace GeradorCertificados.Domain.Cursos;
public sealed class Curso
{
    private Curso() { Nome = null!; }
    private Curso(string nome, string? descricao, int cargaHoraria, DateOnly dataConclusao) { Id = Guid.NewGuid(); Nome = nome; Descricao = descricao; CargaHoraria = cargaHoraria; DataConclusao = dataConclusao; }
    public Guid Id { get; private set; } public string Nome { get; private set; } public string? Descricao { get; private set; } public int CargaHoraria { get; private set; } public DateOnly DataConclusao { get; private set; }
    public static Curso Criar(string nome, string? descricao, int cargaHoraria, DateOnly dataConclusao)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 200) throw new ArgumentException("O nome deve ter entre 1 e 200 caracteres.", nameof(nome));
        if (descricao?.Length > 500) throw new ArgumentException("A descrição deve ter no máximo 500 caracteres.", nameof(descricao));
        if (cargaHoraria <= 0) throw new ArgumentOutOfRangeException(nameof(cargaHoraria), "A carga horária deve ser maior que zero.");
        return new Curso(nome.Trim(), descricao?.Trim(), cargaHoraria, dataConclusao);
    }
}
