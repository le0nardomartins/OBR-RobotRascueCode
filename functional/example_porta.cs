const string SERVO_PORTA = "servo_porta";

void MoverServo(string servo, double velocidade) {
    Bot.GetComponent<Servomotor>(servo).Locked = false;
    Bot.GetComponent<Servomotor>(servo).Apply(500, velocidade);
}

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

async Task Main() {
    await LevantarPorta();
    await Time.Delay(1000);

    await FecharPorta();
}