// Advanced OBR routine for sBotics (C#)
// Tune component names and constants before running in your robot.

const string LEFT_FRONT_MOTOR = "lf";
const string RIGHT_FRONT_MOTOR = "rf";
const string LEFT_REAR_MOTOR = "lb";
const string RIGHT_REAR_MOTOR = "rb";
const string LEFT_LINE_SENSOR = "sl";
const string RIGHT_LINE_SENSOR = "sr";
const string FRONT_LINE_SENSOR = "fc"; // optional
const string BALL_COLOR_SENSOR = "bc"; // optional
const string FRONT_ULTRA_SENSOR = "ultrassonico"; // optional
const string LEFT_WALL_ULTRA_SENSOR = "ultrassonico_esquerda"; // optional
const string RIGHT_WALL_ULTRA_SENSOR = "ultrassonico_direita"; // optional
const string CAMERA_SENSOR = "camera"; // optional (limited support in sBotics)
const string STATUS_LED = "led"; // optional

const double BASE_SPEED = 220;
const double MAX_TURN = 170;
const double BLACK_BRIGHTNESS_MAX = 75;
const double DOTTED_LINE_GRACE_MS = 260;
const double LOOP_DELAY_MS = 12;
const double SEARCH_STEP_MS = 120;
const double SEARCH_TURN_SPEED = 230;
const double SEARCH_FORWARD_SPEED = 180;
const double SEARCH_MAX_MS = 2400;
const double RESCUE_TIMEOUT_MS = 22000;
const double VICTIM_APPROACH_ULTRA = 10;
const double REAR_MOTOR_FACTOR = 1.0;
const double WALL_NEAR_THRESHOLD = 7.0;
const double WALL_CRITICAL_THRESHOLD = 4.5;
const double WALL_AVOID_BACK_MS = 220;
const double WALL_AVOID_TURN_MS = 320;
const double WALL_AVOID_FORWARD_MS = 180;
const double WALL_AVOID_COOLDOWN_MS = 500;
const int VISION_STOP_HOLD_TICKS = 75;
const bool ENABLE_CAMERA_STOP_BEHAVIOR = false;

bool hasFrontLineSensor = false;
bool hasBallColorSensor = false;
bool hasFrontUltraSensor = false;
bool hasLeftWallUltraSensor = false;
bool hasRightWallUltraSensor = false;
bool hasCameraSensor = false;
bool hasLed = false;
bool hasLeftRearMotor = false;
bool hasRightRearMotor = false;

bool rescueDone = false;
bool finishReached = false;

double lastLineSeenMs = 0;
int lastTurnDirection = 1; // 1 = right, -1 = left
double roomStartHeading = 0;
double wallAvoidCooldownUntilMs = 0;

string leftLineSensorName = LEFT_LINE_SENSOR;
string rightLineSensorName = RIGHT_LINE_SENSOR;
string frontLineSensorName = FRONT_LINE_SENSOR;
string ballColorSensorName = BALL_COLOR_SENSOR;
string frontUltraSensorName = FRONT_ULTRA_SENSOR;
string leftWallUltraSensorName = LEFT_WALL_ULTRA_SENSOR;
string rightWallUltraSensorName = RIGHT_WALL_ULTRA_SENSOR;
string cameraSensorName = CAMERA_SENSOR;
string leftFrontMotorName = LEFT_FRONT_MOTOR;
string rightFrontMotorName = RIGHT_FRONT_MOTOR;
string leftRearMotorName = LEFT_REAR_MOTOR;
string rightRearMotorName = RIGHT_REAR_MOTOR;

string visionState = "LIVRE"; // Inspired by detector_pista.py (LIVRE/STOP/OBSTACULO)
int visionStopTicks = 0;
bool wallAvoidHandled = false;
bool lineFoundInScan = false;

double NowMs() {
    return Time.Timestamp * 1000.0;
}

double Clamp(double value, double minValue, double maxValue) {
    if (value < minValue) return minValue;
    if (value > maxValue) return maxValue;
    return value;
}

double NormalizeAngle(double angle) {
    while (angle < 0) angle += 360;
    while (angle >= 360) angle -= 360;
    return angle;
}

double AngleError(double target, double current) {
    double err = NormalizeAngle(target) - NormalizeAngle(current);
    while (err > 180) err -= 360;
    while (err < -180) err += 360;
    return err;
}

bool HasComponent(string name) {
    string[] components = Bot.Components();
    for (int i = 0; i < components.Length; i++) {
        if (components[i] == name) return true;
    }
    return false;
}

string ResolveName(string preferredName, string altName1 = "", string altName2 = "", string altName3 = "") {
    if ((preferredName != "") && HasComponent(preferredName)) return preferredName;
    if ((altName1 != "") && HasComponent(altName1)) return altName1;
    if ((altName2 != "") && HasComponent(altName2)) return altName2;
    if ((altName3 != "") && HasComponent(altName3)) return altName3;
    return preferredName;
}

Color ReadColor(string sensorName) {
    return Bot.GetComponent<ColorSensor>(sensorName).Analog;
}

double ReadBrightness(string sensorName) {
    return Bot.GetComponent<ColorSensor>(sensorName).Analog.Brightness;
}

bool IsBlack(Color color) {
    return (color.Closest() == Colors.Black) || (color.Brightness <= BLACK_BRIGHTNESS_MAX);
}

bool IsRed(Color color) {
    return color.Closest() == Colors.Red;
}

bool IsGreen(Color color) {
    return color.Closest() == Colors.Green;
}

bool IsYellow(Color color) {
    return color.Closest() == Colors.Yellow;
}

bool IsWhite(Color color) {
    return color.Closest() == Colors.White;
}

bool IsStopLikeColor(Color color) {
    return IsRed(color) || IsYellow(color);
}

void SetLed(Color color) {
    if (!hasLed) return;
    Bot.GetComponent<Light>(STATUS_LED).TurnOn(color);
}

double ReadUltraSafe(string sensorName) {
    double d = Bot.GetComponent<UltrasonicSensor>(sensorName).Analog;
    if (d < 0) return 9999;
    return d;
}

async Task Drive(double leftTarget, double rightTarget) {
    double leftRearTarget = leftTarget * REAR_MOTOR_FACTOR;
    double rightRearTarget = rightTarget * REAR_MOTOR_FACTOR;

    Bot.GetComponent<Servomotor>(leftFrontMotorName).Locked = false;
    Bot.GetComponent<Servomotor>(rightFrontMotorName).Locked = false;
    Bot.GetComponent<Servomotor>(leftFrontMotorName).Apply(Math.Abs(leftTarget), leftTarget);
    Bot.GetComponent<Servomotor>(rightFrontMotorName).Apply(Math.Abs(rightTarget), rightTarget);

    if (hasLeftRearMotor) {
        Bot.GetComponent<Servomotor>(leftRearMotorName).Locked = false;
        Bot.GetComponent<Servomotor>(leftRearMotorName).Apply(Math.Abs(leftRearTarget), leftRearTarget);
    }

    if (hasRightRearMotor) {
        Bot.GetComponent<Servomotor>(rightRearMotorName).Locked = false;
        Bot.GetComponent<Servomotor>(rightRearMotorName).Apply(Math.Abs(rightRearTarget), rightRearTarget);
    }
}

async Task StopDrive() {
    await Drive(0, 0);
}

async Task DriveFor(double leftTarget, double rightTarget, double ms) {
    await Drive(leftTarget, rightTarget);
    await Time.Delay(ms);
}

async Task TurnToHeading(double targetHeading, double maxMs = 2200) {
    double start = NowMs();
    while ((NowMs() - start) < maxMs) {
        double error = AngleError(targetHeading, Bot.Compass);
        if (Math.Abs(error) < 4) break;
        double turn = Clamp(error * 3.0, -280, 280);
        await Drive(-turn, turn);
        await Time.Delay(12);
    }
    await StopDrive();
}

async Task BlinkLedFinish() {
    if (!hasLed) return;
    for (int i = 0; i < 8; i++) {
        SetLed(new Color(255, 0, 0));
        await Time.Delay(70);
        SetLed(new Color(0, 255, 0));
        await Time.Delay(70);
        SetLed(new Color(0, 0, 255));
        await Time.Delay(70);
    }
}

bool ShouldEnterRescueRoom(Color leftColor, Color rightColor, Color frontColor) {
    bool leftBlack = IsBlack(leftColor);
    bool rightBlack = IsBlack(rightColor);
    bool frontBlack = hasFrontLineSensor && IsBlack(frontColor);
    bool noLineVisible = !leftBlack && !rightBlack && (!hasFrontLineSensor || !frontBlack);

    bool sideHint = IsGreen(leftColor) || IsRed(leftColor) || IsGreen(rightColor) || IsRed(rightColor);
    bool frontHint = hasFrontLineSensor && (IsGreen(frontColor) || IsRed(frontColor));

    return (!rescueDone) && noLineVisible && (sideHint || frontHint) && ((NowMs() - lastLineSeenMs) > 120);
}

void UpdateVisionState(bool flagStop, bool flagObstacle) {
    if (visionState == "STOP") {
        if (visionStopTicks > 0) {
            visionStopTicks--;
            return;
        }
        if (!flagStop) {
            visionState = "LIVRE";
        }
        return;
    }

    if (visionState == "OBSTACULO") {
        if (!flagObstacle) {
            visionState = "LIVRE";
        }
        return;
    }

    // LIVRE
    if (flagStop) {
        visionState = "STOP";
        visionStopTicks = VISION_STOP_HOLD_TICKS;
        return;
    }

    if (flagObstacle) {
        visionState = "OBSTACULO";
        return;
    }

    visionState = "LIVRE";
}

async Task HandleWallAndSearch(double frontDistance, double leftDistance, double rightDistance) {
    wallAvoidHandled = false;

    if (NowMs() < wallAvoidCooldownUntilMs) {
        return;
    }

    bool frontNear = frontDistance <= WALL_NEAR_THRESHOLD;
    bool leftNear = leftDistance <= WALL_NEAR_THRESHOLD;
    bool rightNear = rightDistance <= WALL_NEAR_THRESHOLD;
    if (!frontNear && !leftNear && !rightNear) {
        return;
    }

    SetLed(new Color(255, 150, 0));

    // Side choice: turn away from the nearest wall.
    int avoidDirection = 1; // 1 = turn right, -1 = turn left
    if (leftNear && !rightNear) avoidDirection = 1;
    else if (rightNear && !leftNear) avoidDirection = -1;
    else if (leftDistance <= rightDistance) avoidDirection = 1;
    else avoidDirection = -1;

    if (frontDistance <= WALL_CRITICAL_THRESHOLD) {
        await DriveFor(-220, -220, WALL_AVOID_BACK_MS);
    } else {
        await StopDrive();
        await Time.Delay(40);
    }

    // Execute a short obstacle-avoid trajectory.
    if (avoidDirection > 0) {
        await DriveFor(240, -200, WALL_AVOID_TURN_MS);
    } else {
        await DriveFor(-200, 240, WALL_AVOID_TURN_MS);
    }
    await DriveFor(SEARCH_FORWARD_SPEED, SEARCH_FORWARD_SPEED, WALL_AVOID_FORWARD_MS);

    // Enter line-search mode after avoiding wall.
    lastLineSeenMs = 0;
    wallAvoidCooldownUntilMs = NowMs() + WALL_AVOID_COOLDOWN_MS;
    await ReacquireLineScan();
    wallAvoidHandled = true;
}

async Task ReacquireLineScan() {
    SetLed(new Color(255, 0, 255));
    double searchStart = NowMs();
    int direction = lastTurnDirection;
    int sweep = 1;
    lineFoundInScan = false;

    while ((NowMs() - searchStart) < SEARCH_MAX_MS) {
        Color left = ReadColor(leftLineSensorName);
        Color right = ReadColor(rightLineSensorName);
        bool leftBlack = IsBlack(left);
        bool rightBlack = IsBlack(right);
        if (leftBlack || rightBlack) {
            lastLineSeenMs = NowMs();
            if (leftBlack && !rightBlack) lastTurnDirection = -1;
            if (rightBlack && !leftBlack) lastTurnDirection = 1;
            lineFoundInScan = true;
            return;
        }

        if (sweep % 2 == 1) direction = lastTurnDirection;
        else direction = -lastTurnDirection;

        double turnPower = SEARCH_TURN_SPEED + (sweep * 18);
        turnPower = Clamp(turnPower, SEARCH_TURN_SPEED, 320);
        await Drive(-direction * turnPower, direction * turnPower);
        await Time.Delay(SEARCH_STEP_MS);
        await Drive(SEARCH_FORWARD_SPEED, SEARCH_FORWARD_SPEED);
        await Time.Delay(70);
        sweep++;
    }

    await StopDrive();
    lineFoundInScan = false;
}

async Task FollowLineStep() {
    Color leftColor = ReadColor(leftLineSensorName);
    Color rightColor = ReadColor(rightLineSensorName);
    Color frontColor = new Color(0, 0, 0);
    if (hasFrontLineSensor) {
        frontColor = ReadColor(frontLineSensorName);
    }

    double frontDistance = hasFrontUltraSensor ? ReadUltraSafe(frontUltraSensorName) : 9999;
    double leftDistance = hasLeftWallUltraSensor ? ReadUltraSafe(leftWallUltraSensorName) : 9999;
    double rightDistance = hasRightWallUltraSensor ? ReadUltraSafe(rightWallUltraSensorName) : 9999;

    bool redDetected = IsRed(leftColor) || IsRed(rightColor) || (hasFrontLineSensor && IsRed(frontColor));
    if (redDetected && rescueDone) {
        finishReached = true;
        await StopDrive();
        await BlinkLedFinish();
        return;
    }

    // Camera support inspired by detector_pista.py state machine.
    bool cameraStopFlag = ENABLE_CAMERA_STOP_BEHAVIOR && hasCameraSensor && hasFrontLineSensor && IsStopLikeColor(frontColor) && !rescueDone;
    bool wallObstacleFlag = (frontDistance <= WALL_NEAR_THRESHOLD) || (leftDistance <= WALL_NEAR_THRESHOLD) || (rightDistance <= WALL_NEAR_THRESHOLD);
    UpdateVisionState(cameraStopFlag, wallObstacleFlag);

    if (visionState == "STOP") {
        SetLed(new Color(255, 0, 0));
        await StopDrive();
        return;
    }

    await HandleWallAndSearch(frontDistance, leftDistance, rightDistance);
    if (wallAvoidHandled) {
        return;
    }

    if (ShouldEnterRescueRoom(leftColor, rightColor, frontColor)) {
        await StopDrive();
        await Time.Delay(80);
        roomStartHeading = Bot.Compass;
        await RunRescueRoom();
        return;
    }

    bool leftBlack = IsBlack(leftColor);
    bool rightBlack = IsBlack(rightColor);
    bool frontBlack = hasFrontLineSensor && IsBlack(frontColor);

    if (leftBlack || rightBlack || frontBlack) {
        lastLineSeenMs = NowMs();
    }

    if (leftBlack && rightBlack) {
        SetLed(new Color(0, 255, 0));
        await Drive(BASE_SPEED, BASE_SPEED);
        return;
    }

    if (leftBlack && !rightBlack) {
        SetLed(new Color(255, 90, 0));
        lastTurnDirection = -1;
        await Drive(BASE_SPEED * 0.45, BASE_SPEED + MAX_TURN);
        return;
    }

    if (!leftBlack && rightBlack) {
        SetLed(new Color(0, 140, 255));
        lastTurnDirection = 1;
        await Drive(BASE_SPEED + MAX_TURN, BASE_SPEED * 0.45);
        return;
    }

    if (frontBlack) {
        SetLed(new Color(0, 255, 160));
        await Drive(BASE_SPEED, BASE_SPEED);
        return;
    }

    double lostFor = NowMs() - lastLineSeenMs;
    if (lostFor <= DOTTED_LINE_GRACE_MS) {
        // Dotted line support: keep moving with memory for short white gaps.
        if (lastTurnDirection > 0) {
            await Drive(BASE_SPEED + 70, BASE_SPEED - 25);
        } else {
            await Drive(BASE_SPEED - 25, BASE_SPEED + 70);
        }
        return;
    }

    // Full line loss (gap / half-circle area): active re-acquire search.
    await ReacquireLineScan();
    if (!lineFoundInScan) {
        await Drive(SEARCH_FORWARD_SPEED, SEARCH_FORWARD_SPEED);
    }
}

bool DetectVictimAhead() {
    if (!hasFrontUltraSensor) return true;
    double d = Bot.GetComponent<UltrasonicSensor>(frontUltraSensorName).Analog;
    return (d > 0) && (d <= VICTIM_APPROACH_ULTRA);
}

Colors ReadVictimType() {
    Color c = new Color(255, 255, 255);

    if (hasBallColorSensor) {
        c = ReadColor(ballColorSensorName);
    } else if (hasFrontLineSensor) {
        // Fallback when there is no dedicated victim color sensor.
        c = ReadColor(frontLineSensorName);
    } else if (hasCameraSensor) {
        // Vision support inspired by detector_pista.py:
        // use a proxy strategy to preserve competition behavior in sBotics.
        if (hasFrontLineSensor) {
            c = ReadColor(frontLineSensorName);
            if (IsBlack(c)) return Colors.Black;
            return Colors.White;
        }
        return Colors.White;
    } else {
        return Colors.White;
    }

    if (IsBlack(c)) return Colors.Black;
    return c.Closest();
}

async Task PushToRedCorner() {
    // Dead victim (black) -> red corner (top-right from room reference).
    await TurnToHeading(NormalizeAngle(roomStartHeading - 40));
    await DriveFor(250, 250, 700);
    await DriveFor(240, 180, 520);
    await DriveFor(260, 260, 620);
    await DriveFor(-230, -230, 430);
}

async Task PushToGreenCorner() {
    // Live victim (white/silver) -> green corner (bottom-left from room reference).
    await TurnToHeading(NormalizeAngle(roomStartHeading + 145));
    await DriveFor(250, 250, 720);
    await DriveFor(180, 240, 520);
    await DriveFor(260, 260, 620);
    await DriveFor(-230, -230, 430);
}

async Task SweepForVictim() {
    await DriveFor(170, -170, 420);
    await DriveFor(-170, 170, 840);
    await DriveFor(170, -170, 420);
    await DriveFor(200, 200, 280);
}

async Task GoToRoomCenter() {
    await TurnToHeading(roomStartHeading);
    await DriveFor(230, 230, 420);
}

async Task ExitRescueRoom() {
    // Return to line continuation side (towards black line after rescue room).
    await TurnToHeading(NormalizeAngle(roomStartHeading));
    await DriveFor(260, 260, 950);
    await DriveFor(240, 120, 530);
    await DriveFor(220, 220, 380);
    await ReacquireLineScan();
}

async Task RunRescueRoom() {
    SetLed(new Color(255, 255, 0));
    double rescueStart = NowMs();
    int rescued = 0;
    int maxVictims = 3; // OBR: usually 2 white/silver + 1 black.

    await GoToRoomCenter();

    while ((NowMs() - rescueStart) < RESCUE_TIMEOUT_MS && rescued < maxVictims) {
        bool victimAhead = DetectVictimAhead();
        if (!victimAhead) {
            await SweepForVictim();
            continue;
        }

        await DriveFor(170, 170, 240);
        Colors victimType = ReadVictimType();

        if (victimType == Colors.Black) {
            SetLed(new Color(255, 30, 30));
            await PushToRedCorner();
        } else {
            SetLed(new Color(30, 255, 30));
            await PushToGreenCorner();
        }

        rescued++;
        await GoToRoomCenter();
    }

    rescueDone = true;
    await ExitRescueRoom();
    SetLed(new Color(0, 255, 0));
}

async Task Initialize() {
    IO.OpenConsole();
    leftLineSensorName = ResolveName(LEFT_LINE_SENSOR, "se", "lc", "sr");
    rightLineSensorName = ResolveName(RIGHT_LINE_SENSOR, "sd", "rc", "right");
    frontLineSensorName = ResolveName(FRONT_LINE_SENSOR, "sf", "frente", "");
    ballColorSensorName = ResolveName(BALL_COLOR_SENSOR, "cor_bola", "", "");
    frontUltraSensorName = ResolveName(FRONT_ULTRA_SENSOR, "us", "ultra", "");
    leftWallUltraSensorName = ResolveName(LEFT_WALL_ULTRA_SENSOR, "usl", "", "");
    rightWallUltraSensorName = ResolveName(RIGHT_WALL_ULTRA_SENSOR, "usr", "", "");
    cameraSensorName = ResolveName(CAMERA_SENSOR, "cam", "", "");

    leftFrontMotorName = ResolveName(LEFT_FRONT_MOTOR, "l", "", "");
    rightFrontMotorName = ResolveName(RIGHT_FRONT_MOTOR, "r", "", "");
    leftRearMotorName = ResolveName(LEFT_REAR_MOTOR, "l2", "", "");
    rightRearMotorName = ResolveName(RIGHT_REAR_MOTOR, "r2", "", "");

    hasFrontLineSensor = HasComponent(frontLineSensorName);
    hasBallColorSensor = HasComponent(ballColorSensorName);
    hasFrontUltraSensor = HasComponent(frontUltraSensorName);
    hasLeftWallUltraSensor = HasComponent(leftWallUltraSensorName);
    hasRightWallUltraSensor = HasComponent(rightWallUltraSensorName);
    hasCameraSensor = HasComponent(cameraSensorName);
    hasLed = HasComponent(STATUS_LED);
    hasLeftRearMotor = HasComponent(leftRearMotorName);
    hasRightRearMotor = HasComponent(rightRearMotorName);

    lastLineSeenMs = NowMs();

    IO.PrintLine("OBR routine started");
    IO.PrintLine("Front motor L/R: " + leftFrontMotorName + " / " + rightFrontMotorName);
    IO.PrintLine("Rear motor L/R available: " + hasLeftRearMotor + " / " + hasRightRearMotor);
    IO.PrintLine("Line sensor L/R: " + leftLineSensorName + " / " + rightLineSensorName);
    IO.PrintLine("Front line sensor: " + hasFrontLineSensor + " (" + frontLineSensorName + ")");
    IO.PrintLine("Ball color sensor: " + hasBallColorSensor + " (" + ballColorSensorName + ")");
    IO.PrintLine("Front ultra sensor: " + hasFrontUltraSensor + " (" + frontUltraSensorName + ")");
    IO.PrintLine("Left wall ultra sensor: " + hasLeftWallUltraSensor + " (" + leftWallUltraSensorName + ")");
    IO.PrintLine("Right wall ultra sensor: " + hasRightWallUltraSensor + " (" + rightWallUltraSensorName + ")");
    IO.PrintLine("Camera sensor available: " + hasCameraSensor + " (" + cameraSensorName + ")");
    IO.PrintLine("LED: " + hasLed);
}

async Task Main() {
    await Initialize();
    while (true) {
        await Time.Delay(LOOP_DELAY_MS);
        if (finishReached) {
            await StopDrive();
            continue;
        }
        await FollowLineStep();
    }
}
