const string SERVO_PORTA = "servo_porta";
const string SERVO_BRACO = "servo_garra_braco";

// Funcao Base de Movimento do Servo
void MoverServo(string servo, double velocidade) {
    Bot.GetComponent<Servomotor>(servo).Locked = false;
    Bot.GetComponent<Servomotor>(servo).Apply(500, velocidade);
}


// ======================= Fechar/Abrir porta =======================
async Task FecharPorta() {
    MoverServo(SERVO_PORTA, 100); // Fechar / abaixar
    await Time.Delay(1000);

    Bot.GetComponent<Servomotor>(SERVO_PORTA).Locked = true;
}

async Task LevantarPorta() {
    MoverServo(SERVO_PORTA, -100); // Levantar / abrir
    await Time.Delay(1000);

    Bot.GetComponent<Servomotor>(SERVO_PORTA).Locked = true;
}


// ======================= Descer/Subir Garra =======================

// ABAIXAR E LEVANTAR BRACO
async Task AbaixarBraco() {
    MoverServo(SERVO_BRACO, 170); // Abaixar ( -80 --> 0 (+80) --> 90 (+90) )
    await Time.Delay(3000);
    Bot.GetComponent<Servomotor>(SERVO_BRACO).Locked = true;
}

async Task LevantarBraco() {
    MoverServo(SERVO_BRACO, -170); // Levantar ( (-80) -80 <-- (-90) 0 <-- 90 ) 
    await Time.Delay(2000);
    Bot.GetComponent<Servomotor>(SERVO_BRACO).Locked = true;
}

// PREPARACAO INICIAL BRACO
async Task PreparacaoInicialBraco() {
    MoverServo(SERVO_BRACO, -170); // 1030ms para sair da posição inicial e chegar a final
    await Time.Delay(2000);
    Bot.GetComponent<Servomotor>(SERVO_BRACO).Locked = true;
}


// ======================= Motores de Tração =======================
const string MOTOR_ESQUERDO_TRASEIRO = "motor_esquerdo_traseiro";
const string MOTOR_DIREITO_TRASEIRO  = "motor_direito_traseiro";
const string MOTOR_DIREITO_FRONTAL   = "motor_direito_frontal";
const string MOTOR_ESQUERDO_FRONTAL  = "motor_esquerdo_frontal";

// ======================= Sensores IR =======================
const string SENSOR_ESQUERDO_INTERNO = "sensor_IR_esquerda_interno";
const string SENSOR_ESQUERDO_EXTERNO = "sensor_IR_esquerda_externo";
const string SENSOR_MEIO             = "sensor_IR_meio";
const string SENSOR_DIREITO_INTERNO  = "sensor_IR_direita_interno";
const string SENSOR_DIREITO_EXTERNO  = "sensor_IR_direita_externo";

// ======================= Sensores Ultrassônicos =======================
const string ULTRA_FRENTE_ESQUERDA = "ultrassonico_frente_esquerda";
const string ULTRA_FRENTE_DIREITA  = "ultrassonico_frente_direita";
const string ULTRA_DIREITA         = "ultrassonico_direita";
const string ULTRA_ESQUERDA        = "ultrassonico_esquerda";

// ======================= Parâmetros Básicos =======================
const double FORCA_MOTOR        = 1000;
const double BRILHO_MAXIMO_PRETO = 10;
const double BRILHO_MINIMO_VERDE = 20;
const double MARGEM_VERDE        = 12;

// ======================= Velocidades =======================
const double VELOCIDADE_FRENTE         = 200;
const double VELOCIDADE_AJUSTE_LEVE    = 2400;
const double VELOCIDADE_LENTA_LEVE     = -450;
const double VELOCIDADE_AJUSTE_FORTE   = 4200;
const double VELOCIDADE_LENTA_FORTE    = -950;
const double VELOCIDADE_GIRO_VERDE_PARADO = 1500;
const double ANGULO_GIRO_VERDE_GRAUS   = 95;
const double VELOCIDADE_SAIDA_VERDE    = 170;
const double VELOCIDADE_PONTE_BRANCO   = 120;

// ======================= Velocidades de Obstáculo =======================
const double CM_POR_UNIDADE_ULTRASSOM    = 10.0;
const double DISTANCIA_OBSTACULO         = 40.0; // Distancia para ver o obstaculo
const double VELOCIDADE_GIRO_OBSTACULO   = 1300;
const double VELOCIDADE_BUSCA_GIRO       = 350;
const double VELOCIDADE_DESVIO           = 320;
const double TEMPO_MAX_GIRO_OBSTACULO_MS = 1000;
const double TEMPO_ESPERA_RETORNO_MS     = 1500;
const double PAUSA_ESTABILIZAR_MS        = 80;
const double ANGULO_GIRO_OBSTACULO_GRAUS = 70;
const double ANGULO_GIRO_RETORNO_GRAUS   = 70;  // giro de retorno à linha após passar o obstáculo (FASE 3)
const double TEMPO_MEMORIA_OBSTACULO_MS  = 10000;
const double TEMPO_AVANCO_APOS_BLOCO_MS  = 300;   // aguarda após sensor lateral perder o bloco antes de virar de retorno
const double DISTANCIA_RE_DESVIO_CM      = 30.0;  // distância mínima para iniciar desvio; dá ré até chegar aqui

// Fases de Obstáculo
const int MODO_SEGUIR_LINHA = 0;
const int MODO_FASE1_DESVIO  = 1;
const int MODO_FASE2_PASSE   = 2;
const int MODO_FASE2_ESPERA  = 3;
const int MODO_FASE3_RETORNO = 4;
const int MODO_FASE4_PROCURA = 5;
const int MODO_FASE5_AJUSTE  = 6;
const int MODO_FASE2_FRENTE  = 7;

// ======================= Tempos =======================
const double TEMPO_FRENTE_MS                 = 60;
const double TEMPO_CEGO_ANTES_CURVA_VERDE_MS = 1000;
const double TEMPO_BLOQUEIO_90_SEM_VERDE_MS  = 200;
const double TEMPO_PULSO_AJUSTE_LEVE_MS      = 90;
const double TEMPO_PULSO_AJUSTE_FORTE_MS     = 180;
const double TEMPO_SAIDA_VERDE_MS            = 320;
const double TEMPO_CEGO_APOS_AJUSTE_MS       = 70;
const double TEMPO_ESTABILIZAR_CURVA_MS      = 60;
const double TEMPO_PONTE_BRANCO_MS           = 260;
const double TEMPO_RE_PONTE_BRANCO_MS        = 90;
const double TEMPO_BRANCO_PARA_PONTE_MS      = 2000;
const double TEMPO_MEMORIA_VERDE_MS          = 450;
const double INTERVALO_SENSOR_MEIO_MS        = 1;
const double TEMPO_MAX_GIRO_VERDE_MS         = 1750;

// ======================= Estado Global =======================
string ultimoEstado = "";
int    ultimaDirecao = 0;
double inicioTodosBrancosMs = 0;
double ultimoVerdeEsquerdaMs = 0;
double ultimoVerdeDireitaMs  = 0;
double ultimoDeteccao90SemVerdeMs = 0;

int    modoAtual             = MODO_SEGUIR_LINHA;
int    ladoDesvio            = 1;
double anguloInicioGiro      = 0;
bool   jaViuObstaculoLateral  = false;
int    fase2Iteracoes         = 0;
double inicioModoMs           = 0;
double ultimoObstaculoMs     = 0;
bool   buscaViuLateral       = false;
bool   buscaAchouAoAvancar   = false;
double inicioBusca2Ms        = 0;
string ultimoLogModo         = "";
string ultimoLogUltrassonico = "";
double primeiroVermelhoMs    = 0;

// ======================= Utilitários =======================
double AgoraMs() { return Time.Timestamp * 1000.0; }

double Abs(double v) { return v < 0 ? -v : v; }

double NormalizarAngulo(double a) {
    while (a < 0) a += 360;
    while (a >= 360) a -= 360;
    return a;
}

double DiferencaAngular(double origem, double atual) {
    double d = Abs(NormalizarAngulo(atual) - NormalizarAngulo(origem));
    if (d > 180) d = 360 - d;
    return d;
}

// ======================= Leitura de Cor =======================
Color LerCor(string sensor) {
    return Bot.GetComponent<ColorSensor>(sensor).Analog;
}

bool Se_Preto(Color cor) {
    if (cor.Blue > cor.Red && cor.Blue > cor.Green) return false;
    return (cor.Closest() == Colors.Black) || (cor.Brightness <= BRILHO_MAXIMO_PRETO);
}

bool Se_Verde(Color cor) {
    return cor.Closest() == Colors.Green
        || (cor.Green >= BRILHO_MINIMO_VERDE && cor.Green > (cor.Red + MARGEM_VERDE) && cor.Green > (cor.Blue + MARGEM_VERDE));
}

bool Se_Vermelho(Color cor) {
    return cor.Closest() == Colors.Red;
}

bool AlgumSensorVermelho() {
    return Se_Vermelho(LerCor(SENSOR_ESQUERDO_INTERNO))
        || Se_Vermelho(LerCor(SENSOR_ESQUERDO_EXTERNO))
        || Se_Vermelho(LerCor(SENSOR_MEIO))
        || Se_Vermelho(LerCor(SENSOR_DIREITO_INTERNO))
        || Se_Vermelho(LerCor(SENSOR_DIREITO_EXTERNO));
}

// ======================= Sensores IR =======================
bool PretoEsquerdaInterno() { return Se_Preto(LerCor(SENSOR_ESQUERDO_INTERNO)); }
bool PretoEsquerdaExterno()  { return Se_Preto(LerCor(SENSOR_ESQUERDO_EXTERNO)); }
bool PretoMeio()             { return Se_Preto(LerCor(SENSOR_MEIO)); }
bool PretoDireitaInterno()   { return Se_Preto(LerCor(SENSOR_DIREITO_INTERNO)); }
bool PretoDireitaExterno()   { return Se_Preto(LerCor(SENSOR_DIREITO_EXTERNO)); }

bool VerdeEsquerda() {
    return Se_Verde(LerCor(SENSOR_ESQUERDO_INTERNO)) || Se_Verde(LerCor(SENSOR_ESQUERDO_EXTERNO));
}
bool VerdeDireita() {
    return Se_Verde(LerCor(SENSOR_DIREITO_INTERNO)) || Se_Verde(LerCor(SENSOR_DIREITO_EXTERNO));
}

bool AlgumSensorSe_Preto() {
    return PretoMeio() || PretoEsquerdaInterno() || PretoEsquerdaExterno()
        || PretoDireitaInterno() || PretoDireitaExterno();
}

// ======================= Motores =======================
void LigarMotor(string motor, double velocidade) {
    Bot.GetComponent<Servomotor>(motor).Locked = false;
    Bot.GetComponent<Servomotor>(motor).Apply(FORCA_MOTOR, velocidade);
}

void Mover(double velocidadeEsquerda, double velocidadeDireita) {
    LigarMotor(MOTOR_ESQUERDO_FRONTAL,  velocidadeDireita);
    LigarMotor(MOTOR_ESQUERDO_TRASEIRO, velocidadeDireita);
    LigarMotor(MOTOR_DIREITO_FRONTAL,   velocidadeEsquerda);
    LigarMotor(MOTOR_DIREITO_TRASEIRO,  velocidadeEsquerda);
}

// ======================= Log =======================
string NomeCor(bool preto) { return preto ? "PRETO" : "BRANCO"; }

void ImprimirEstado(bool ei, bool ee, bool m, bool di, bool de, string modo) {
    string estado = modo + " | EI=" + NomeCor(ei) + " EE=" + NomeCor(ee)
        + " M=" + NomeCor(m) + " DI=" + NomeCor(di) + " DE=" + NomeCor(de);
    if (estado == ultimoEstado) return;
    ultimoEstado = estado;
    IO.PrintLine(estado);
}

void EntrarModo(int novoModo, string texto) {
    modoAtual = novoModo;
    inicioModoMs = AgoraMs();
    if (texto == ultimoLogModo) return;
    ultimoLogModo = texto;
    IO.PrintLine("[MODO] " + texto);
}

// ======================= Movimentos Auxiliares =======================
async Task Frente() {
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_FRENTE_MS);
}

async Task<bool> AguardarOuMeioSe_Preto(double tempoMs) {
    double t = 0;
    while (t < tempoMs) {
        if (PretoMeio()) return true;
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
        t += INTERVALO_SENSOR_MEIO_MS;
    }
    return PretoMeio();
}

async Task<bool> AguardarQualquerSe_Preto(double tempoMs) {
    double t = 0;
    while (t < tempoMs) {
        if (AlgumSensorSe_Preto()) return true;
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
        t += INTERVALO_SENSOR_MEIO_MS;
    }
    return AlgumSensorSe_Preto();
}

async Task AvancarCegoAposAjuste() {
    if (PretoMeio()) { Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE); return; }
    Mover(VELOCIDADE_FRENTE * 0.7, VELOCIDADE_FRENTE * 0.7);
    await AguardarOuMeioSe_Preto(TEMPO_CEGO_APOS_AJUSTE_MS);
}

void LimparMemoriaSe_Verde() {
    ultimoVerdeEsquerdaMs = 0;
    ultimoVerdeDireitaMs  = 0;
    inicioTodosBrancosMs  = 0;
}

void MoverPonteBranco(double direcao) {
    if (ultimaDirecao < 0)
        Mover(direcao * VELOCIDADE_PONTE_BRANCO * 0.65, direcao * VELOCIDADE_PONTE_BRANCO);
    else if (ultimaDirecao > 0)
        Mover(direcao * VELOCIDADE_PONTE_BRANCO, direcao * VELOCIDADE_PONTE_BRANCO * 0.65);
    else
        Mover(direcao * VELOCIDADE_PONTE_BRANCO, direcao * VELOCIDADE_PONTE_BRANCO);
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

// ======================= Curvas Verdes =======================
async Task CurvaVerdeEsquerda() {
    LimparMemoriaSe_Verde(); ultimaDirecao = -1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0); await Time.Delay(20);
    double ini = AgoraMs(), ang = Bot.Compass;
    Mover(VELOCIDADE_GIRO_VERDE_PARADO, -VELOCIDADE_GIRO_VERDE_PARADO);
    while (DiferencaAngular(ang, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS && (AgoraMs() - ini) < TEMPO_MAX_GIRO_VERDE_MS)
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    Mover(0, 0);
}

async Task CurvaVerdeDireita() {
    LimparMemoriaSe_Verde(); ultimaDirecao = 1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0); await Time.Delay(20);
    double ini = AgoraMs(), ang = Bot.Compass;
    Mover(-VELOCIDADE_GIRO_VERDE_PARADO, VELOCIDADE_GIRO_VERDE_PARADO);
    while (DiferencaAngular(ang, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS && (AgoraMs() - ini) < TEMPO_MAX_GIRO_VERDE_MS)
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    Mover(0, 0);
}

async Task Curva90SemVerdeEsquerda() {
    ultimoDeteccao90SemVerdeMs = AgoraMs(); inicioTodosBrancosMs = 0; ultimaDirecao = -1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0); await Time.Delay(20);
    double ini = AgoraMs(), ang = Bot.Compass;
    Mover(VELOCIDADE_GIRO_VERDE_PARADO, -VELOCIDADE_GIRO_VERDE_PARADO);
    while (DiferencaAngular(ang, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS && (AgoraMs() - ini) < TEMPO_MAX_GIRO_VERDE_MS)
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    Mover(0, 0);
}

async Task Curva90SemVerdeDireita() {
    ultimoDeteccao90SemVerdeMs = AgoraMs(); inicioTodosBrancosMs = 0; ultimaDirecao = 1;
    Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
    await Time.Delay(TEMPO_CEGO_ANTES_CURVA_VERDE_MS);
    Mover(0, 0); await Time.Delay(20);
    double ini = AgoraMs(), ang = Bot.Compass;
    Mover(-VELOCIDADE_GIRO_VERDE_PARADO, VELOCIDADE_GIRO_VERDE_PARADO);
    while (DiferencaAngular(ang, Bot.Compass) < ANGULO_GIRO_VERDE_GRAUS && (AgoraMs() - ini) < TEMPO_MAX_GIRO_VERDE_MS)
        await Time.Delay(INTERVALO_SENSOR_MEIO_MS);
    Mover(0, 0);
}

// ======================= Ajustes =======================
async Task AjustarDireitaInterno() {
    ultimaDirecao = 1;
    Mover(VELOCIDADE_LENTA_LEVE, VELOCIDADE_AJUSTE_LEVE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_LEVE_MS)) { Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE); return; }
    await AvancarCegoAposAjuste();
}

async Task AjustarEsquerdaInterno() {
    ultimaDirecao = -1;
    Mover(VELOCIDADE_AJUSTE_LEVE, VELOCIDADE_LENTA_LEVE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_LEVE_MS)) { Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE); return; }
    await AvancarCegoAposAjuste();
}

async Task AjustarDireitaExterno() {
    ultimaDirecao = 1;
    Mover(VELOCIDADE_LENTA_FORTE, VELOCIDADE_AJUSTE_FORTE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_FORTE_MS)) { Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE); return; }
    await AvancarCegoAposAjuste();
}

async Task AjustarEsquerdaExterno() {
    ultimaDirecao = -1;
    Mover(VELOCIDADE_AJUSTE_FORTE, VELOCIDADE_LENTA_FORTE);
    if (await AguardarOuMeioSe_Preto(TEMPO_PULSO_AJUSTE_FORTE_MS)) { Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE); return; }
    await AvancarCegoAposAjuste();
}

// ======================= Ultrassônicos =======================
double LerUltraCm(string nomeSensor) {
    double d = Bot.GetComponent<UltrasonicSensor>(nomeSensor).Analog;
    if (d < 0) return -1;
    return d * CM_POR_UNIDADE_ULTRASSOM;
}

double LerLateral(int lado) {
    return lado > 0 ? LerUltraCm(ULTRA_DIREITA) : LerUltraCm(ULTRA_ESQUERDA);
}

bool ObstaculoNaFrente(double fe, double fd) {
    return (fe >= 0 && fe <= DISTANCIA_OBSTACULO) && (fd >= 0 && fd <= DISTANCIA_OBSTACULO);
}

bool LateralVeObstaculo(int lado) {
    double cm = LerLateral(lado);
    return cm >= 0 && cm <= DISTANCIA_OBSTACULO;
}

void GirarParaLado(int lado) {
    if (lado > 0) Mover(-VELOCIDADE_GIRO_OBSTACULO, VELOCIDADE_GIRO_OBSTACULO);
    else Mover(VELOCIDADE_GIRO_OBSTACULO, -VELOCIDADE_GIRO_OBSTACULO);
}


// ======================= Inicializacao =======================
async Task Inicializacao() {
    await FecharPorta();
    await Time.Delay(1000);
    await PreparacaoInicialBraco();
}


// ======================= Main =======================
async Task Main() {
    IO.OpenConsole();
    await Inicializacao();

    while (true) {

        double frenteEsquerdaCm = LerUltraCm(ULTRA_FRENTE_ESQUERDA);
        double frenteDireitaCm  = LerUltraCm(ULTRA_FRENTE_DIREITA);
        double direitaCm        = LerUltraCm(ULTRA_DIREITA);
        double esquerdaCm       = LerUltraCm(ULTRA_ESQUERDA);

        // FASE 1: Desvio Primário
        if (modoAtual == MODO_FASE1_DESVIO) {
            GirarParaLado(ladoDesvio);
            bool girou = DiferencaAngular(anguloInicioGiro, Bot.Compass) >= ANGULO_GIRO_OBSTACULO_GRAUS;
            bool tempoEsgotou = (AgoraMs() - inicioModoMs) > TEMPO_MAX_GIRO_OBSTACULO_MS;
            if (girou || tempoEsgotou) {
                Mover(0, 0);
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                jaViuObstaculoLateral = false;
                IO.PrintLine("[DESVIO] Fase 1: terminada");
                EntrarModo(MODO_FASE2_PASSE, "FASE 2: PASSE CEGO");
            }
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        // FASE 2a: Passe Cego — anda reto, monitora lateral até obstáculo sair
        if (modoAtual == MODO_FASE2_PASSE) {
            fase2Iteracoes++;
            Mover(VELOCIDADE_DESVIO, VELOCIDADE_DESVIO);
            bool veAtual = LateralVeObstaculo(-ladoDesvio);
            if (veAtual) jaViuObstaculoLateral = true;
            bool passou = jaViuObstaculoLateral && !veAtual;
            // timeout normal: só após ver o bloco (evita saída prematura)
            bool timeout = jaViuObstaculoLateral && (AgoraMs() - inicioModoMs) > TEMPO_MAX_GIRO_OBSTACULO_MS;
            // fallback: tempo OU contador de iterações (garante saída mesmo sem Time.Timestamp)
            bool timeoutGlobal = (AgoraMs() - inicioModoMs) > TEMPO_MAX_GIRO_OBSTACULO_MS * 2
                              || fase2Iteracoes > 35;
            if (passou || timeout || timeoutGlobal) {
                anguloInicioGiro = Bot.Compass;
                EntrarModo(MODO_FASE2_ESPERA, "FASE 2: VIRANDO DE VOLTA");
            }
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        // FASE 2b: vira no sentido oposto ao desvio pelos mesmos graus da FASE 1
        if (modoAtual == MODO_FASE2_ESPERA) {
            GirarParaLado(-ladoDesvio);
            bool girou = DiferencaAngular(anguloInicioGiro, Bot.Compass) >= ANGULO_GIRO_OBSTACULO_GRAUS;
            bool tempoEsgotou = (AgoraMs() - inicioModoMs) > TEMPO_MAX_GIRO_OBSTACULO_MS;
            if (girou || tempoEsgotou) {
                Mover(0, 0);
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                jaViuObstaculoLateral = false;
                IO.PrintLine("[DESVIO] Fase 2b: giro concluido");
                EntrarModo(MODO_FASE2_FRENTE, "FASE 2c: AVANCANDO APOS GIRO");
            }
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        // FASE 2c: avança, aplica regra sensor direito, aguarda bloco passar para iniciar FASE 3
        if (modoAtual == MODO_FASE2_FRENTE) {
            bool veLatNow = LateralVeObstaculo(-ladoDesvio);
            if (veLatNow) jaViuObstaculoLateral = true;
            bool obstaculoPassou = jaViuObstaculoLateral && !veLatNow;
            bool achouLinha = AlgumSensorSe_Preto();
            bool timeoutFase2c = (AgoraMs() - inicioModoMs) > TEMPO_MAX_GIRO_OBSTACULO_MS * 4;

            if (achouLinha) {
                Mover(0, 0);
                IO.PrintLine("[DESVIO] Fase 2c: linha encontrada");
                ultimoObstaculoMs = 0;
                EntrarModo(MODO_SEGUIR_LINHA, "SEGUIR LINHA");
                continue;
            }

            if (obstaculoPassou || timeoutFase2c) {
                Mover(VELOCIDADE_DESVIO, VELOCIDADE_DESVIO);
                await Time.Delay(TEMPO_AVANCO_APOS_BLOCO_MS);
                Mover(0, 0);
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                anguloInicioGiro = Bot.Compass;
                IO.PrintLine("[DESVIO] Fase 2c: obstaculo passou, iniciando FASE 3");
                EntrarModo(MODO_FASE3_RETORNO, "FASE 3: GIRO DE RETORNO");
                continue;
            }

            if (!PretoMeio() && PretoDireitaInterno()) await AjustarDireitaInterno();
            else Mover(VELOCIDADE_DESVIO, VELOCIDADE_DESVIO);
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        // FASE 3: Giro de Retorno
        if (modoAtual == MODO_FASE3_RETORNO) {
            GirarParaLado(-ladoDesvio);
            bool girou = DiferencaAngular(anguloInicioGiro, Bot.Compass) >= ANGULO_GIRO_RETORNO_GRAUS;
            bool tempoEsgotou = (AgoraMs() - inicioModoMs) > TEMPO_MAX_GIRO_OBSTACULO_MS;
            if (girou || tempoEsgotou) {
                Mover(0, 0);
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                IO.PrintLine("[DESVIO] Fase 3: terminada");
                EntrarModo(MODO_SEGUIR_LINHA, "SEGUIR LINHA");
            }
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        // FASE 4: Procurar Nova Linha
        if (modoAtual == MODO_FASE4_PROCURA) {
            bool veLatNow = LateralVeObstaculo(-ladoDesvio);
            if (veLatNow) buscaViuLateral = true;
            bool lateralConfirmada = buscaViuLateral && !veLatNow;
            if (AlgumSensorSe_Preto()) {
                buscaAchouAoAvancar = true;
                buscaViuLateral     = true;
                lateralConfirmada   = true;
            } else {
                Mover(VELOCIDADE_DESVIO, VELOCIDADE_DESVIO);
            }
            if (lateralConfirmada) {
                Mover(0, 0);
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                IO.PrintLine("[DESVIO] Fase 4: terminada");
                inicioBusca2Ms = 0;
                EntrarModo(MODO_FASE5_AJUSTE, "FASE 5: AJUSTE DE LINHA");
            }
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        // FASE 5: Ajuste de Linha
        if (modoAtual == MODO_FASE5_AJUSTE) {
            if (inicioBusca2Ms == 0) inicioBusca2Ms = AgoraMs();
            double tempoGirando = AgoraMs() - inicioBusca2Ms;
            bool _di = PretoDireitaInterno();
            bool _de = PretoDireitaExterno();
            bool _ei = PretoEsquerdaInterno();
            bool _ee = PretoEsquerdaExterno();
            bool _m  = PretoMeio();
            bool ajustado   = _m && !_ei && !_ee && !_di && !_de;
            bool timeoutGiro = tempoGirando >= TEMPO_MAX_GIRO_OBSTACULO_MS;
            if (ajustado || timeoutGiro) {
                Mover(0, 0);
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                IO.PrintLine("[DESVIO] Fase 5: terminada");
                ultimoObstaculoMs = 0;
                EntrarModo(MODO_SEGUIR_LINHA, "SEGUIR LINHA");
            } else {
                int ladoGiro = buscaAchouAoAvancar ? ladoDesvio : -ladoDesvio;
                if (ladoGiro > 0) Mover(-VELOCIDADE_BUSCA_GIRO, VELOCIDADE_BUSCA_GIRO);
                else Mover(VELOCIDADE_BUSCA_GIRO, -VELOCIDADE_BUSCA_GIRO);
            }
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        // ---- Seguir Linha: checar obstáculo primeiro ----
        if (ObstaculoNaFrente(frenteEsquerdaCm, frenteDireitaCm)) {
            Mover(0, 0);
            IO.PrintLine("OBSTACULO ENCONTRADO");

            // dá ré até ficar a mais de DISTANCIA_RE_DESVIO_CM do obstáculo
            double fe2 = frenteEsquerdaCm, fd2 = frenteDireitaCm;
            while ((fe2 >= 0 && fe2 < DISTANCIA_RE_DESVIO_CM) || (fd2 >= 0 && fd2 < DISTANCIA_RE_DESVIO_CM)) {
                Mover(-VELOCIDADE_DESVIO, -VELOCIDADE_DESVIO);
                await Time.Delay(TEMPO_FRENTE_MS);
                fe2 = LerUltraCm(ULTRA_FRENTE_ESQUERDA);
                fd2 = LerUltraCm(ULTRA_FRENTE_DIREITA);
            }
            Mover(0, 0);
            await Time.Delay(PAUSA_ESTABILIZAR_MS);

            // esquece obstáculo anterior, reseta estado para desvio limpo
            jaViuObstaculoLateral = false;
            fase2Iteracoes        = 0;
            buscaViuLateral       = false;
            buscaAchouAoAvancar   = false;
            inicioBusca2Ms        = 0;

            ultimoObstaculoMs = AgoraMs();
            double dirCheck = LerUltraCm(ULTRA_DIREITA);
            ladoDesvio = (dirCheck < 0 || dirCheck > DISTANCIA_OBSTACULO) ? 1 : -1;
            anguloInicioGiro = Bot.Compass;
            string ladoStr = ladoDesvio > 0 ? "DIREITA" : "ESQUERDA";
            EntrarModo(MODO_FASE1_DESVIO, "FASE 1: DESVIO " + ladoStr);
            await Time.Delay(TEMPO_FRENTE_MS);
            continue;
        }

        bool pretoEsquerdaInterno = PretoEsquerdaInterno();
        bool pretoEsquerdaExterno = PretoEsquerdaExterno();
        bool pretoMeio            = PretoMeio();
        bool pretoDireitaInterno  = PretoDireitaInterno();
        bool pretoDireitaExterno  = PretoDireitaExterno();
        bool verdeEsquerda        = VerdeEsquerda();
        bool verdeDireita         = VerdeDireita();
        bool pretoEsquerda        = pretoEsquerdaInterno || pretoEsquerdaExterno;
        bool pretoDireita         = pretoDireitaInterno  || pretoDireitaExterno;
        bool todosBrancos         = !pretoMeio && !pretoDireita && !pretoEsquerda;
        bool obstaculoRecente     = ultimoObstaculoMs > 0 && (AgoraMs() - ultimoObstaculoMs) <= TEMPO_MEMORIA_OBSTACULO_MS;

        if (verdeEsquerda) ultimoVerdeEsquerdaMs = AgoraMs();
        if (verdeDireita)  ultimoVerdeDireitaMs  = AgoraMs();

        bool verdeEsquerdaRecente = ultimoVerdeEsquerdaMs > 0 && (AgoraMs() - ultimoVerdeEsquerdaMs) <= TEMPO_MEMORIA_VERDE_MS;
        bool verdeDireitaRecente  = ultimoVerdeDireitaMs  > 0 && (AgoraMs() - ultimoVerdeDireitaMs)  <= TEMPO_MEMORIA_VERDE_MS;
        bool verdeEsquerdaAtivo   = verdeEsquerda || verdeEsquerdaRecente;
        bool verdeDireitaAtivo    = verdeDireita  || verdeDireitaRecente;

        bool bloqueio90SemVerdeAtivo = ultimoDeteccao90SemVerdeMs > 0
            && (AgoraMs() - ultimoDeteccao90SemVerdeMs) <= TEMPO_BLOQUEIO_90_SEM_VERDE_MS;
        bool curva90SemVerdeEsquerda = !bloqueio90SemVerdeAtivo && !verdeEsquerdaAtivo && !verdeDireitaAtivo
            && pretoMeio && pretoEsquerdaInterno && pretoEsquerdaExterno && !pretoDireitaInterno && !pretoDireitaExterno;
        bool curva90SemVerdeDireita = !bloqueio90SemVerdeAtivo && !verdeEsquerdaAtivo && !verdeDireitaAtivo
            && pretoMeio && !pretoEsquerdaInterno && !pretoEsquerdaExterno && pretoDireitaInterno && pretoDireitaExterno;

        if (todosBrancos) { if (inicioTodosBrancosMs == 0) inicioTodosBrancosMs = AgoraMs(); }
        else { inicioTodosBrancosMs = 0; }

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
        } else if (pretoMeio && pretoEsquerda && pretoDireita && obstaculoRecente) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "TODOS PRETOS / ALINHANDO POS OBSTACULO");
            if (ladoDesvio > 0) Mover(-VELOCIDADE_BUSCA_GIRO, VELOCIDADE_BUSCA_GIRO);
            else Mover(VELOCIDADE_BUSCA_GIRO, -VELOCIDADE_BUSCA_GIRO);
        } else if (pretoMeio) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO / FRENTE");
            await Frente();
        } else if (!pretoMeio && pretoDireitaInterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO BRANCO / DIREITA LEVE");
            await AjustarDireitaInterno();
        } else if (!pretoMeio && pretoEsquerdaInterno) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "MEIO BRANCO / ESQUERDA LEVE");
            await AjustarEsquerdaInterno();
        } else if (todosBrancos && obstaculoRecente && (AgoraMs() - inicioModoMs) >= 500) {
            ImprimirEstado(pretoEsquerdaInterno, pretoEsquerdaExterno, pretoMeio, pretoDireitaInterno, pretoDireitaExterno, "BUSCA LINHA RETORNO");
            inicioTodosBrancosMs = 0;
            buscaViuLateral      = false;
            buscaAchouAoAvancar  = false;
            inicioBusca2Ms       = 0;
            EntrarModo(MODO_FASE4_PROCURA, "FASE 4: PROCURANDO LINHA");
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