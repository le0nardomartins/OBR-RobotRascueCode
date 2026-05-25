# OBR em C# para sBotics

Codigo principal: `obr_main.cs`.

## Componentes usados

- `lf`: motor dianteiro esquerdo.
- `rf`: motor dianteiro direito.
- `lb`: motor traseiro esquerdo.
- `rb`: motor traseiro direito.
- `sl`: sensor de linha/cor esquerdo.
- `sr`: sensor de linha/cor direito.
- `scbl`: sensor de cor traseiro esquerdo.
- `scbr`: sensor de cor traseiro direito.
- `ultf`: sensor frontal para parede/caixa.
- `ultl`: sensor lateral esquerdo.
- `ultr`: sensor lateral direito.
- `ultrampa`: sensor inclinado para o chao para detectar rampa.
- `cam`: camera instalada, apenas diagnosticada no terminal.
- `sc`: servo da camera, apenas diagnosticado no terminal.
- `led`: LED opcional de status.

## Terminal

Ao iniciar, antes de qualquer movimento, o codigo imprime a checagem de componentes:

- lista tudo que o simulador encontrou no robo;
- marca cada componente esperado como `OK`, `FALTOU OBRIGATORIO` ou `FALTOU OPCIONAL`;
- lista componentes sobrando no modelo;
- bloqueia o movimento se faltar `lf`, `rf`, `sl` ou `sr`.

O console imprime uma linha por segundo (`INTERVALO_LOG_TERMINAL_MS = 1000`) no formato:

```text
TEL linha=PRETO/BRANCO traseira=N/A/N/A ultf/ultl/ultr=9999/9999/9999 rampa=False(9999) cam=True sc=True motores=0/0/0/0 bussola=0
```

Campos:

- `linha`: cores dos sensores `sl/sr`.
- `traseira`: cores dos sensores `scbl/scbr`.
- `ultf/ultl/ultr`: distancias dos tres sensores.
- `rampa`: estado da rampa e leitura do `ultrampa`.
- `cam` e `sc`: mostram se a camera e o servo existem.
- `motores`: comandos finais `lf/rf/lb/rb`.
- `bussola`: valor atual da bussola.

## Rampa

O `ultrampa` tem alcance curto. A rampa fica ativa quando a leitura esta entre:

- `DISTANCIA_RAMPA_MINIMA = 1.0`
- `DISTANCIA_RAMPA_MAXIMA = 2.0`

O LED fica azul durante toda a rotina. Quando `rampaAtiva` fica `true`, os motores traseiros recebem tracao. Fora da rampa, `lb/rb` recebem comando `0` e funcionam como roda boba.

## Calibracao principal

- `VELOCIDADE_BASE`: velocidade em reta.
- `VELOCIDADE_CURVA`: velocidade reduzida em curva.
- `VELOCIDADE_GIRO_CURVA`: giro controlado em curva fechada.
- `BRILHO_MAXIMO_PRETO`: limite de brilho para considerar preto.
- `LIMITE_PAREDE_PROXIMA`: distancia para iniciar desvio de parede.
- `DISTANCIA_RAMPA_MAXIMA`: alcance maximo do sensor de rampa.
- `INTERVALO_LEITURA_SENSORES_MS`: intervalo entre medicoes de sensores.
- `INTERVALO_LOG_TERMINAL_MS`: intervalo entre prints do terminal.
