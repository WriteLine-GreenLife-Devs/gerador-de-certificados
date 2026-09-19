using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Cursos;
using Moq;
namespace GeradorCertificados.Application.Tests.Modulos.Cursos;
[TestClass]
public sealed class ServicoCursosTests
{
    [TestMethod]
    public async Task CadastrarAsync_ComDadosValidos_PersisteCurso()
    { Mock<IRepositorioCurso> repositorioMock = new(); var servico = new ServicoCursos(repositorioMock.Object); var curso = await servico.CadastrarAsync("Curso", null, 12, new DateOnly(2026, 2, 1), CancellationToken.None); repositorioMock.Verify(r => r.AdicionarAsync(It.Is<Curso>(c => c.Id == curso.Id && c.Nome == "Curso"), It.IsAny<CancellationToken>()), Times.Once); }
    [TestMethod]
    public async Task ConsultarAsync_ComCursoInexistente_RetornaNulo()
    { Mock<IRepositorioCurso> repositorioMock = new(); repositorioMock.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Curso?)null); var servico = new ServicoCursos(repositorioMock.Object); Assert.IsNull(await servico.ConsultarAsync(Guid.NewGuid(), CancellationToken.None)); }
}
