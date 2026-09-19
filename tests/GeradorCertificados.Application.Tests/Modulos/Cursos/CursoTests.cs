using GeradorCertificados.Domain.Cursos;
namespace GeradorCertificados.Application.Tests.Modulos.Cursos;
[TestClass]
public sealed class CursoTests
{
    [TestMethod] public void Criar_ComDadosValidos_CriaCurso() { var curso = Curso.Criar("C# Avançado", "Descrição", 20, new DateOnly(2026, 12, 10)); Assert.AreEqual("C# Avançado", curso.Nome); Assert.AreEqual(20, curso.CargaHoraria); }
    [TestMethod] [DataRow("")] [DataRow(" ")] public void Criar_ComNomeInvalido_LancaArgumentException(string nome) => Assert.Throws<ArgumentException>(() => Curso.Criar(nome, null, 10, new DateOnly(2026, 1, 1)));
    [TestMethod] public void Criar_ComDescricaoMaiorQue500Caracteres_LancaArgumentException() => Assert.Throws<ArgumentException>(() => Curso.Criar("Curso", new string('a', 501), 10, new DateOnly(2026, 1, 1)));
    [TestMethod] public void Criar_ComCargaHorariaNaoPositiva_LancaArgumentOutOfRangeException() => Assert.Throws<ArgumentOutOfRangeException>(() => Curso.Criar("Curso", null, 0, new DateOnly(2026, 1, 1)));
    [TestMethod] public void Criar_ComDataConclusaoPadrao_LancaArgumentException() => Assert.Throws<ArgumentException>(() => Curso.Criar("Curso", null, 10, DateOnly.MinValue));
}
