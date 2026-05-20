import cv2
import numpy as np
import json
from pathlib import Path
from PID import PID

BASE_DIR = Path(__file__).resolve().parent

# ┌─────────────────────────────────────────────────────────────────────────┐
# │  CONFIGURAÇÃO DE ENTRADA                                                │
# │  MODE = "video" | "image"                                               │
# └─────────────────────────────────────────────────────────────────────────┘
MODE       = "image"
IMAGE_NAME = "image_1.png"

VIDEO_PATH = BASE_DIR / "videos" / "video.mp4"
IMAGE_PATH = BASE_DIR / "images" / IMAGE_NAME

ROI_W = 320
ROI_H = 240

DEFAULT_UPPER  = 1280
DEFAULT_LOWER  = 1280
DEFAULT_Y_TOP  = 263
DEFAULT_Y_BOT  = 541
DEFAULT_LIMIAR = 195
DEFAULT_VEL    = 30

# ── Máquina de estados ────────────────────────────────────────────────────
STATE_LIVRE     = "LIVRE"
STATE_STOP      = "STOP"
STATE_OBSTACULO = "OBSTACULO"

OBSTACLE_THRESHOLD  = 0.04   # área da bbox / área do frame → "próximo"
OBSTACLE_DESVIO_PX  = 60     # pixels de desvio lateral ao evitar obstáculo
STOP_WAIT_FRAMES    = 90     # frames parado ao ver STOP/vermelho (~3s a 30fps)
OBSTACLE_CLASSES    = {0, 1, 2, 3, 5, 7}  # person, bicycle, car, motorcycle, bus, truck

# ── Detectores YOLO ──────────────────────────────────────────────────────
sign_detector     = None
obstacle_detector = None

try:
    from ultralytics import YOLO

    _sign_path = BASE_DIR / "model" / "traffic_sign_detector.pt"
    if _sign_path.exists():
        sign_detector = YOLO(str(_sign_path))
    else:
        print(f"[Sinais] Modelo nao encontrado: {_sign_path}")

    obstacle_detector = YOLO("yolo11n.pt")   # baixa automaticamente na 1ª execução
    print("[Obstaculos] Detector COCO carregado (yolo11n).")

except ImportError:
    print("[YOLO] ultralytics nao instalado. Rode: pip install ultralytics==8.3.5")

sign_boxes:     list = []
obstacle_boxes: list = []
detect_counter       = 0
DETECT_INTERVAL      = 5


# ── Detecção de sinais ────────────────────────────────────────────────────

def detect_signs(frame: np.ndarray) -> list:
    """Retorna [(x1,y1,x2,y2,label,conf), ...]"""
    if sign_detector is None:
        return []
    out = []
    for r in sign_detector.predict(frame, verbose=False, conf=0.4):
        for box in r.boxes:
            x1, y1, x2, y2 = (int(v) for v in box.xyxy[0])
            out.append((x1, y1, x2, y2, r.names[int(box.cls[0])], float(box.conf[0])))
    return out


def signs_to_flags(boxes: list) -> tuple:
    """(STOP, SG=verde, SV=vermelho/amarelo)"""
    stop, sg, sv = False, False, False
    for *_, label, _ in boxes:
        lbl = label.lower()
        if "stop" in lbl:            stop = True
        if "green" in lbl:           sg   = True
        if "red" in lbl or "yellow" in lbl: sv = True
    return stop, sg, sv


def draw_sign_boxes(img: np.ndarray, boxes: list) -> None:
    for x1, y1, x2, y2, label, conf in boxes:
        cv2.rectangle(img, (x1, y1), (x2, y2), (0, 165, 255), 2)
        cv2.putText(img, f"{label} {conf:.2f}", (x1, max(y1 - 6, 10)),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 165, 255), 1)


# ── Detecção de obstáculos ────────────────────────────────────────────────

def detect_obstacles(frame: np.ndarray) -> list:
    """Retorna [(x1,y1,x2,y2,label,conf,proximity), ...] sorted por proximidade."""
    if obstacle_detector is None:
        return []
    h, w  = frame.shape[:2]
    farea = h * w
    out   = []
    for r in obstacle_detector.predict(frame, verbose=False, conf=0.4,
                                       classes=list(OBSTACLE_CLASSES)):
        for box in r.boxes:
            x1, y1, x2, y2 = (int(v) for v in box.xyxy[0])
            proximity = ((x2 - x1) * (y2 - y1)) / farea
            out.append((x1, y1, x2, y2, r.names[int(box.cls[0])],
                        float(box.conf[0]), proximity))
    return sorted(out, key=lambda b: b[6], reverse=True)


def draw_obstacle_boxes(img: np.ndarray, boxes: list) -> None:
    for x1, y1, x2, y2, label, _, proximity in boxes:
        color = (0, 0, 220) if proximity > OBSTACLE_THRESHOLD else (0, 200, 255)
        cv2.rectangle(img, (x1, y1), (x2, y2), color, 2)
        cv2.putText(img, f"{label} {proximity:.0%}", (x1, max(y1 - 6, 10)),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.45, color, 1)


def max_proximity(boxes: list) -> float:
    return boxes[0][6] if boxes else 0.0


# ── Máquina de estados ────────────────────────────────────────────────────

def next_state(state: str, timer: int, flag_stop: bool, flag_sv: bool,
               prox: float) -> tuple:
    """Retorna (novo_state, novo_timer)."""
    if state == STATE_STOP:
        if timer > 0:
            return STATE_STOP, timer - 1
        if not flag_stop and not flag_sv:
            return STATE_LIVRE, 0
        return STATE_STOP, 0

    if state == STATE_OBSTACULO:
        if prox <= OBSTACLE_THRESHOLD:
            return STATE_LIVRE, 0
        return STATE_OBSTACULO, 0

    # STATE_LIVRE
    if flag_stop or flag_sv:
        return STATE_STOP, STOP_WAIT_FRAMES
    if prox > OBSTACLE_THRESHOLD:
        return STATE_OBSTACULO, 0
    return STATE_LIVRE, 0


def state_effects(state: str, vel_base: int,
                  obs_boxes: list, frame_width: int) -> tuple:
    """Retorna (effective_vel, cx_offset_extra)."""
    if state == STATE_STOP:
        return 0, 0
    if state == STATE_OBSTACULO and obs_boxes:
        ox = (obs_boxes[0][0] + obs_boxes[0][2]) // 2
        desvio = OBSTACLE_DESVIO_PX if ox < frame_width // 2 else -OBSTACLE_DESVIO_PX
        return max(vel_base // 2, 1), desvio
    return vel_base, 0


# ── Utilitários de visão ──────────────────────────────────────────────────

def nothing(x):
    pass


def drawDots(img, points, labels):
    for i, p in enumerate(points):
        x, y = int(p[0]), int(p[1])
        cv2.circle(img, (x, y), 5, (0, 255, 255), -1)
        cv2.putText(img, labels[i], (x + 6, y - 6),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.45, (0, 255, 255), 1)


def pidHub(erro, pid_reta, pid_curva, dt=0.033):
    if -cv2.getTrackbarPos("Erro", "Comandos") < erro < cv2.getTrackbarPos("Erro", "Comandos"):
        return pid_reta.update(erro, dt=dt)
    return pid_curva.update(erro, dt=dt)


def lane_pipeline(frame: np.ndarray, upper: int, lower: int,
                  y_top: int, y_bot: int, limiar_value: int, cx_offset: int = 0):
    """Retorna (img_anotada, limiar_bgr, erro_px)."""
    img = frame.copy()
    _, width = frame.shape[:2]
    cx = width // 2 + cx_offset

    pts_origem = np.float32([
        [cx - upper // 2, y_top],
        [cx + upper // 2, y_top],
        [cx + lower // 2, y_bot],
        [cx - lower // 2, y_bot],
    ])
    pts_destino = np.float32([
        [0,     0    ],
        [ROI_W, 0    ],
        [ROI_W, ROI_H],
        [0,     ROI_H],
    ])

    pts_poly = pts_origem.astype(np.int32).reshape((-1, 1, 2))
    overlay  = img.copy()
    cv2.fillPoly(overlay, [pts_poly], (255, 0, 0))
    cv2.addWeighted(overlay, 0.3, img, 0.7, 0, img)
    cv2.polylines(img, [pts_poly], True, (255, 0, 0), 2)
    drawDots(img, pts_origem, ["P1", "P2", "P3", "P4"])

    M   = cv2.getPerspectiveTransform(pts_origem, pts_destino)
    roi = cv2.warpPerspective(frame, M, (ROI_W, ROI_H))

    gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
    gray = cv2.GaussianBlur(gray, (5, 5), 0)
    _, limiar = cv2.threshold(gray, limiar_value, 255, cv2.THRESH_BINARY)
    limiar_bgr = cv2.cvtColor(limiar, cv2.COLOR_GRAY2BGR)

    mid_y         = ROI_H // 2
    row           = limiar[mid_y]
    left_indices  = np.where(row[:ROI_W // 2] == 255)[0]
    right_indices = np.where(row[ROI_W // 2:] == 255)[0]
    erro = 0
    if len(left_indices) > 0 and len(right_indices) > 0:
        left_x  = left_indices[-1]
        right_x = right_indices[0] + ROI_W // 2
        mid_x   = (left_x + right_x) // 2
        erro    = mid_x - ROI_W // 2
        cv2.line(limiar_bgr, (left_x, mid_y), (right_x, mid_y), (100, 100, 100), 2)
        cv2.circle(limiar_bgr, (mid_x, mid_y), 5, (0, 255, 0), -1)
        cv2.circle(limiar_bgr, (ROI_W // 2, round(ROI_H * 0.9)), 5, (0, 0, 255), -1)

    return img, limiar_bgr, erro


def build_dashboard(img_anotada: np.ndarray, limiar_bgr: np.ndarray,
                    erro: int, angulo: int, vel: int,
                    kp_reta: float, ki_reta: float, kd_reta: float,
                    kp_curva: float, ki_curva: float, kd_curva: float,
                    sign_boxes: list, state: str = STATE_LIVRE,
                    obs_boxes: list = None, prox: float = 0.0) -> np.ndarray:
    if obs_boxes is None:
        obs_boxes = []

    img_view  = cv2.resize(img_anotada, (960, 540))
    bird_view = cv2.resize(limiar_bgr,  (320, 180))

    img_view[10:190, 630:950] = bird_view
    cv2.rectangle(img_view, (630, 10), (950, 190), (0, 255, 255), 2)
    cv2.putText(img_view, "Bird Eye", (635, 205),
                cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 255), 1)

    flag_stop, flag_sg, flag_sv = signs_to_flags(sign_boxes)
    sign_labels = [b[4] for b in sign_boxes] if sign_boxes else ["—"]
    obs_labels  = [b[4] for b in obs_boxes]  if obs_boxes  else ["—"]

    info = np.zeros((160, 960, 3), dtype=np.uint8)

    # Estado da máquina
    state_color = {STATE_LIVRE: (0, 220, 0), STATE_STOP: (0, 0, 220),
                   STATE_OBSTACULO: (0, 140, 255)}.get(state, (200, 200, 200))
    cv2.putText(info, f"Estado: {state}", (20, 30),
                cv2.FONT_HERSHEY_SIMPLEX, 0.8, state_color, 2)

    # Telemetria PID
    cv2.putText(info, f"Erro: {erro}",     (20,  70), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
    cv2.putText(info, f"Servo: {angulo}",  (20, 115), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0),   2)
    cv2.putText(info, f"Vel: {vel}",       (350, 70), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2)
    cv2.putText(info, f"KpR:{kp_reta:.2f} KiR:{ki_reta:.3f} KdR:{kd_reta:.2f}",
                (350, 115), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (255, 255, 0), 2)
    cv2.putText(info, f"KpC:{kp_curva:.2f} KiC:{ki_curva:.3f} KdC:{kd_curva:.2f}",
                (350, 150), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 255, 255), 2)

    # Flags de sinais
    c_stop = (0, 0, 255)   if flag_stop else (60, 60, 60)
    c_sg   = (0, 255, 0)   if flag_sg   else (60, 60, 60)
    c_sv   = (0, 165, 255) if flag_sv   else (60, 60, 60)
    cv2.putText(info, "STOP", (700, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.8, c_stop, 2)
    cv2.putText(info, "SG",   (790, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.8, c_sg,   2)
    cv2.putText(info, "SV",   (855, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.8, c_sv,   2)

    # Sinais detectados
    cv2.putText(info, f"Sinais: {', '.join(sign_labels[:3])}", (700, 75),
                cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 165, 255), 1)

    # Obstáculos detectados + proximidade
    prox_color = (0, 0, 220) if prox > OBSTACLE_THRESHOLD else (0, 200, 120)
    cv2.putText(info, f"Obst: {', '.join(obs_labels[:3])}  {prox:.0%}", (700, 100),
                cv2.FONT_HERSHEY_SIMPLEX, 0.4, prox_color, 1)

    # Barra de proximidade
    bar_w = int(prox * 200)
    cv2.rectangle(info, (700, 110), (900, 125), (50, 50, 50), -1)
    if bar_w > 0:
        cv2.rectangle(info, (700, 110), (700 + min(bar_w, 200), 125), prox_color, -1)
    cv2.putText(info, "Prox.", (905, 122), cv2.FONT_HERSHEY_SIMPLEX, 0.35, (180, 180, 180), 1)

    return np.vstack((img_view, info))


def save_pipeline_steps(frame: np.ndarray, upper: int, lower: int,
                        y_top: int, y_bot: int, limiar_value: int) -> None:
    out_dir = BASE_DIR / "output"
    out_dir.mkdir(exist_ok=True)
    _, w = frame.shape[:2]
    cx = w // 2

    cv2.imwrite(str(out_dir / "01_original.jpg"), frame)

    pts_orig = np.float32([
        [cx - upper // 2, y_top], [cx + upper // 2, y_top],
        [cx + lower // 2, y_bot], [cx - lower // 2, y_bot],
    ])
    pts_dest = np.float32([[0, 0], [ROI_W, 0], [ROI_W, ROI_H], [0, ROI_H]])
    M        = cv2.getPerspectiveTransform(pts_orig, pts_dest)
    roi_bird = cv2.warpPerspective(frame, M, (ROI_W, ROI_H))
    cv2.imwrite(str(out_dir / "02_bird_eye.jpg"), roi_bird)

    gray = cv2.cvtColor(roi_bird, cv2.COLOR_BGR2GRAY)
    cv2.imwrite(str(out_dir / "03_grayscale.jpg"), gray)

    blurred = cv2.GaussianBlur(gray, (5, 5), 0)
    cv2.imwrite(str(out_dir / "04_blur_gaussiano.jpg"), blurred)

    _, thresh = cv2.threshold(blurred, limiar_value, 255, cv2.THRESH_BINARY)
    cv2.imwrite(str(out_dir / "05_threshold.jpg"), thresh)

    result = cv2.cvtColor(thresh, cv2.COLOR_GRAY2BGR)
    mid_y  = ROI_H // 2
    row    = thresh[mid_y]
    l_idx  = np.where(row[:ROI_W // 2] == 255)[0]
    r_idx  = np.where(row[ROI_W // 2:] == 255)[0]
    if len(l_idx) > 0 and len(r_idx) > 0:
        lx  = l_idx[-1]
        rx  = r_idx[0] + ROI_W // 2
        mx  = (lx + rx) // 2
        epx = mx - ROI_W // 2
        cv2.line(result, (lx, mid_y), (rx, mid_y), (80, 80, 80), 2)
        cv2.circle(result, (mx, mid_y), 5, (0, 255, 0), -1)
        cv2.circle(result, (ROI_W // 2, round(ROI_H * 0.9)), 5, (0, 0, 255), -1)
        cv2.putText(result, f"erro={epx}px", (4, ROI_H - 6),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 255, 255), 1)
    cv2.imwrite(str(out_dir / "06_deteccao_faixas.jpg"), result)
    print(f"[Pipeline] 6 imagens salvas em: {out_dir}")


# ══════════════════════════════════════════════════════════════════════════════
#  MODO IMAGEM
# ══════════════════════════════════════════════════════════════════════════════

_CONFIG_PATH = BASE_DIR / "output" / "config.json"


def load_config(width: int) -> dict:
    if _CONFIG_PATH.exists():
        try:
            with open(_CONFIG_PATH) as f:
                return json.load(f)
        except Exception:
            pass
    return {"upper": DEFAULT_UPPER, "lower": DEFAULT_LOWER,
            "y_top": DEFAULT_Y_TOP, "y_bot": DEFAULT_Y_BOT,
            "limiar": DEFAULT_LIMIAR, "desl_x": width // 2}


def save_config(upper: int, lower: int, y_top: int, y_bot: int,
                limiar: int, desl_x: int) -> None:
    _CONFIG_PATH.parent.mkdir(exist_ok=True)
    with open(_CONFIG_PATH, "w") as f:
        json.dump({"upper": upper, "lower": lower, "y_top": y_top,
                   "y_bot": y_bot, "limiar": limiar, "desl_x": desl_x}, f, indent=2)
    print(f"[Config] Salvo → upper={upper} lower={lower} y_top={y_top} "
          f"y_bot={y_bot} limiar={limiar} desl_x={desl_x}")


def create_image_controls(width: int, height: int, cfg: dict) -> None:
    win = "Controles"
    cv2.namedWindow(win, cv2.WINDOW_NORMAL)
    cv2.resizeWindow(win, 500, 320)
    cv2.createTrackbar("Linha superior", win, cfg["upper"],              width,  nothing)
    cv2.createTrackbar("Linha inferior", win, cfg["lower"],              width,  nothing)
    cv2.createTrackbar("Altura sup",     win, cfg["y_top"],              height, nothing)
    cv2.createTrackbar("Altura inf",     win, cfg["y_bot"],              height, nothing)
    cv2.createTrackbar("Desl. X",        win, cfg.get("desl_x", width // 2), width, nothing)
    cv2.createTrackbar("Limiar",         win, cfg["limiar"],             255,    nothing)


def run_image_mode():
    if not IMAGE_PATH.exists():
        print(f"[Erro] Imagem nao encontrada: {IMAGE_PATH}")
        print(f"       Coloque o arquivo em:  images/{IMAGE_NAME}")
        return

    frame = cv2.imread(str(IMAGE_PATH))
    if frame is None:
        print(f"[Erro] Nao foi possivel carregar: {IMAGE_PATH}")
        return

    height, width = frame.shape[:2]
    print(f"[Imagem] {IMAGE_PATH.name}  ({width}x{height})")

    s_boxes = detect_signs(frame)
    o_boxes = detect_obstacles(frame)
    prox    = max_proximity(o_boxes)

    flag_stop, flag_sg, flag_sv = signs_to_flags(s_boxes)
    state, _ = next_state(STATE_LIVRE, 0, flag_stop, flag_sv, prox)

    print(f"[Imagem] Sinais: {[b[4] for b in s_boxes] or ['nenhum']}  "
          f"STOP={flag_stop} SG={flag_sg} SV={flag_sv}")
    print(f"[Imagem] Obstaculos: {[b[4] for b in o_boxes] or ['nenhum']}  "
          f"Prox={prox:.0%}  Estado={state}")
    print("[Imagem] S:Salvar Config  P:Salvar Pipeline  Q/ESC:Sair")

    cfg = load_config(width)
    create_image_controls(width, height, cfg)

    cv2.namedWindow("Visao", cv2.WINDOW_NORMAL)
    cv2.resizeWindow("Visao", 960, 700)

    WIN  = "Controles"
    hint = "S:Salvar Config   P:Salvar Pipeline   Q:Sair"

    while True:
        upper        = cv2.getTrackbarPos("Linha superior", WIN)
        lower        = cv2.getTrackbarPos("Linha inferior", WIN)
        y_top        = cv2.getTrackbarPos("Altura sup",     WIN)
        y_bot        = cv2.getTrackbarPos("Altura inf",     WIN)
        desl_x       = cv2.getTrackbarPos("Desl. X",        WIN)
        limiar_value = cv2.getTrackbarPos("Limiar",         WIN)
        cx_offset    = desl_x - width // 2

        _, cx_extra = state_effects(state, DEFAULT_VEL, o_boxes, width)

        img_anotada, limiar_bgr, erro = lane_pipeline(
            frame, upper, lower, y_top, y_bot, limiar_value, cx_offset + cx_extra
        )
        draw_sign_boxes(img_anotada, s_boxes)
        draw_obstacle_boxes(img_anotada, o_boxes)

        dashboard = build_dashboard(
            img_anotada, limiar_bgr, erro,
            angulo=90, vel=DEFAULT_VEL,
            kp_reta=0, ki_reta=0, kd_reta=0,
            kp_curva=0, ki_curva=0, kd_curva=0,
            sign_boxes=s_boxes, state=state,
            obs_boxes=o_boxes, prox=prox,
        )
        cv2.putText(dashboard, hint, (10, dashboard.shape[0] - 6),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.4, (150, 150, 150), 1)

        cv2.imshow("Visao", dashboard)
        key = cv2.waitKey(30) & 0xFF

        if key == ord('s'):
            save_config(upper, lower, y_top, y_bot, limiar_value, desl_x)
            out_dir = BASE_DIR / "output"
            out_dir.mkdir(exist_ok=True)
            result_path = out_dir / f"{IMAGE_PATH.stem}_result.jpg"
            cv2.imwrite(str(result_path), dashboard)
            print(f"[Imagem] Resultado salvo: {result_path}")

        elif key == ord('p'):
            save_pipeline_steps(frame, upper, lower, y_top, y_bot, limiar_value)

        elif key in (ord('q'), 27):
            break

    cv2.destroyAllWindows()


# ══════════════════════════════════════════════════════════════════════════════
#  MODO VIDEO
# ══════════════════════════════════════════════════════════════════════════════

def createControls(width, height):
    cv2.namedWindow("Comandos", cv2.WINDOW_NORMAL)
    cv2.resizeWindow("Comandos", 600, 900)
    cv2.createTrackbar("-- ROI --",       "Comandos", 0,              1,      nothing)
    cv2.createTrackbar("Linha superior",  "Comandos", DEFAULT_UPPER,  width,  nothing)
    cv2.createTrackbar("Linha inferior",  "Comandos", DEFAULT_LOWER,  width,  nothing)
    cv2.createTrackbar("Altura sup",      "Comandos", DEFAULT_Y_TOP,  height, nothing)
    cv2.createTrackbar("Altura inf",      "Comandos", DEFAULT_Y_BOT,  height, nothing)
    cv2.createTrackbar("-- IMAGE --",     "Comandos", 0,              1,      nothing)
    cv2.createTrackbar("Limiar",          "Comandos", DEFAULT_LIMIAR, 255,    nothing)
    cv2.createTrackbar("Vel",             "Comandos", DEFAULT_VEL,    255,    nothing)
    cv2.createTrackbar("-- RETA --",      "Comandos", 0,              1,      nothing)
    cv2.createTrackbar("Kp reta",         "Comandos", 200,            1000,   nothing)
    cv2.createTrackbar("Ki reta",         "Comandos", 0,              1000,   nothing)
    cv2.createTrackbar("Kd reta",         "Comandos", 5,              1000,   nothing)
    cv2.createTrackbar("-- CURVA --",     "Comandos", 0,              1,      nothing)
    cv2.createTrackbar("Kp curva",        "Comandos", 650,            1000,   nothing)
    cv2.createTrackbar("Ki curva",        "Comandos", 0,              1000,   nothing)
    cv2.createTrackbar("Kd curva",        "Comandos", 0,              1000,   nothing)
    cv2.createTrackbar("-- ERRO --",      "Comandos", 0,              1,      nothing)
    cv2.createTrackbar("Erro",            "Comandos", 12,             100,    nothing)
    cv2.createTrackbar("-- PIPELINE --",  "Comandos", 0,              1,      nothing)
    cv2.createTrackbar("Salvar Pipeline", "Comandos", 0,              1,      nothing)


def run_video_mode():
    global sign_boxes, obstacle_boxes, detect_counter

    if not VIDEO_PATH.exists():
        print(f"[Erro] Video nao encontrado: {VIDEO_PATH}")
        return

    cap = cv2.VideoCapture(str(VIDEO_PATH))
    ret, frame = cap.read()
    if not ret:
        print(f"[Erro] Nao foi possivel abrir: {VIDEO_PATH}")
        cap.release()
        return

    height, width = frame.shape[:2]

    pid_reta  = PID(Kp=0, Ki=0, Kd=0, output_limit=90.0)
    pid_curva = PID(Kp=0, Ki=0, Kd=0, output_limit=90.0)

    createControls(width, height)
    cv2.namedWindow("Visao", cv2.WINDOW_NORMAL)
    cv2.resizeWindow("Visao", 960, 700)

    estado = STATE_LIVRE
    timer  = 0
    erro   = 0
    angulo = 0

    while True:
        ret, frame = cap.read()
        if not ret:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            continue

        detect_counter += 1
        if detect_counter >= DETECT_INTERVAL:
            detect_counter = 0
            sign_boxes     = detect_signs(frame)
            obstacle_boxes = detect_obstacles(frame)

        flag_stop, _, flag_sv = signs_to_flags(sign_boxes)
        prox = max_proximity(obstacle_boxes)

        estado, timer = next_state(estado, timer, flag_stop, flag_sv, prox)
        vel_base      = cv2.getTrackbarPos("Vel", "Comandos")
        eff_vel, cx_extra = state_effects(estado, vel_base, obstacle_boxes, width)

        upper        = cv2.getTrackbarPos("Linha superior", "Comandos")
        lower        = cv2.getTrackbarPos("Linha inferior", "Comandos")
        y_top        = cv2.getTrackbarPos("Altura sup",     "Comandos")
        y_bot        = cv2.getTrackbarPos("Altura inf",     "Comandos")
        limiar_value = cv2.getTrackbarPos("Limiar",         "Comandos")

        kp_reta  = cv2.getTrackbarPos("Kp reta",  "Comandos") / 100.0
        ki_reta  = cv2.getTrackbarPos("Ki reta",  "Comandos") / 1000.0
        kd_reta  = cv2.getTrackbarPos("Kd reta",  "Comandos") / 100.0
        pid_reta.setValues(kp_reta, ki_reta, kd_reta)

        kp_curva = cv2.getTrackbarPos("Kp curva", "Comandos") / 100.0
        ki_curva = cv2.getTrackbarPos("Ki curva", "Comandos") / 1000.0
        kd_curva = cv2.getTrackbarPos("Kd curva", "Comandos") / 100.0
        pid_curva.setValues(kp_curva, ki_curva, kd_curva)

        if cv2.getTrackbarPos("Salvar Pipeline", "Comandos") == 1:
            save_pipeline_steps(frame, upper, lower, y_top, y_bot, limiar_value)
            cv2.setTrackbarPos("Salvar Pipeline", "Comandos", 0)

        img_anotada, limiar_bgr, erro = lane_pipeline(
            frame, upper, lower, y_top, y_bot, limiar_value, cx_extra
        )
        draw_sign_boxes(img_anotada, sign_boxes)
        draw_obstacle_boxes(img_anotada, obstacle_boxes)

        angulo = pidHub(erro, pid_reta, pid_curva, dt=0.033)

        dashboard = build_dashboard(
            img_anotada, limiar_bgr, erro,
            int(angulo + 90), eff_vel,
            kp_reta, ki_reta, kd_reta,
            kp_curva, ki_curva, kd_curva,
            sign_boxes, estado, obstacle_boxes, prox,
        )
        cv2.imshow("Visao", dashboard)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()


# ══════════════════════════════════════════════════════════════════════════════
#  ENTRADA PRINCIPAL
# ══════════════════════════════════════════════════════════════════════════════

if MODE == "image":
    run_image_mode()
elif MODE == "video":
    run_video_mode()
else:
    print(f"[Erro] MODE invalido: '{MODE}'. Use 'video' ou 'image'.")
