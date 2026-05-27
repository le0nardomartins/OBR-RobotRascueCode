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


// =======================  =======================



// ======================= Inicializacao =======================
async Task Inicializacao() { 
    await FecharPorta();
    await Time.Delay(1000);
    await PreparacaoInicialBraco();

}


// ======================= Main =======================
async Task Main() {
    await Inicializacao();

}