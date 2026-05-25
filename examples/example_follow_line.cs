const string MOTOR_ESQUERDO_TRASEIRO = "motor_esquerdo_traseiro";
const string MOTOR_DIREITO_TRASEIRO = "motor_direito_traseiro";
const string MOTOR_DIREITO_FRONTAL = "motor_direito_frontal";
const string MOTOR_ESQUERDO_FRONTAL = "motor_esquerdo_frontal";

const string SENSOR_ESQUERDO = "sensor_IR_esquerda";
const string SENSOR_DIREITO = "sensor_IR_direita";
const string SENSOR_TRASEIRO_ESQUERDO = "sensor_cor_esquerdo";
const string SENSOR_TRASEIRO_DIREITO = "sensor_cor_direito";

// =========== ajuste geral ===========
const double FORCA_MOTOR = 800; // Torque dos motores. Aumentar da mais forca; reduzir deixa mais fraco.
const double BRILHO_MAXIMO_PRETO = 10; // Limite para considerar preto. Aumentar aceita cinza; reduzir exige preto mais escuro.
const double INTERVALO_LEITURA_MS = 40; // Tempo normal entre leituras.

// =========== segue linha ===========
const double VELOCIDADE_RETA = 300; // Velocidade principal do robo.
const double VELOCIDADE_CORRECAO = 260; // Forca da correcao quando um sensor ve preto.
const double TEMPO_EXTRA_CORRECAO_MS = 30; // Tempo extra corrigindo depois que o sensor sai do preto.
const double TEMPO_AVANCO_CEGO_MS = 500; // Avanco inicial antes de ler sensores.

// =========== curva verde ===========
const double VELOCIDADE_DRIFT_90 = 150; // Forca da curva verde de 90 graus.
const double TEMPO_DRIFT_90_MS = 850; // Duracao da curva verde.

// =========== busca ===========
const double TEMPO_BRANCO_PARA_PROCURAR_MS = 5000; // Tempo em branco antes de procurar a linha.
const double VELOCIDADE_RE_BUSCA = -150; // Velocidade principal da re no samba.
const double TEMPO_SAMBA_MS = 240; // Tempo de cada lado do samba em re.
const double VELOCIDADE_GIRO_VERIFICACAO = 200; // Velocidade do lado rapido no giro de verificacao.
const double TEMPO_GIRO_TESTE_MS = 5000; // Duracao maxima do giro de verificacao.
const double TEMPO_MAXIMO_ALINHAR_MS = 2000; // Tempo maximo tentando alinhar na linha.
const double TEMPO_MAXIMO_TRAZER_LINHA_MS = 1600; // Tempo maximo trazendo a linha da traseira para frente.

double inicioBrancoMs = 0;
string ultimoEstadoSensores = "";
string ultimoEstadoModo = "";

double AgoraMs() {
    return Time.Timestamp * 1000.0;
}

bool EhPreto(Color cor) {
    return (cor.Closest() == Colors.Black) && (cor.Brightness <= BRILHO_MAXIMO_PRETO);
}

bool EhVerde(Color cor) {
    return cor.Closest() == Colors.Green;
}

string NomeCor(Color cor) {
    if (EhPreto(cor)) return "PRETO";
    if (EhVerde(cor)) return "VERDE";
    return "BRANCO";
}

void ImprimirSensores(Color corEsquerda, Color corDireita) {
    string estadoAtual = NomeCor(corEsquerda) + " / " + NomeCor(corDireita);
    if (estadoAtual == ultimoEstadoSensores) return;
    ultimoEstadoSensores = estadoAtual;
    IO.PrintLine("Follow line: " + estadoAtual);
}

void ImprimirModo(string estadoAtual) {
    if (estadoAtual == ultimoEstadoModo) return;
    ultimoEstadoModo = estadoAtual;
    IO.PrintLine("Modo: " + estadoAtual);
}

void LigarMotor(string motor, double velocidade) {
    Bot.GetComponent<Servomotor>(motor).Locked = false;
    Bot.GetComponent<Servomotor>(motor).Apply(FORCA_MOTOR, velocidade);
}

void Mover(double velocidadeEsquerda, double velocidadeDireita) {
    LigarMotor(MOTOR_ESQUERDO_FRONTAL, velocidadeEsquerda);
    LigarMotor(MOTOR_ESQUERDO_TRASEIRO, velocidadeEsquerda);
    LigarMotor(MOTOR_DIREITO_FRONTAL, velocidadeDireita);
    LigarMotor(MOTOR_DIREITO_TRASEIRO, velocidadeDireita);
}

Color LerCor(string sensor) {
    return Bot.GetComponent<ColorSensor>(sensor).Analog;
}

bool LinhaDianteiraPreta() {
    return EhPreto(LerCor(SENSOR_ESQUERDO)) || EhPreto(LerCor(SENSOR_DIREITO));
}

bool LinhaTraseiraPreta() {
    return EhPreto(LerCor(SENSOR_TRASEIRO_ESQUERDO)) || EhPreto(LerCor(SENSOR_TRASEIRO_DIREITO));
}

int DirecaoLinhaDianteira() {
    bool pretoEsquerda = EhPreto(LerCor(SENSOR_ESQUERDO));
    bool pretoDireita = EhPreto(LerCor(SENSOR_DIREITO));
    if (pretoEsquerda && !pretoDireita) return -1;
    if (!pretoEsquerda && pretoDireita) return 1;
    return 0;
}

int DirecaoLinhaTraseira() {
    bool pretoEsquerda = EhPreto(LerCor(SENSOR_TRASEIRO_ESQUERDO));
    bool pretoDireita = EhPreto(LerCor(SENSOR_TRASEIRO_DIREITO));
    if (pretoEsquerda && !pretoDireita) return -1;
    if (!pretoEsquerda && pretoDireita) return 1;
    return 0;
}

void PararMotores() {
    Mover(0, 0);
}

async Task DesfazerGiroVerificacao(double tempoGiroMs) {
    ImprimirModo("BUSCA: DESFAZENDO GIRO");
    double inicioDesfazerGiro = AgoraMs();
    double velocidadeLenta = VELOCIDADE_GIRO_VERIFICACAO * 0.45;

    Mover(VELOCIDADE_GIRO_VERIFICACAO, velocidadeLenta);
    while ((AgoraMs() - inicioDesfazerGiro) < tempoGiroMs) {
        await Time.Delay(0);
    }

    PararMotores();
}

async Task CurvaVerdeEsquerda() {
    ImprimirModo("CURVA VERDE ESQUERDA 90");
    Mover(VELOCIDADE_RETA * 0.25, VELOCIDADE_RETA * 0.25);
    await Time.Delay(120);
    Mover(-VELOCIDADE_DRIFT_90 * 0.8, VELOCIDADE_DRIFT_90);
    await Time.Delay(TEMPO_DRIFT_90_MS);
}

async Task CurvaVerdeDireita() {
    ImprimirModo("CURVA VERDE DIREITA 90");
    Mover(VELOCIDADE_RETA * 0.25, VELOCIDADE_RETA * 0.25);
    await Time.Delay(120);
    Mover(VELOCIDADE_DRIFT_90, -VELOCIDADE_DRIFT_90 * 0.8);
    await Time.Delay(TEMPO_DRIFT_90_MS);
}

async Task CorrigirParaEsquerda() {
    ImprimirModo("CORRIGINDO PARA ESQUERDA");
    Mover(-VELOCIDADE_CORRECAO * 0.45, VELOCIDADE_CORRECAO);

    while (EhPreto(LerCor(SENSOR_ESQUERDO)) && !EhPreto(LerCor(SENSOR_DIREITO))) {
        await Time.Delay(10);
    }

    await Time.Delay(TEMPO_EXTRA_CORRECAO_MS);
    Mover(VELOCIDADE_RETA, VELOCIDADE_RETA);
}

async Task CorrigirParaDireita() {
    ImprimirModo("CORRIGINDO PARA DIREITA");
    Mover(VELOCIDADE_CORRECAO, -VELOCIDADE_CORRECAO * 0.45);

    while (!EhPreto(LerCor(SENSOR_ESQUERDO)) && EhPreto(LerCor(SENSOR_DIREITO))) {
        await Time.Delay(10);
    }

    await Time.Delay(TEMPO_EXTRA_CORRECAO_MS);
    Mover(VELOCIDADE_RETA, VELOCIDADE_RETA);
}

async Task SeguirLinhaNormal() {
    Color corEsquerda = LerCor(SENSOR_ESQUERDO);
    Color corDireita = LerCor(SENSOR_DIREITO);
    ImprimirSensores(corEsquerda, corDireita);
    bool verdeEsquerda = EhVerde(corEsquerda);
    bool verdeDireita = EhVerde(corDireita);
    bool pretoEsquerda = EhPreto(corEsquerda);
    bool pretoDireita = EhPreto(corDireita);

    if (pretoEsquerda || pretoDireita || verdeEsquerda || verdeDireita) {
        inicioBrancoMs = AgoraMs();
    }

    if (verdeEsquerda && !verdeDireita) {
        await CurvaVerdeEsquerda();
    } else if (!verdeEsquerda && verdeDireita) {
        await CurvaVerdeDireita();
    } else if (pretoEsquerda && !pretoDireita) {
        await CorrigirParaEsquerda();
    } else if (!pretoEsquerda && pretoDireita) {
        await CorrigirParaDireita();
    } else {
        ImprimirModo("SEGUINDO LINHA NORMAL");
        Mover(VELOCIDADE_RETA, VELOCIDADE_RETA);
    }
}

async Task ProcurarLinha() {
    ImprimirModo("BUSCA INICIADA");
    bool achouLinhaNoGiro = false;

    Mover(VELOCIDADE_RETA * 0.25, VELOCIDADE_RETA * 0.25);
    await Time.Delay(120);

    ImprimirModo("BUSCA: GIRO DE VERIFICACAO");
    double inicioGiro = AgoraMs();
    Mover(VELOCIDADE_GIRO_VERIFICACAO * 0.45, VELOCIDADE_GIRO_VERIFICACAO);
    while ((AgoraMs() - inicioGiro) < TEMPO_GIRO_TESTE_MS) {
        if (LinhaDianteiraPreta()) {
            achouLinhaNoGiro = true;
            break;
        }
        await Time.Delay(0);
    }

    if (achouLinhaNoGiro) {
        ImprimirModo("BUSCA: LINHA ACHADA NO GIRO");
        PararMotores();
        await DesfazerGiroVerificacao(AgoraMs() - inicioGiro);
        ImprimirModo("SEGUINDO LINHA NORMAL");
        Mover(VELOCIDADE_RETA, VELOCIDADE_RETA);
        inicioBrancoMs = AgoraMs();
        return;
    }

    ImprimirModo("BUSCA: RE SAMBANDO");
    while (!LinhaDianteiraPreta() && !LinhaTraseiraPreta()) {
        Mover(VELOCIDADE_RE_BUSCA, VELOCIDADE_RE_BUSCA * 0.03);
        await Time.Delay(TEMPO_SAMBA_MS);
        if (LinhaDianteiraPreta() || LinhaTraseiraPreta()) break;
        Mover(VELOCIDADE_RE_BUSCA * 0.03, VELOCIDADE_RE_BUSCA);
        await Time.Delay(TEMPO_SAMBA_MS);
    }

    int direcaoDianteira = DirecaoLinhaDianteira();
    int direcaoTraseira = DirecaoLinhaTraseira();
    PararMotores();
    if (direcaoDianteira != 0 || LinhaDianteiraPreta()) {
        ImprimirModo("BUSCA: LINHA ACHADA NA FRENTE");
        await AlinharLinhaAposBusca(direcaoDianteira);
    } else {
        ImprimirModo("BUSCA: LINHA ACHADA NA RE");
        await TrazerLinhaDaReParaFrente(direcaoTraseira);
        await AlinharLinhaAposBusca(direcaoTraseira);
    }
}

async Task TrazerLinhaDaReParaFrente(int direcaoTraseira) {
    ImprimirModo("BUSCA: TRAZENDO LINHA PARA FRENTE");
    double inicioTrazerLinha = AgoraMs();
    double velocidadeTrazerLinha = VELOCIDADE_RETA * 0.4;

    while (!LinhaDianteiraPreta() && (AgoraMs() - inicioTrazerLinha) < TEMPO_MAXIMO_TRAZER_LINHA_MS) {
        if (direcaoTraseira < 0) {
            Mover(velocidadeTrazerLinha * 0.45, velocidadeTrazerLinha);
        } else if (direcaoTraseira > 0) {
            Mover(velocidadeTrazerLinha, velocidadeTrazerLinha * 0.45);
        } else {
            Mover(velocidadeTrazerLinha, velocidadeTrazerLinha);
        }

        await Time.Delay(INTERVALO_LEITURA_MS);
    }

    PararMotores();
}

async Task AlinharLinhaAposBusca(int direcaoPreferida) {
    ImprimirModo("BUSCA: ALINHANDO NA LINHA");
    double inicioAlinhamento = AgoraMs();
    double velocidadeAlinhar = VELOCIDADE_GIRO_VERIFICACAO * 0.75;

    while ((AgoraMs() - inicioAlinhamento) < TEMPO_MAXIMO_ALINHAR_MS) {
        Color corEsquerda = LerCor(SENSOR_ESQUERDO);
        Color corDireita = LerCor(SENSOR_DIREITO);
        ImprimirSensores(corEsquerda, corDireita);

        bool pretoEsquerda = EhPreto(corEsquerda);
        bool pretoDireita = EhPreto(corDireita);

        if (pretoEsquerda && pretoDireita) {
            break;
        }

        if (pretoEsquerda && !pretoDireita) {
            Mover(-velocidadeAlinhar, velocidadeAlinhar);
        } else if (!pretoEsquerda && pretoDireita) {
            Mover(velocidadeAlinhar, -velocidadeAlinhar);
        } else if (direcaoPreferida < 0) {
            Mover(-velocidadeAlinhar, velocidadeAlinhar);
        } else if (direcaoPreferida > 0) {
            Mover(velocidadeAlinhar, -velocidadeAlinhar);
        } else {
            Mover(VELOCIDADE_GIRO_VERIFICACAO, -VELOCIDADE_GIRO_VERIFICACAO);
        }

        await Time.Delay(INTERVALO_LEITURA_MS);
    }

    PararMotores();
    await Time.Delay(80);
    ImprimirModo("SEGUINDO LINHA NORMAL");
    Mover(VELOCIDADE_RETA, VELOCIDADE_RETA);
    inicioBrancoMs = AgoraMs();
}

async Task Main() {
    IO.OpenConsole();
    ImprimirModo("AVANCO INICIAL CEGO");

    Mover(VELOCIDADE_RETA, VELOCIDADE_RETA);
    await Time.Delay(TEMPO_AVANCO_CEGO_MS);
    inicioBrancoMs = AgoraMs();

    while (true) {
        await SeguirLinhaNormal();

        if ((AgoraMs() - inicioBrancoMs) > TEMPO_BRANCO_PARA_PROCURAR_MS) {
            await ProcurarLinha();
        }

        await Time.Delay(INTERVALO_LEITURA_MS);
    }
}
