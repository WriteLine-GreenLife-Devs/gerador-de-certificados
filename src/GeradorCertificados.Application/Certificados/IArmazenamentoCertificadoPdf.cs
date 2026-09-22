namespace GeradorCertificados.Application.Certificados;

public interface IArmazenamentoCertificadoPdf
{
    string ObterDiretorioBase();
    string ObterCaminhoRelativo(Guid solicitacaoId, Guid certificadoId);
    string ObterCaminhoFisico(Guid solicitacaoId, Guid certificadoId);
    void Salvar(Guid solicitacaoId, Guid certificadoId, byte[] pdf);
    bool ArquivoExiste(Guid solicitacaoId, Guid certificadoId);
}
