# OBR em C# para sBotics

Projeto pronto para uso em C# no sBotics com:

- seguidor de linha robusto (reta, ondulada, pontilhada)
- recuperacao de linha em gap/meio circulo
- desvio de parede com 3 ultrassonicos (frontal + 2 laterais)
- sala de resgate (preta -> vermelho, branca/prata -> verde)
- suporte de camera com maquina de estado inspirada em `tests/detector_pista.py`
- tracao 4 motores com fallback automatico para 2 motores

## Arquivo principal

- `obr_line.cs`

## Arquivos espelho

- `examples/example_sensors_on.cs`
- `examples/example_motors_on.cs`

## Como usar no sBotics

1. Abra o menu Programacao.
2. Escolha linguagem C#.
3. Apague todo o conteudo atual do editor (`Ctrl+A`, `Delete`).
4. Cole o conteudo de `obr_line.cs`.
5. Compile e rode.

## Componentes esperados (nomes padrao)

- `LEFT_FRONT_MOTOR = "lf"`
- `RIGHT_FRONT_MOTOR = "rf"`
- `LEFT_REAR_MOTOR = "lb"` (opcional)
- `RIGHT_REAR_MOTOR = "rb"` (opcional)
- `LEFT_LINE_SENSOR = "sl"` (sensor de linha da esquerda)
- `RIGHT_LINE_SENSOR = "sr"` (sensor de linha da direita)
- `FRONT_LINE_SENSOR = "fc"` (opcional)
- `BALL_COLOR_SENSOR = "bc"` (opcional)
- `FRONT_ULTRA_SENSOR = "ultrassonico"` (opcional)
- `LEFT_WALL_ULTRA_SENSOR = "ultrassonico_esquerda"` (opcional)
- `RIGHT_WALL_ULTRA_SENSOR = "ultrassonico_direita"` (opcional)
- `CAMERA_SENSOR = "camera"` (opcional)
- `STATUS_LED = "led"` (opcional)

Observacoes:

- o script tenta resolver aliases automaticamente (ex.: `l/r/l2/r2`, `lc/rc`, `us`, `cam`) para facilitar migracao
- se seu sensor esquerdo de linha nao for `sl`, altere `LEFT_LINE_SENSOR` no topo do `obr_line.cs`

## Parametros para calibracao

- `BASE_SPEED`
- `MAX_TURN`
- `BLACK_BRIGHTNESS_MAX`
- `DOTTED_LINE_GRACE_MS`
- `SEARCH_MAX_MS`
- `WALL_NEAR_THRESHOLD`
- `WALL_CRITICAL_THRESHOLD`
- `REAR_MOTOR_FACTOR`
- `RESCUE_TIMEOUT_MS`
- `ENABLE_CAMERA_STOP_BEHAVIOR` (padrao `false`)

## Regras de compatibilidade C# no sBotics (importante)

Para evitar erros de parser no sBotics:

- nao use identificadores contendo `try` em nenhum lugar do nome
- evite `async Task<T>`; prefira `async Task` + flags de estado
- mantenha nomes simples em ASCII

## Troubleshooting rapido

Se aparecer `Erro: uso da palavra reservada "try"`:

1. Confirme que esta usando `obr_line.cs` atualizado.
2. No editor sBotics, busque por `try` e por `Task<`.
3. Se achar, remova/renomeie.
4. Apague tudo e cole novamente o arquivo principal.

## Estrutura do projeto

- `obr_line.cs` -> script principal para competicao
- `examples/` -> copias espelho para teste rapido
- `tests/detector_pista.py` -> referencia de visao computacional
