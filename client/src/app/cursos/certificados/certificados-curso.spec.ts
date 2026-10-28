import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { CursoDto } from '../cursos.models';
import { CursosService } from '../cursos.service';
import { StatusProcessamentoDto } from './certificados.models';
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
  let obterStatus: ReturnType<typeof vi.fn>;
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

  function criarTela(statusInicial = 'Concluido'): void {
    download = new Subject<Blob>();
    statusResponses = [of(criarStatus(statusInicial))];
    obterStatus = vi.fn(() => statusResponses.shift() ?? of(criarStatus(statusInicial)));
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
            listar: vi.fn(() => of([])),
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
});
