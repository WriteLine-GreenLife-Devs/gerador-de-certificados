# Gerador de Certificados Online

API para cadastro de usuários e cursos, solicitação de certificados, geração de PDFs e download de ZIPs.

## Estrutura

- `GeradorCertificados.Api`: controllers, contratos HTTP e configurações da API.
- `GeradorCertificados.Application`: casos de uso, serviços, handlers e interfaces.
- `GeradorCertificados.Domain`: entidades e regras de negócio.
- `GeradorCertificados.Infrastructure`: persistência, autenticação, arquivos, PDF/ZIP e integrações externas.
- `tests`: testes automatizados.

Cada módulo deve manter suas regras no `Domain`, seus casos de uso no `Application`, detalhes técnicos no `Infrastructure` e endpoints/contratos no `Api`.

## Executar

```powershell
dotnet restore
dotnet run --project src/GeradorCertificados.Api
```
