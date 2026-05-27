const string MOTOR_ESQUERDO_TRASEIRO = "motor_esquerdo_traseiro";
const string MOTOR_DIREITO_TRASEIRO = "motor_direito_traseiro";
const string MOTOR_DIREITO_FRONTAL = "motor_direito_frontal";
const string MOTOR_ESQUERDO_FRONTAL = "motor_esquerdo_frontal";

const string SENSOR_ESQUERDO_INTERNO = "sensor_IR_esquerda_interno";
const string SENSOR_ESQUERDO_EXTERNO = "sensor_IR_esquerda_externo";
const string SENSOR_MEIO = "sensor_IR_meio";
const string SENSOR_DIREITO_INTERNO = "sensor_IR_direita_interno";
const string SENSOR_DIREITO_EXTERNO = "sensor_IR_direita_externo";

const string ULTRA_FRENTE_DIREITA = "ultrassonico_frente_direita";
const string ULTRA_FRENTE_ESQUERDA = "ultrassonico_frente_esquerda";
const string ULTRA_DIREITA = "ultrassonico_direita";
const string ULTRA_ESQUERDA = "ultrassonico_esquerda";

const double FORCA_MOTOR = 800;
const double BRILHO_MAXIMO_PRETO = 45;

// O ultrassonico do sBotics retorna em "unidades do simulador".
// Ajuste este fator ate bater com uma regua de cm no seu mapa.
const double CM_POR_UNIDADE_ULTRASSOM = 10.0;

const double DISTANCIA_PAREDE_FRENTE_CM = 22;
const double DISTANCIA_PAREDE_LATERAL_CM = 18;

const double VELOCIDADE_FRENTE = 220;
const double VELOCIDADE_LENTA_BUSCA = 120;
const double VELOCIDADE_GIRO_PARADO = 1300;
const double VELOCIDADE_AJUSTE_LEVE = 280;
const double VELOCIDADE_AJUSTE_FORTE = 420;

const double INTERVALO_LOOP_MS = 25;
const double TEMPO_MAX_GIRO_MS = 1800;
const double TEMPO_MAX_ANDAR_LADO_MS = 4000;
const double PAUSA_ESTABILIZAR_MS = 80;
const double INTERVALO_LOG_ULTRA_MS = 160;

const int ESTADO_SEGUIR_LINHA = 0;
const int ESTADO_GIRAR_DIREITA = 1;
const int ESTADO_ANDAR_LADO_DA_PAREDE = 2;
const int ESTADO_GIRAR_PARA_FRENTE = 3;
const int ESTADO_PROCURAR_LINHA = 4;

int estado = ESTADO_SEGUIR_LINHA;
double inicioEstadoMs = 0;
string ultimoLogEstado = "";
double ultimoLogUltrassonicoMs = 0;

double AgoraMs() {
    return Time.Timestamp * 1000.0;
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

void Parar() {
    Mover(0, 0);
}

Color LerCor(string sensor) {
    return Bot.GetComponent<ColorSensor>(sensor).Analog;
}

bool EhPreto(Color cor) {
    return (cor.Closest() == Colors.Black) || (cor.Brightness <= BRILHO_MAXIMO_PRETO);
}

bool PretoMeio() {
    return EhPreto(LerCor(SENSOR_MEIO));
}

bool PretoEsquerdaInterno() {
    return EhPreto(LerCor(SENSOR_ESQUERDO_INTERNO));
}

bool PretoEsquerdaExterno() {
    return EhPreto(LerCor(SENSOR_ESQUERDO_EXTERNO));
}

bool PretoDireitaInterno() {
    return EhPreto(LerCor(SENSOR_DIREITO_INTERNO));
}

bool PretoDireitaExterno() {
    return EhPreto(LerCor(SENSOR_DIREITO_EXTERNO));
}

bool AlgumSensorLinhaPreto() {
    return PretoMeio()
        || PretoEsquerdaInterno()
        || PretoEsquerdaExterno()
        || PretoDireitaInterno()
        || PretoDireitaExterno();
}

double LerUltraUnidade(string nomeSensor) {
    return Bot.GetComponent<UltrasonicSensor>(nomeSensor).Analog;
}

double UltraUnidadeParaCm(double distanciaUnidade) {
    if (distanciaUnidade < 0) return -1;
    return distanciaUnidade * CM_POR_UNIDADE_ULTRASSOM;
}

double LerUltraCm(string nomeSensor) {
    return UltraUnidadeParaCm(LerUltraUnidade(nomeSensor));
}

bool DistanciaValidaAte(double distanciaCm, double limiteCm) {
    return distanciaCm >= 0 && distanciaCm <= limiteCm;
}

bool ParedeNaFrente(double frenteEsquerdaCm, double frenteDireitaCm) {
    return DistanciaValidaAte(frenteEsquerdaCm, DISTANCIA_PAREDE_FRENTE_CM)
        && DistanciaValidaAte(frenteDireitaCm, DISTANCIA_PAREDE_FRENTE_CM);
}

bool FrenteLivre(double frenteEsquerdaCm, double frenteDireitaCm) {
    bool esquerdaLivre = (frenteEsquerdaCm < 0) || (frenteEsquerdaCm > DISTANCIA_PAREDE_FRENTE_CM);
    bool direitaLivre = (frenteDireitaCm < 0) || (frenteDireitaCm > DISTANCIA_PAREDE_FRENTE_CM);
    return esquerdaLivre && direitaLivre;
}

bool ParedeNaDireita(double direitaCm) {
    return DistanciaValidaAte(direitaCm, DISTANCIA_PAREDE_LATERAL_CM);
}

bool ParedeNaEsquerda(double esquerdaCm) {
    return DistanciaValidaAte(esquerdaCm, DISTANCIA_PAREDE_LATERAL_CM);
}

string FmtDist(double cm) {
    if (cm < 0) return "NADA";
    return cm.ToString("0.0");
}

void LogUltrassonicos(double frenteEsquerdaCm, double frenteDireitaCm, double esquerdaCm, double direitaCm) {
    if ((AgoraMs() - ultimoLogUltrassonicoMs) < INTERVALO_LOG_ULTRA_MS) return;
    ultimoLogUltrassonicoMs = AgoraMs();
    IO.PrintLine(
        "[ULTRA cm] FE=" + FmtDist(frenteEsquerdaCm)
        + " FD=" + FmtDist(frenteDireitaCm)
        + " E=" + FmtDist(esquerdaCm)
        + " D=" + FmtDist(direitaCm)
    );
}

void LogEstado(string texto) {
    if (texto == ultimoLogEstado) return;
    ultimoLogEstado = texto;
    IO.PrintLine("[ESTADO] " + texto);
}

void EntrarEstado(int novoEstado, string texto) {
    estado = novoEstado;
    inicioEstadoMs = AgoraMs();
    LogEstado(texto);
}

void SeguirLinhaRapido() {
    bool m = PretoMeio();
    bool ei = PretoEsquerdaInterno();
    bool ee = PretoEsquerdaExterno();
    bool di = PretoDireitaInterno();
    bool de = PretoDireitaExterno();

    if (m) {
        Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);
        return;
    }

    if (de || di) {
        Mover(VELOCIDADE_AJUSTE_FORTE, VELOCIDADE_AJUSTE_LEVE);
        return;
    }

    if (ee || ei) {
        Mover(VELOCIDADE_AJUSTE_LEVE, VELOCIDADE_AJUSTE_FORTE);
        return;
    }

    Mover(VELOCIDADE_LENTA_BUSCA, VELOCIDADE_LENTA_BUSCA);
}

void ProcurarLinhaDevagar() {
    bool ei = PretoEsquerdaInterno() || PretoEsquerdaExterno();
    bool di = PretoDireitaInterno() || PretoDireitaExterno();

    if (PretoMeio()) {
        Mover(VELOCIDADE_LENTA_BUSCA, VELOCIDADE_LENTA_BUSCA);
    } else if (ei && !di) {
        Mover(VELOCIDADE_LENTA_BUSCA * 0.5, VELOCIDADE_LENTA_BUSCA);
    } else if (di && !ei) {
        Mover(VELOCIDADE_LENTA_BUSCA, VELOCIDADE_LENTA_BUSCA * 0.5);
    } else {
        Mover(VELOCIDADE_LENTA_BUSCA, VELOCIDADE_LENTA_BUSCA);
    }
}

async Task Main() {
    IO.OpenConsole();
    EntrarEstado(ESTADO_SEGUIR_LINHA, "SEGUIR LINHA");

    while (true) {
        double frenteEsquerdaCm = LerUltraCm(ULTRA_FRENTE_ESQUERDA);
        double frenteDireitaCm = LerUltraCm(ULTRA_FRENTE_DIREITA);
        double direitaCm = LerUltraCm(ULTRA_DIREITA);
        double esquerdaCm = LerUltraCm(ULTRA_ESQUERDA);

        LogUltrassonicos(frenteEsquerdaCm, frenteDireitaCm, esquerdaCm, direitaCm);

        if (estado == ESTADO_SEGUIR_LINHA) {
            if (ParedeNaFrente(frenteEsquerdaCm, frenteDireitaCm)) {
                EntrarEstado(ESTADO_GIRAR_DIREITA, "DESVIO: GIRAR 90 DIREITA (PARADO)");
            } else {
                SeguirLinhaRapido();
            }
        } else if (estado == ESTADO_GIRAR_DIREITA) {
            Mover(VELOCIDADE_GIRO_PARADO, -VELOCIDADE_GIRO_PARADO);

            bool frenteLivre = FrenteLivre(frenteEsquerdaCm, frenteDireitaCm);
            bool direitaVendoParede = ParedeNaDireita(direitaCm);
            bool tempoEstourou = (AgoraMs() - inicioEstadoMs) > TEMPO_MAX_GIRO_MS;

            if ((frenteLivre && direitaVendoParede) || tempoEstourou) {
                Parar();
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                EntrarEstado(ESTADO_ANDAR_LADO_DA_PAREDE, "DESVIO: ANDAR COM PAREDE NA DIREITA");
            }
        } else if (estado == ESTADO_ANDAR_LADO_DA_PAREDE) {
            Mover(VELOCIDADE_FRENTE, VELOCIDADE_FRENTE);

            bool direitaPerdeuParede = !ParedeNaDireita(direitaCm);
            bool tempoEstourou = (AgoraMs() - inicioEstadoMs) > TEMPO_MAX_ANDAR_LADO_MS;

            if (direitaPerdeuParede || tempoEstourou) {
                Parar();
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                EntrarEstado(ESTADO_GIRAR_PARA_FRENTE, "DESVIO: GIRAR PARA FRENTE");
            }
        } else if (estado == ESTADO_GIRAR_PARA_FRENTE) {
            Mover(-VELOCIDADE_GIRO_PARADO, VELOCIDADE_GIRO_PARADO);

            bool frenteLivre = FrenteLivre(frenteEsquerdaCm, frenteDireitaCm);
            bool esquerdaVendoParede = ParedeNaEsquerda(esquerdaCm);
            bool tempoEstourou = (AgoraMs() - inicioEstadoMs) > TEMPO_MAX_GIRO_MS;

            if ((frenteLivre && esquerdaVendoParede) || tempoEstourou) {
                Parar();
                await Time.Delay(PAUSA_ESTABILIZAR_MS);
                EntrarEstado(ESTADO_PROCURAR_LINHA, "BUSCA: PROCURANDO LINHA PRETA DEVAGAR");
            }
        } else if (estado == ESTADO_PROCURAR_LINHA) {
            if (AlgumSensorLinhaPreto()) {
                EntrarEstado(ESTADO_SEGUIR_LINHA, "SEGUIR LINHA");
            } else {
                ProcurarLinhaDevagar();
            }
        }

        await Time.Delay(INTERVALO_LOOP_MS);
    }
}
