const string MOTOR_ESQUERDO_TRASEIRO = "motor_esquerdo_traseiro";
const string MOTOR_DIREITO_TRASEIRO = "motor_direito_traseiro";
const string MOTOR_DIREITO_FRONTAL = "motor_direito_frontal";
const string MOTOR_ESQUERDO_FRONTAL = "motor_esquerdo_frontal";

const double FORCA_MOTOR = 500;
const double VELOCIDADE_MOTOR = 500;
const double INTERVALO_LOOP_MS = 100;

void LigarMotor(string nomeMotor, double velocidade) {
    Bot.GetComponent<Servomotor>(nomeMotor).Locked = false;
    Bot.GetComponent<Servomotor>(nomeMotor).Apply(FORCA_MOTOR, velocidade);
}

async Task Main() {
    IO.OpenConsole();
    IO.PrintLine("Ligando motores:");
    IO.PrintLine(MOTOR_ESQUERDO_FRONTAL);
    IO.PrintLine(MOTOR_DIREITO_FRONTAL);
    IO.PrintLine(MOTOR_ESQUERDO_TRASEIRO);
    IO.PrintLine(MOTOR_DIREITO_TRASEIRO);

    while (true) {
        LigarMotor(MOTOR_ESQUERDO_FRONTAL, VELOCIDADE_MOTOR);
        LigarMotor(MOTOR_DIREITO_FRONTAL, VELOCIDADE_MOTOR);
        LigarMotor(MOTOR_ESQUERDO_TRASEIRO, VELOCIDADE_MOTOR);
        LigarMotor(MOTOR_DIREITO_TRASEIRO, VELOCIDADE_MOTOR);
        await Time.Delay(INTERVALO_LOOP_MS);
    }
}
