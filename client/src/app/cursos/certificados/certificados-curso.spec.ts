import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { CursoDto } from '../cursos.models';
import { CursosService } from '../cursos.service';
import { CertificadoDto, StatusProcessamentoDto } from './certificados.models';
import { CertificadosService } from './certificados.service';
import { CertificadosCurso } from './certificados-curso';

describe('CertificadosCurso download', () => {
  const cursoId = 'curso-123';
  const curso: CursoDto = {
    id: cursoId,
    nome: 'Curso de teste',
    cargaHoraria: 20,
    dataConclusao: '2026-10-28',
  };
  let fixture: ComponentFixture<CertificadosCurso>;
  let download: Subject<Blob>;
  let downloadDescriptor: PropertyDescriptor | undefined;
  let revokeDescriptor: PropertyDescriptor | undefined;
  let createObjectUrl: ReturnType<typeof vi.fn>;
  let revokeObjectUrl: ReturnType<typeof vi.fn>;
  let clickAnchor: ReturnType<typeof vi.spyOn>;
  let statusResponses: Observable<StatusProcessamentoDto>[];
  let certificadosResponses: Observable<readonly CertificadoDto[]>[];
  let obterStatus: ReturnType<typeof vi.fn>;
  let listar: ReturnType<typeof vi.fn>;
  let baixar: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    downloadDescriptor = Object.getOwnPropertyDescriptor(URL, 'createObjectURL');
    revokeDescriptor = Object.getOwnPropertyDescriptor(URL, 'revokeObjectURL');
    createObjectUrl = vi.fn(() => 'blob:certificados');
    revokeObjectUrl = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: createObjectUrl,
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: revokeObjectUrl,
    });
    clickAnchor = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
  });

  afterEach(() => {
    fixture?.destroy();
    vi.restoreAllMocks();
    vi.useRealTimers();
    if (downloadDescriptor) {
      Object.defineProperty(URL, 'createObjectURL', downloadDescriptor);
    } else {
      Reflect.deleteProperty(URL, 'createObjectURL');
    }
    if (revokeDescriptor) {
      Object.defineProperty(URL, 'revokeObjectURL', revokeDescriptor);
    } else {
      Reflect.deleteProperty(URL, 'revokeObjectURL');
    }
  });

  function criarTela(
    statusInicial = 'Concluido',
    primeiraLista: Observable<readonly CertificadoDto[]> = of([]),
  ): void {
    download = new Subject<Blob>();
    statusResponses = [of(criarStatus(statusInicial))];
    certificadosResponses = [primeiraLista];
    obterStatus = vi.fn(() => statusResponses.shift() ?? of(criarStatus(statusInicial)));
    listar = vi.fn(() => certificadosResponses.shift() ?? of([]));
    baixar = vi.fn(() => download.asObservable());

    TestBed.configureTestingModule({
      imports: [CertificadosCurso],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => cursoId } } },
        },
        {
          provide: CursosService,
          useValue: { obterPorId: vi.fn(() => of(curso)) },
        },
        {
          provide: CertificadosService,
          useValue: {
            obterStatus,
            listar,
            baixar,
            solicitar: vi.fn(),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(CertificadosCurso);
    fixture.detectChanges();
  }

  function criarStatus(status: string): StatusProcessamentoDto {
    return {
      solicitacaoId: 'solicitacao-123',
      cursoId,
      status,
      dataSolicitacao: '2026-10-28T12:00:00Z',
      quantidadeTotal: 1,
      quantidadeGerada: 1,
      quantidadeFalha: 0,
    };
  }

  function botaoDownload(): HTMLButtonElement {
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    );
    const button = buttons.find((item) =>
      item.textContent?.includes('Baixar ZIP') || item.textContent?.includes('Baixando ZIP'),
    );

    if (!(button instanceof HTMLButtonElement)) {
      throw new Error('Botão de download não encontrado.');
    }

    return button;
  }

  function iniciarDownload(): void {
    botaoDownload().click();
    fixture.detectChanges();
  }

  async function concluirTratamentoDoErro(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
    fixture.detectChanges();
  }

  it('inicia o download quando a API retorna o Blob ZIP', () => {
    criarTela();
    const arquivo = new Blob(['zip-data'], { type: 'application/zip' });

    iniciarDownload();
    download.next(arquivo);
    download.complete();
    fixture.detectChanges();

    expect(createObjectUrl).toHaveBeenCalledWith(arquivo);
    expect(clickAnchor).toHaveBeenCalledOnce();
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:certificados');
    expect(botaoDownload().disabled).toBe(false);
  });

  it('informa que o ZIP ainda não está pronto para HTTP 409', async () => {
    criarTela();
    baixar.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );

    iniciarDownload();
    await concluirTratamentoDoErro();

    expect(fixture.nativeElement.textContent).toContain('O ZIP ainda não está pronto');
    expect(botaoDownload().disabled).toBe(false);
  });

  it('informa que o ZIP não foi encontrado para HTTP 404', async () => {
    criarTela();
    baixar.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );

    iniciarDownload();
    await concluirTratamentoDoErro();

    expect(fixture.nativeElement.textContent).toContain('O ZIP não foi encontrado');
    expect(botaoDownload().disabled).toBe(false);
  });

  it('diferencia HTTP 409 quando o processamento está em Falha', async () => {
    criarTela();
    statusResponses.push(of(criarStatus('Falha')));

    iniciarDownload();
    const atualizarStatus = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Atualizar status'));
    atualizarStatus?.click();
    fixture.detectChanges();

    download.error(new HttpErrorResponse({ status: 409 }));
    await concluirTratamentoDoErro();

    expect(fixture.nativeElement.textContent).toContain('O processamento dos certificados falhou');
    expect(botaoDownload().disabled).toBe(true);
  });

  it('interpreta Problem Details recebido como Blob', async () => {
    criarTela();
    const detalhes = new Blob(
      [JSON.stringify({ title: 'Erro', detail: 'Falha ao preparar o arquivo.' })],
      { type: 'application/problem+json' },
    );
    const lerDetalhes = vi.fn().mockResolvedValue(
      JSON.stringify({ title: 'Erro', detail: 'Falha ao preparar o arquivo.' }),
    );
    Object.defineProperty(detalhes, 'text', { configurable: true, value: lerDetalhes });
    baixar.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: 500, error: detalhes })),
    );
    iniciarDownload();
    await concluirTratamentoDoErro();

    expect(lerDetalhes).toHaveBeenCalledOnce();
    expect(fixture.nativeElement.textContent).toContain('Falha ao preparar o arquivo.');
    expect(botaoDownload().disabled).toBe(false);
  });

  it('mantém o botão desabilitado antes de Concluido', () => {
    criarTela('GerandoCertificados');

    expect(botaoDownload().disabled).toBe(true);
    expect(baixar).not.toHaveBeenCalled();
  });

  it('habilita o botão quando o processamento está Concluido', () => {
    criarTela('Concluido');

    expect(botaoDownload().disabled).toBe(false);
  });

  it('desabilita o botão enquanto o download está em andamento', () => {
    criarTela('Concluido');

    iniciarDownload();

    expect(botaoDownload().disabled).toBe(true);
    expect(botaoDownload().textContent).toContain('Baixando ZIP...');
  });

  it('exibe carregamento na consulta inicial e não durante atualização em segundo plano', () => {
    const consultaInicial = new Subject<readonly CertificadoDto[]>();
    criarTela('Concluido', consultaInicial);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Carregando certificados...');

    consultaInicial.next([criarCertificado('cert-1', 'Maria', 'Gerado')]);
    consultaInicial.complete();
    fixture.detectChanges();

    const consultaEmSegundoPlano = new Subject<readonly CertificadoDto[]>();
    certificadosResponses.push(consultaEmSegundoPlano);
    statusResponses.push(of(criarStatus('Concluido')));
    fixture.componentInstance['atualizarStatus']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Carregando certificados...');
    consultaEmSegundoPlano.complete();
  });

  it('exibe mensagem própria quando a API retorna uma lista vazia', () => {
    criarTela();

    expect(fixture.nativeElement.textContent).toContain('Ainda não há certificados para exibir.');
    expect(fixture.nativeElement.textContent).not.toContain('Não foi possível carregar os certificados');
  });

  it('trata 404 de solicitação inexistente como ausência normal', () => {
    criarTela();
    certificadosResponses.push(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 404,
            error: {
              title: 'Solicitação não encontrada',
              detail: 'Não existe solicitação para o curso curso-123.',
            },
          }),
      ),
    );
    fixture.componentInstance['carregarCertificados']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ainda não há certificados para exibir.');
    expect(fixture.nativeElement.textContent).not.toContain('Não foi possível carregar');
  });

  it('exibe mensagem amigável para erro inesperado da listagem', () => {
    criarTela();
    certificadosResponses.push(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    fixture.componentInstance['carregarCertificados']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Não foi possível carregar os certificados. Tente novamente.',
    );
  });

  it('preserva certificados carregados quando uma atualização falha', () => {
    criarTela();
    const certificados = [
      criarCertificado('cert-1', 'Maria', 'Gerado'),
      criarCertificado('cert-2', 'João', 'Pendente'),
    ];
    certificadosResponses.push(of(certificados));
    fixture.componentInstance['carregarCertificados']();
    fixture.detectChanges();

    certificadosResponses.push(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );
    fixture.componentInstance['carregarCertificados']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Maria');
    expect(fixture.nativeElement.textContent).toContain('João');
    expect(fixture.nativeElement.textContent).toContain(
      'Não foi possível carregar os certificados. Tente novamente.',
    );
  });

  it('mostra erro para 404 que não corresponde a solicitação inexistente', () => {
    criarTela();
    certificadosResponses.push(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 404,
            error: { title: 'Curso não encontrado', detail: 'O curso não existe.' },
          }),
      ),
    );
    fixture.componentInstance['carregarCertificados']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Não foi possível encontrar o curso para carregar os certificados.',
    );
    expect(fixture.nativeElement.textContent).not.toContain(
      'Ainda não há certificados para exibir.',
    );
  });

  it('exibe status individuais e data válida, sem exibir data ausente ou inválida', () => {
    criarTela();
    certificadosResponses.push(
      of([
        criarCertificado('cert-pendente', 'Ana', 'Pendente', null),
        criarCertificado('cert-gerado', 'Bruno', 'Gerado', '2026-10-28T12:00:00Z'),
        criarCertificado('cert-falha', 'Carla', 'Falha', null),
        criarCertificado('cert-data-invalida', 'Davi', 'Gerado', 'data-invalida'),
      ]),
    );
    fixture.componentInstance['carregarCertificados']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Pendente');
    expect(fixture.nativeElement.textContent).toContain('Gerado');
    expect(fixture.nativeElement.textContent).toContain('Falha');
    expect(fixture.nativeElement.textContent).toContain('Gerado em 28/10/2026');
    expect(fixture.nativeElement.textContent).not.toContain('Gerado em data-invalida');
    expect(fixture.nativeElement.textContent).toContain('Davi');
  });

  it('consulta status e atualiza certificados durante o processamento', async () => {
    vi.useFakeTimers();
    criarTela('GerandoCertificados');
    certificadosResponses.push(
      of([criarCertificado('cert-1', 'Maria', 'Pendente')]),
    );
    vi.advanceTimersByTime(2_000);
    await Promise.resolve();
    fixture.detectChanges();

    expect(obterStatus).toHaveBeenCalledTimes(2);
    expect(listar).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.textContent).toContain('Maria');
  });

  it('atualiza a listagem após o processamento ser concluído e encerra o polling', async () => {
    vi.useFakeTimers();
    criarTela('GerandoCertificados');
    statusResponses.push(of(criarStatus('Concluido')));
    certificadosResponses.push(
      of([criarCertificado('cert-1', 'Maria', 'Gerado')]),
    );
    vi.advanceTimersByTime(2_000);
    await Promise.resolve();
    fixture.detectChanges();

    const chamadasStatusAposConclusao = obterStatus.mock.calls.length;

    vi.advanceTimersByTime(6_000);

    expect(listar).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.textContent).toContain('Maria');
    expect(obterStatus).toHaveBeenCalledTimes(chamadasStatusAposConclusao);
  });

  it('serializa atualizações da lista e aplica a resposta mais recente', () => {
    const consultaInicial = new Subject<readonly CertificadoDto[]>();
    criarTela('GerandoCertificados', consultaInicial);
    certificadosResponses.push(
      of([criarCertificado('cert-atual', 'Resposta atual', 'Gerado')]),
    );

    fixture.componentInstance['carregarCertificados']();
    expect(listar).toHaveBeenCalledTimes(1);

    consultaInicial.next([criarCertificado('cert-antigo', 'Resposta antiga', 'Pendente')]);
    consultaInicial.complete();
    fixture.detectChanges();

    expect(listar).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.textContent).toContain('Resposta atual');
    expect(fixture.nativeElement.textContent).not.toContain('Resposta antiga');
  });

  it('processa a atualização de status pendente sem iniciar consultas concorrentes', () => {
    vi.useFakeTimers();
    criarTela('GerandoCertificados');
    const primeiraAtualizacao = new Subject<StatusProcessamentoDto>();
    statusResponses.push(primeiraAtualizacao);

    vi.advanceTimersByTime(2_000);
    expect(obterStatus).toHaveBeenCalledTimes(2);

    fixture.componentInstance['atualizarStatus']();
    fixture.componentInstance['atualizarStatus']();
    expect(obterStatus).toHaveBeenCalledTimes(2);

    primeiraAtualizacao.next(criarStatus('GerandoCertificados'));
    primeiraAtualizacao.complete();

    expect(obterStatus).toHaveBeenCalledTimes(3);
  });

  it('cancela o polling quando o componente é destruído', () => {
    vi.useFakeTimers();
    criarTela('GerandoCertificados');

    fixture.destroy();
    vi.advanceTimersByTime(6_000);

    expect(obterStatus).toHaveBeenCalledTimes(1);
  });

  function criarCertificado(
    id: string,
    nomeAluno: string,
    status: string,
    dataGeracao: string | null = null,
  ): CertificadoDto {
    return { id, nomeAluno, status, dataGeracao };
  }
});
