// ===================== Declarando Motores =====================
const string MOTOR_ESQUERDO_TRASEIRO = "motor_esquerdo_traseiro";
const string MOTOR_DIREITO_TRASEIRO = "motor_direito_traseiro";
const string MOTOR_DIREITO_FRONTAL = "motor_direito_frontal";
const string MOTOR_ESQUERDO_FRONTAL = "motor_esquerdo_frontal";

// ===================== Declarando Sensores =====================
const string SENSOR_ESQUERDO_INTERNO = "sensor_IR_esquerda_interno";
const string SENSOR_ESQUERDO_EXTERNO = "sensor_IR_esquerda_externo";
const string SENSOR_MEIO = "sensor_IR_meio";
const string SENSOR_DIREITO_INTERNO = "sensor_IR_direita_interno";
const string SENSOR_DIREITO_EXTERNO = "sensor_IR_direita_externo";

// ===================== Declarando Variáveis Básicas =====================
const double FORCA_MOTOR = 1000; // Torque dos motores
const double BRILHO_MAXIMO_PRETO = 45; // Valor do brilho máximo para ser considerado preto (menor, mais rígido)
const double BRILHO_MINIMO_VERDE = 20; // Valor do brilho mínimo pra ser considerado verde
const double MARGEM_VERDE = 12;

// ===================== Declarando Velocidades =====================
const double VELOCIDADE_FRENTE = 200; // Velocidade dde frente
const double VELOCIDADE_AJUSTE_LEVE = 2400; // Velocidade do ajuste leve
const double VELOCIDADE_LENTA_LEVE = -450;
const double VELOCIDADE_AJUSTE_FORTE = 4200;
const double VELOCIDADE_LENTA_FORTE = -950;
const double VELOCIDADE_VERDE_90 = 1500;
const double VELOCIDADE_VERDE_LENTA = -650;
const double VELOCIDADE_GIRO_VERDE_PARADO = 1500;
const double ANGULO_GIRO_VERDE_GRAUS = 95;

// ===================== Declarando Tempos =====================
const double TEMPO_FRENTE_MS = 60;
const double TEMPO_CEGO_ANTES_CURVA_VERDE_MS = 1000; // Tempo para o robô andar além da linha e virar alinhado com ela
const double TEMPO_BLOQUEIO_90_SEM_VERDE_MS = 200; // Tempo para que o robô fique "cego" de forma temporária para não ficar em loop na curva
const double TEMPO_PULSO_AJUSTE_LEVE_MS = 90;
const double TEMPO_PULSO_AJUSTE_FORTE_MS = 180;
const double TEMPO_CURVA_VERDE_90_MS = 500;
const double TEMPO_SAIDA_VERDE_MS = 320;
const double TEMPO_CEGO_APOS_AJUSTE_MS = 70;
const double TEMPO_ESTABILIZAR_CURVA_MS = 60;
const double TEMPO_PONTE_BRANCO_MS = 260;
const double TEMPO_RE_PONTE_BRANCO_MS = 90;
const double TEMPO_BRANCO_PARA_PONTE_MS = 2000;
const double TEMPO_MEMORIA_VERDE_MS = 450;
const double VELOCIDADE_PONTE_BRANCO = 120;
const double VELOCIDADE_SAIDA_VERDE = 170;
const double INTERVALO_SENSOR_MEIO_MS = 1;
const double TEMPO_MAX_GIRO_VERDE_MS = 1750;

string ultimoEstado = "";
int ultimaDirecao = 0; // -1 esquerda, 1 direita.
double inicioTodosBrancosMs = 0;
double ultimoVerdeEsquerdaMs = 0;
double ultimoVerdeDireitaMs = 0;
double ultimoDeteccao90SemVerdeMs = 0;

double AgoraMs() {
    return Time.Timestamp * 1000.0;
}

double Abs(double valor) {
    if (valor < 0) return -valor;
    return valor;
}

double NormalizarAngulo(double angulo) {
    while (angulo < 0) angulo += 360;
    while (angulo >= 360) angulo -= 360;
    return angulo;
}

double DiferencaAngular(double origem, double atual) {
    double diferenca = Abs(NormalizarAngulo(atual) - NormalizarAngulo(origem));
    if (diferenca > 180) diferenca = 360 - diferenca;
    return diferenca;
}

bool Se_Preto(Color cor) {
    return (cor.Closest() == Colors.Black) || (cor.Brightness <= BRILHO_MAXIMO_PRETO);
}

bool Se_Verde(Color cor) {
    return cor.Closest() == Colors.Green
        || (cor.Green >= BRILHO_MINIMO_VERDE && cor.Green > (cor.Red + MARGEM_VERDE) && cor.Green > (cor.Blue + MARGEM_VERDE));
}

string NomeCor(bool preto) {
    if (preto) return "PRETO";
    return "BRANCO";
}

void ImprimirEstado(bool pretoEsquerdaInterno, bool pretoEsquerdaExterno, bool pretoMeio, bool pretoDireitaInterno, bool pretoDireitaExterno, string modo) {
    string estado = modo
        + " | EI=" + NomeCor(pretoEsquerdaInterno)
        + " EE=" + NomeCor(pretoEsquerdaExterno)
        + " M=" + NomeCor(pretoMeio)
        + " DI=" + NomeCor(pretoDireitaInterno)
        + " DE=" + NomeCor(pretoDireitaExterno);
    if (estado == ultimoEstado) return;
    ultimoEstado = estado;
    IO.PrintLine(estado);
}

void LigarMotor(string motor, double velocidade) {
    Bot.GetComponent<Servomotor>(motor).Locked = false;
    Bot.GetComponent<Servomotor>(motor).Apply(FORCA_MOTOR, velocidade);
}

void Mover(double velocidadeEsquerda, double velocidadeDireita) {
    // Mapeamento invertido para corrigir direita/esquerda espelhados no robo.
    LigarMotor(MOTOR_ESQUERDO_FRONTAL, velocidadeDireita);
    LigarMotor(MOTOR_ESQUERDO_TRASEIRO, velocidadeDireita);
    LigarMotor(MOTOR_DIREITO_FRONTAL, velocidadeEsquerda);
    LigarMotor(MOTOR_DIREITO_TRASEIRO, velocidadeEsquerda);
}

Color LerCor(string sensor) {
    return Bot.GetComponent<ColorSensor>(sensor).Analog;
}

bool PretoEsquerdaInterno() {
    return Se_Preto(LerCor(SENSOR_ESQUERDO_INTERNO));
}

bool PretoEsquerdaExterno() {
    return Se_Preto(LerCor(SENSOR_ESQUERDO_EXTERNO));
}

bool PretoMeio() {
    return Se_Preto(LerCor(SENSOR_MEIO));
}

bool PretoDireitaInterno() {
    return Se_Preto(LerCor(SENSOR_DIREITO_INTERNO));
}

bool PretoDireitaExterno() {
    return Se_Preto(LerCor(SENSOR_DIREITO_EXTERNO));
}

bool VerdeEsquerda() {
    return Se_Verde(LerCor(SENSOR_ESQUERDO_INTERNO)) || Se_Verde(LerCor(SENSOR_ESQUERDO_EXTERNO));
}

bool VerdeDireita() {
    return Se_Verde(LerCor(SENSOR_DIREITO_INTERNO)) || Se_Verde(LerCor(SENSOR_DIREITO_EXTERNO));
}

async Task Frente() {
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_FRENTE_MS);
}

async Task EstabilizarDepoisDaCurva() {
    Mover(VELOCIDADE_FRENTE * 0.65, VELOCIDADE_FRENTE * 0.65);
    await Time.Delay(TEMPO_ESTABILIZAR_CURVA_MS);
}

async Task SairDaMarcaSe_Verde() {
    Mover(VELOCIDADE_SAIDA_VERDE, VELOCIDADE_SAIDA_VERDE);
    await Time.Delay(TEMPO_SAIDA_VERDE_MS);
}

void LimparMemoriaSe_Verde() {
    ultimoVerdeEsquerdaMs = 0;
    ultimoVerdeDireitaMs = 0;
    inicioTodosBrancosMs = 0;
}

async Task<bool> AguardarOuMeioSe_Preto(double tempoMs) {
    double tempoPassadoMs = 0;

    while (tempoPassadoMs < tempoMs) {
        if (PretoMeio()) return true;
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
        tempoPassadoMs += INTERVALO_SENSOR_MEIO_MS;
    }

    return PretoMeio();
}

async Task AvancarCegoAposAjuste() {
    if (PretoMeio()) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }

    Mover(VELOCIDADE_FRENTE * 0.7, VELOCIDADE_FRENTE * 0.7);
    await AguardarOuMeioSe_Preto(TEMPO_CEGO_APOS_AJUSTE_MS);
}

bool AlgumSensorSe_Preto() {
    return PretoMeio()
        || PretoEsquerdaInterno()
        || PretoEsquerdaExterno()
        || PretoDireitaInterno()
        || PretoDireitaExterno();
}

async Task<bool> AguardarQualquerSe_Preto(double tempoMs) {
    double tempoPassadoMs = 0;

    while (tempoPassadoMs < tempoMs) {
        if (AlgumSensorSe_Preto()) return true;
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
        tempoPassadoMs += INTERVALO_SENSOR_MEIO_MS;
    }

    return AlgumSensorSe_Preto();
}

void MoverPonteBranco(double direcao) {
    if (ultimaDirecao < 0) {
        Mover(direcao * VELOCIDADE_PONTE_BRANCO * 0.65, direcao * VELOCIDADE_PONTE_BRANCO);
    } else if (ultimaDirecao > 0) {
        Mover(direcao * VELOCIDADE_PONTE_BRANCO, direcao * VELOCIDADE_PONTE_BRANCO * 0.65);
    } else {
        Mover(direcao * VELOCIDADE_PONTE_BRANCO, direcao * VELOCIDADE_PONTE_BRANCO);
    }
}

async Task<bool> TentarAtravessarFalhaDaLinha() {
    MoverPonteBranco(1);
    if (await AguardarQualquerSe_Preto(TEMPO_PONTE_BRANCO_MS)) return true;

    MoverPonteBranco(-1);
    if (await AguardarQualquerSe_Preto(TEMPO_RE_PONTE_BRANCO_MS)) return true;

    return false;
}

async Task ManterDirecaoEmBranco() {
    MoverPonteBranco(1);
    await Time.Delay(TEMPO_FRENTE_MS);
}

async Task CurvaVerdeEsquerda() {
    LimparMemoriaSe_Verde();
    ultimaDirecao = -1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0);
    await Time.Delay(20);

    double inicioMs = AgoraMs();
    double anguloInicio = Bot.Compass;
    Mover(VELOCIDADE_GIRO_VERDE_PARADO, -VELOCIDADE_GIRO_VERDE_PARADO);

    while (DiferencaAngular(anguloInicio, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS
        && (AgoraMs() - inicioMs) < TEMPO_MAX_GIRO_VERDE_MS) {
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    }

    Mover(0, 0);
}

async Task CurvaVerdeDireita() {
    LimparMemoriaSe_Verde();
    ultimaDirecao = 1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0);
    await Time.Delay(20);

    double inicioMs = AgoraMs();
    double anguloInicio = Bot.Compass;
    Mover(-VELOCIDADE_GIRO_VERDE_PARADO, VELOCIDADE_GIRO_VERDE_PARADO);

    while (DiferencaAngular(anguloInicio, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS
        && (AgoraMs() - inicioMs) < TEMPO_MAX_GIRO_VERDE_MS) {
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    }

    Mover(0, 0);
}

async Task Curva90SemVerdeEsquerda() {
    ultimoDeteccao90SemVerdeMs = AgoraMs();
    inicioTodosBrancosMs = 0;
    ultimaDirecao = -1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0);
    await Time.Delay(20);

    double inicioMs = AgoraMs();
    double anguloInicio = Bot.Compass;
    Mover(VELOCIDADE_GIRO_VERDE_PARADO, -VELOCIDADE_GIRO_VERDE_PARADO);

    while (DiferencaAngular(anguloInicio, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS
        && (AgoraMs() - inicioMs) < TEMPO_MAX_GIRO_VERDE_MS) {
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    }

    Mover(0, 0);
}

async Task Curva90SemVerdeDireita() {
    ultimoDeteccao90SemVerdeMs = AgoraMs();
    inicioTodosBrancosMs = 0;
    ultimaDirecao = 1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0);
    await Time.Delay(20);

    double inicioMs = AgoraMs();
    double anguloInicio = Bot.Compass;
    Mover(-VELOCIDADE_GIRO_VERDE_PARADO, VELOCIDADE_GIRO_VERDE_PARADO);

    while (DiferencaAngular(anguloInicio, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS
        && (AgoraMs() - inicioMs) < TEMPO_MAX_GIRO_VERDE_MS) {
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    }

    Mover(0, 0);
}

async Task AjustarDireitaInterno() {
    ultimaDirecao = 1;
    Mover(VELOCIDADE_LENTA_LEVE, VELOCIDADE_AJUSTE_LEVE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_LEVE_MS)) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }
    await AvancarCegoAposAjuste();
}

async Task AjustarEsquerdaInterno() {
    ultimaDirecao = -1;
    Mover(VELOCIDADE_AJUSTE_LEVE, VELOCIDADE_LENTA_LEVE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_LEVE_MS)) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }
    await AvancarCegoAposAjuste();
}

async Task AjustarDireitaExterno() {
    ultimaDirecao = 1;
    Mover(VELOCIDADE_LENTA_FORTE, VELOCIDADE_AJUSTE_FORTE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_FORTE_MS)) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }
    await AvancarCegoAposAjuste();
}

async Task AjustarEsquerdaExterno() {
    ultimaDirecao = -1;
    Mover(VELOCIDADE_AJUSTE_FORTE, VELOCIDADE_LENTA_FORTE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_FORTE_MS)) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }
    await AvancarCegoAposAjuste();
}

async Task Main() {
    IO.OpenConsole();

    while (true) {
        bool pretoEsquerdaInterno = PretoEsquerdaInterno();
        bool pretoEsquerdaExterno = PretoEsquerdaExterno();
        bool pretoMeio = PretoMeio();
        bool pretoDireitaInterno = PretoDireitaInterno();
        bool pretoDireitaExterno = PretoDireitaExterno();
        bool verdeEsquerda = VerdeEsquerda();
        bool verdeDireita = VerdeDireita();
        bool pretoEsquerda = pretoEsquerdaInterno || pretoEsquerdaExterno;
        bool pretoDireita = pretoDireitaInterno || pretoDireitaExterno;
        bool todosBrancos = !pretoMeio && !pretoDireita && !pretoEsquerda;

        if (verdeEsquerda) ultimoVerdeEsquerdaMs = AgoraMs();
        if (verdeDireita) ultimoVerdeDireitaMs = AgoraMs();

        bool verdeEsquerdaRecente = ultimoVerdeEsquerdaMs > 0 && (AgoraMs() - ultimoVerdeEsquerdaMs) <= TEMPO_MEMORIA_VERDE_MS;
        bool verdeDireitaRecente = ultimoVerdeDireitaMs > 0 && (AgoraMs() - ultimoVerdeDireitaMs) <= TEMPO_MEMORIA_VERDE_MS;
        bool verdeEsquerdaAtivo = verdeEsquerda || verdeEsquerdaRecente;
        bool verdeDireitaAtivo = verdeDireita || verdeDireitaRecente;
        bool bloqueio90SemVerdeAtivo = ultimoDeteccao90SemVerdeMs > 0
            && (AgoraMs() - ultimoDeteccao90SemVerdeMs) <= TEMPO_BLOQUEIO_90_SEM_VERDE_MS;
        bool curva90SemVerdeEsquerda = !bloqueio90SemVerdeAtivo
            && !verdeEsquerdaAtivo
            && !verdeDireitaAtivo
            && pretoMeio
            && pretoEsquerdaInterno
            && pretoEsquerdaExterno
            && !pretoDireitaInterno
            && !pretoDireitaExterno;
        bool curva90SemVerdeDireita = !bloqueio90SemVerdeAtivo
            && !verdeEsquerdaAtivo
            && !verdeDireitaAtivo
            && pretoMeio
            && !pretoEsquerdaInterno
            && !pretoEsquerdaExterno
            && pretoDireitaInterno
            && pretoDireitaExterno;

        if (todosBrancos) {
            if (inicioTodosBrancosMs == 0) inicioTodosBrancosMs = AgoraMs();
        } else {
            inicioTodosBrancosMs = 0;
        }

        if (verdeEsquerdaAtivo && !verdeDireitaAtivo) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "VERDE / ESQUERDA");
            await CurvaVerdeEsquerda();
        } else if (!verdeEsquerdaAtivo && verdeDireitaAtivo) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "VERDE / DIREITA");
            await CurvaVerdeDireita();
        } else if (curva90SemVerdeEsquerda) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "90 SEM VERDE / ESQUERDA");
            await Curva90SemVerdeEsquerda();
        } else if (curva90SemVerdeDireita) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "90 SEM VERDE / DIREITA");
            await Curva90SemVerdeDireita();
        } else if (pretoMeio) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO / FRENTE");
            await Frente();
        } else if (!pretoMeio && pretoDireitaInterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO BRANCO / DIREITA LEVE");
            await AjustarDireitaInterno();
        } else if (!pretoMeio && pretoEsquerdaInterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO BRANCO / ESQUERDA LEVE");
            await AjustarEsquerdaInterno();
        } else if (todosBrancos && (AgoraMs() - inicioTodosBrancosMs) > TEMPO_BRANCO_PARA_PONTE_MS && await TentarAtravessarFalhaDaLinha()) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "PONTE BRANCO / ACHOU");
        } else if (todosBrancos && (AgoraMs() - inicioTodosBrancosMs) > TEMPO_BRANCO_PARA_PONTE_MS) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "PONTE BRANCO / RECUOU");
            await ManterDirecaoEmBranco();
        } else if (todosBrancos) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "BRANCO / MANTENDO DIRECAO");
            await ManterDirecaoEmBranco();
        } else if (pretoDireita && pretoEsquerda) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "CRUZAMENTO / FRENTE");
            await Frente();
        } else if (pretoDireitaExterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "DIREITA FORTE");
            await AjustarDireitaExterno();
        } else if (pretoEsquerdaExterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "ESQUERDA FORTE");
            await AjustarEsquerdaExterno();
        }

        await Time.Delay(TEMPO_FRENTE_MS);
    }
}
