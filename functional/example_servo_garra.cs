const string SERVO_BRACO = "servo_garra_braco";

void MoverServo(string servo, double velocidade) {
    Bot.GetComponent<Servomotor>(servo).Locked = false;
    Bot.GetComponent<Servomotor>(servo).Apply(500, velocidade);
}


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


async Task Main() {

	await PreparacaoInicialBraco();

    await Time.Delay(1000); 

    await AbaixarBraco();

    await Time.Delay(1000); 

    await LevantarBraco();

    await AbaixarBraco();
    
}