const string MOTOR_ESQUERDO_TRASEIRO = "motor_esquerdo_traseiro";
const string MOTOR_DIREITO_TRASEIRO = "motor_direito_traseiro";
const string MOTOR_DIREITO_FRONTAL = "motor_direito_frontal";
const string MOTOR_ESQUERDO_FRONTAL = "motor_esquerdo_frontal";

const string SENSOR_ESQUERDO_INTERNO = "sensor_IR_esquerda_interno";
const string SENSOR_ESQUERDO_EXTERNO = "sensor_IR_esquerda_externo";
const string SENSOR_MEIO = "sensor_IR_meio";
const string SENSOR_DIREITO_INTERNO = "sensor_IR_direita_interno";
const string SENSOR_DIREITO_EXTERNO = "sensor_IR_direita_externo";

const double FORCA_MOTOR = 800;
const double BRILHO_MAXIMO_PRETO = 45;
const double BRILHO_MINIMO_VERDE = 20;
const double MARGEM_VERDE = 12;

const double VELOCIDADE_FRENTE = 200;
const double VELOCIDADE_AJUSTE_LEVE = 2400;
const double VELOCIDADE_LENTA_LEVE = -450;
const double VELOCIDADE_AJUSTE_FORTE = 4200;
const double VELOCIDADE_LENTA_FORTE = -950;
const double VELOCIDADE_VERDE_90 = 1500;
const double VELOCIDADE_VERDE_LENTA = -650;

const double TEMPO_FRENTE_MS = 60;
const double TEMPO_PULSO_AJUSTE_LEVE_MS = 90;
const double TEMPO_PULSO_AJUSTE_FORTE_MS = 180;
const double TEMPO_CURVA_VERDE_90_MS = 620;
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

string ultimoEstado = "";
int ultimaDirecao = 0; // -1 esquerda, 1 direita.
double inicioTodosBrancosMs = 0;
double ultimoVerdeEsquerdaMs = 0;
double ultimoVerdeDireitaMs = 0;

double AgoraMs() {
    return Time.Timestamp * 1000.0;
}

bool EhPreto(Color cor) {
    return (cor.Closest() == Colors.Black) || (cor.Brightness <= BRILHO_MAXIMO_PRETO);
}

bool EhVerde(Color cor) {
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
    LigarMotor(MOTOR_ESQUERDO_FRONTAL, velocidadeEsquerda);
    LigarMotor(MOTOR_ESQUERDO_TRASEIRO, velocidadeEsquerda);
    LigarMotor(MOTOR_DIREITO_FRONTAL, velocidadeDireita);
    LigarMotor(MOTOR_DIREITO_TRASEIRO, velocidadeDireita);
}

Color LerCor(string sensor) {
    return Bot.GetComponent<ColorSensor>(sensor).Analog;
}

bool PretoEsquerdaInterno() {
    return EhPreto(LerCor(SENSOR_ESQUERDO_INTERNO));
}

bool PretoEsquerdaExterno() {
    return EhPreto(LerCor(SENSOR_ESQUERDO_EXTERNO));
}

bool PretoMeio() {
    return EhPreto(LerCor(SENSOR_MEIO));
}

bool PretoDireitaInterno() {
    return EhPreto(LerCor(SENSOR_DIREITO_INTERNO));
}

bool PretoDireitaExterno() {
    return EhPreto(LerCor(SENSOR_DIREITO_EXTERNO));
}

bool VerdeEsquerda() {
    return EhVerde(LerCor(SENSOR_ESQUERDO_INTERNO)) || EhVerde(LerCor(SENSOR_ESQUERDO_EXTERNO));
}

bool VerdeDireita() {
    return EhVerde(LerCor(SENSOR_DIREITO_INTERNO)) || EhVerde(LerCor(SENSOR_DIREITO_EXTERNO));
}

async Task Frente() {
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_FRENTE_MS);
}

async Task EstabilizarDepoisDaCurva() {
    Mover(VELOCIDADE_FRENTE * 0.65, VELOCIDADE_FRENTE * 0.65);
    await Time.Delay(TEMPO_ESTABILIZAR_CURVA_MS);
}

async Task SairDaMarcaVerde() {
    Mover(VELOCIDADE_SAIDA_VERDE, VELOCIDADE_SAIDA_VERDE);
    await Time.Delay(TEMPO_SAIDA_VERDE_MS);
}

void LimparMemoriaVerde() {
    ultimoVerdeEsquerdaMs = 0;
    ultimoVerdeDireitaMs = 0;
    inicioTodosBrancosMs = 0;
}

async Task<bool> AguardarOuMeioPreto(double tempoMs) {
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
    await AguardarOuMeioPreto(TEMPO_CEGO_APOS_AJUSTE_MS);
}

bool AlgumSensorPreto() {
    return PretoMeio()
        || PretoEsquerdaInterno()
        || PretoEsquerdaExterno()
        || PretoDireitaInterno()
        || PretoDireitaExterno();
}

async Task<bool> AguardarQualquerPreto(double tempoMs) {
    double tempoPassadoMs = 0;

    while (tempoPassadoMs < tempoMs) {
        if (AlgumSensorPreto()) return true;
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
        tempoPassadoMs += INTERVALO_SENSOR_MEIO_MS;
    }

    return AlgumSensorPreto();
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
    if (await AguardarQualquerPreto(TEMPO_PONTE_BRANCO_MS)) return true;

    MoverPonteBranco(-1);
    if (await AguardarQualquerPreto(TEMPO_RE_PONTE_BRANCO_MS)) return true;

    return false;
}

async Task ManterDirecaoEmBranco() {
    MoverPonteBranco(1);
    await Time.Delay(TEMPO_FRENTE_MS);
}

async Task CurvaVerdeEsquerda() {
    LimparMemoriaVerde();
    ultimaDirecao = -1;
    Mover(VELOCIDADE_VERDE_90, VELOCIDADE_VERDE_LENTA);
    await Time.Delay(TEMPO_CURVA_VERDE_90_MS);
    await EstabilizarDepoisDaCurva();
    await SairDaMarcaVerde();
}

async Task CurvaVerdeDireita() {
    LimparMemoriaVerde();
    ultimaDirecao = 1;
    Mover(VELOCIDADE_VERDE_LENTA, VELOCIDADE_VERDE_90);
    await Time.Delay(TEMPO_CURVA_VERDE_90_MS);
    await EstabilizarDepoisDaCurva();
    await SairDaMarcaVerde();
}

async Task AjustarDireitaInterno() {
    ultimaDirecao = 1;
    Mover(VELOCIDADE_LENTA_LEVE, VELOCIDADE_AJUSTE_LEVE);
    if (await AguardarOuMeioPreto(TEMPO_PULSO_AJUSTE_LEVE_MS)) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }
    await AvancarCegoAposAjuste();
}

async Task AjustarEsquerdaInterno() {
    ultimaDirecao = -1;
    Mover(VELOCIDADE_AJUSTE_LEVE, VELOCIDADE_LENTA_LEVE);
    if (await AguardarOuMeioPreto(TEMPO_PULSO_AJUSTE_LEVE_MS)) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }
    await AvancarCegoAposAjuste();
}

async Task AjustarDireitaExterno() {
    ultimaDirecao = 1;
    Mover(VELOCIDADE_LENTA_FORTE, VELOCIDADE_AJUSTE_FORTE);
    if (await AguardarOuMeioPreto(TEMPO_PULSO_AJUSTE_FORTE_MS)) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }
    await AvancarCegoAposAjuste();
}

async Task AjustarEsquerdaExterno() {
    ultimaDirecao = -1;
    Mover(VELOCIDADE_AJUSTE_FORTE, VELOCIDADE_LENTA_FORTE);
    if (await AguardarOuMeioPreto(TEMPO_PULSO_AJUSTE_FORTE_MS)) {
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

        if (todosBrancos) {
            if (inicioTodosBrancosMs == 0) inicioTodosBrancosMs = AgoraMs();
        } else {
            inicioTodosBrancosMs = 0;
        }

        if ((verdeEsquerda || verdeEsquerdaRecente) && !(verdeDireita || verdeDireitaRecente)) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "VERDE / ESQUERDA");
            await CurvaVerdeEsquerda();
        } else if (!(verdeEsquerda || verdeEsquerdaRecente) && (verdeDireita || verdeDireitaRecente)) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "VERDE / DIREITA");
            await CurvaVerdeDireita();
        } else if (pretoMeio) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO / FRENTE");
            await Frente();
        } else if (!pretoMeio && pretoDireitaInterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO BRANCO / ESQUERDA LEVE");
            await AjustarEsquerdaInterno();
        } else if (!pretoMeio && pretoEsquerdaInterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO BRANCO / DIREITA LEVE");
            await AjustarDireitaInterno();
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
