# OBR em C# para sBotics

Codigo para testes de robo seguidor de linha no sBotics. O repositorio esta organizado principalmente em exemplos dentro de `examples/`.

## Componentes atuais

### Motores

Os exemplos atuais usam quatro servomotores com estes nomes:

- `motor_esquerdo_frontal`: motor dianteiro esquerdo.
- `motor_esquerdo_traseiro`: motor traseiro esquerdo.
- `motor_direito_frontal`: motor dianteiro direito.
- `motor_direito_traseiro`: motor traseiro direito.

Todos os exemplos de movimento usam a funcao `Mover(esquerda, direita)`, aplicando a mesma velocidade nos dois motores do mesmo lado.

### Sensores do follow line simples

O exemplo principal de linha simples usa cinco sensores IR/cor:

- `sensor_IR_esquerda_externo`: sensor externo esquerdo, usado para curva/saida critica.
- `sensor_IR_esquerda_interno`: sensor interno esquerdo, usado para ajuste fino.
- `sensor_IR_meio`: sensor central, referencia principal. Quando ele ve preto, o robo esta alinhado.
- `sensor_IR_direita_interno`: sensor interno direito, usado para ajuste fino.
- `sensor_IR_direita_externo`: sensor externo direito, usado para curva/saida critica.