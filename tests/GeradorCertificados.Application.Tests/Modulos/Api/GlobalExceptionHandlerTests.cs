using System.Text;
using GeradorCertificados.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeradorCertificados.Application.Tests.Modulos.Api;

[TestClass]
public sealed class GlobalExceptionHandlerTests
{
    [TestMethod]
    public async Task TryHandleAsync_ComExcecaoInesperada_Retorna500SemDetalhesInternos()
    {
        var logger = new CapturingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var responseBody = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/auth/cadastro";
        context.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("connection string super-secret"),
            CancellationToken.None);

        responseBody.Position = 0;
        var body = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();

        Assert.IsTrue(handled);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        StringAssert.Contains(body, "Erro interno do servidor.");
        Assert.IsFalse(body.Contains("connection string super-secret", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(logger.ExceptionLogged);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public bool ExceptionLogged { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error && exception is not null)
                ExceptionLogged = true;
        }
    }
}