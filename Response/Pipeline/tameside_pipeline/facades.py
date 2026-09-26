"""Stage D part 2: facade attributes from street-level images, then propagation to unseen facades.

Per observed facade (best Mapillary view from `streetview`):
  1. Project the facade quad (footprint edge x ground..eaves, heights from Stage C) into the image with the
     Mapillary SfM pose (computed_rotation, camera_parameters), and warp it to a front-on crop at PX_PER_M.
  2. Detect windows / doors / shopfronts / garage doors (OWLv2, open-vocabulary) plus occluders (cars, vans, trees).
  3. Parse: storeys from window rows, bays from element columns, door colour, wall colour and material (CLIP on
     wall-only pixels).
Unseen facades inherit from: same building -> terrace row / street neighbours of the same archetype -> area archetype
prior -> archetype default, each with a lower confidence and a recorded `basis`.

Images are cached locally for processing only and never shipped (Mapillary CC BY-SA 4.0; contributors credited).
"""
from __future__ import annotations

import json
import math
import os
import time
import urllib.parse
import urllib.request
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

PX_PER_M = 40
MIN_SRC_PX_PER_M_FOR_BAYS = 15.0  # below this, only colour/material are trusted
MIN_FACADE_PROB = 0.5
MAX_OCCLUSION = 0.4
VALIDITY_PROMPTS = {
    "facade": ["a photo of the front of a house", "a photo of a shop front", "a photo of a building facade with windows"],
    "other": ["a photo of a car", "a photo of a road", "a photo of the sky", "a photo of a tree", "a photo of a garden wall or fence", "a photo of a hedge"],
}
CAMERA_HEIGHT_M = 1.4  # dashcam / handheld
MIN_VISIBLE = 0.6
DETECT_PROMPTS = ["a window", "a door", "a shop front", "a garage door", "a car", "a van", "a tree"]
ELEMENT = {"a window": "window", "a door": "door", "a shop front": "shopfront", "a garage door": "garage"}
OCCLUDER = {"a car", "a van", "a tree"}
SCORE_MIN = {"window": 0.18, "door": 0.2, "shopfront": 0.22, "garage": 0.22, "occluder": 0.2}

MATERIAL_PROMPTS = {
    "brick_red": "a close-up photo of a red brick wall",
    "brick_buff": "a close-up photo of a yellow buff brick wall",
    "brick_painted": "a close-up photo of a painted brick wall",
    "stone": "a close-up photo of a stone block wall",
    "pebbledash": "a close-up photo of a grey pebbledash rendered wall",
    "render": "a close-up photo of a smooth painted rendered wall",
    "cladding": "a close-up photo of a wall with cladding panels",
    "glass": "a close-up photo of a glass shop window",
}
ARCHETYPE_DEFAULT_MATERIAL = {
    "terrace_redbrick": "brick_red", "semi_detached": "brick_red", "detached": "brick_red", "council_1960s": "brick_buff",
    "council_highrise": "cladding", "shop_terrace": "brick_red", "retail_modern": "cladding", "mill_brick": "brick_red",
    "mill_stone": "stone", "church": "stone", "civic": "stone", "industrial_shed": "cladding", "other": "brick_red",
}
ARCHETYPE_DEFAULT_BAY_M = {"terrace_redbrick": 2.5, "semi_detached": 2.8, "detached": 3.0, "shop_terrace": 3.0, "council_1960s": 3.0}


# ---------------------------------------------------------------- image fetch

def fetch_image_details(ids: list[str], cache: Path, tok: str) -> dict[str, dict]:
    """Pose, intrinsics and 2048 px thumbnail for each image (cached)."""
    cache.mkdir(parents=True, exist_ok=True)
    meta_file = cache / "details.json"
    details = json.loads(meta_file.read_text()) if meta_file.exists() else {}
    todo = [i for i in ids if i not in details]
    fields = "id,thumb_2048_url,camera_parameters,camera_type,computed_rotation,computed_compass_angle,computed_geometry,computed_altitude,width,height"
    for k in range(0, len(todo), 50):
        batch = todo[k:k + 50]
        q = urllib.parse.urlencode({"ids": ",".join(batch), "fields": fields, "access_token": tok})
        for attempt in range(4):
            try:
                data = json.load(urllib.request.urlopen(f"https://graph.mapillary.com/?{q}", timeout=120))
                break
            except Exception:
                time.sleep(2 ** attempt)
        else:
            data = {}
        for i, d in data.items():
            details[i] = {kk: d.get(kk) for kk in fields.split(",")}
        meta_file.write_text(json.dumps(details))
    return details


def download_thumb(detail: dict, cache: Path) -> Path | None:
    path = cache / "img" / f"{detail['id']}.jpg"
    if path.exists():
        return path
    url = detail.get("thumb_2048_url")
    if not url:
        return None
    path.parent.mkdir(parents=True, exist_ok=True)
    for attempt in range(3):
        try:
            path.write_bytes(urllib.request.urlopen(url, timeout=120).read())
            return path
        except Exception:
            time.sleep(2 ** attempt)
    return None


# ---------------------------------------------------------------- camera model

def rotation_matrix(detail: dict) -> np.ndarray:
    """World (ENU) -> camera (x right, y down, z forward). OpenSfM angle-axis when present, else yaw-only."""
    r = detail.get("computed_rotation")
    if r and len(r) == 3:
        import cv2

        R, _ = cv2.Rodrigues(np.asarray(r, dtype=np.float64))
        return R
    yaw = math.radians(detail.get("computed_compass_angle") or 0.0)
    fwd = np.array([math.sin(yaw), math.cos(yaw), 0.0])
    right = np.array([math.cos(yaw), -math.sin(yaw), 0.0])
    down = np.array([0.0, 0.0, -1.0])
    return np.stack([right, down, fwd])


def focal_px(detail: dict, w: int, h: int) -> float:
    cp = detail.get("camera_parameters")
    if cp and cp[0] and (detail.get("camera_type") in (None, "perspective", "brown", "fisheye")):
        return float(cp[0]) * max(w, h)
    return (w / 2) / math.tan(math.radians(100.0) / 2)  # 16:9 action-cam default


def project(points_enu: np.ndarray, R: np.ndarray, f: float, w: int, h: int, detail: dict) -> np.ndarray:
    """Project ENU points (relative to the camera centre) to pixels; NaN if behind the camera."""
    pc = points_enu @ R.T
    z = pc[:, 2]
    x, y = pc[:, 0] / z, pc[:, 1] / z
    cp = detail.get("camera_parameters") or []
    if len(cp) >= 3 and detail.get("camera_type") in (None, "perspective", "brown"):
        r2 = x * x + y * y
        d = 1 + cp[1] * r2 + cp[2] * r2 * r2
        x, y = x * d, y * d
    uv = np.stack([f * x + w / 2, f * y + h / 2], axis=1)
    uv[z <= 0.5] = np.nan
    return uv


def delta_rotation(yaw_deg: float, pitch_deg: float) -> np.ndarray:
    """Small correction in the camera frame: yaw about the camera's down axis (y), pitch about its right axis (x)."""
    y, p = math.radians(yaw_deg), math.radians(pitch_deg)
    Ry = np.array([[math.cos(y), 0, math.sin(y)], [0, 1, 0], [-math.sin(y), 0, math.cos(y)]])
    Rx = np.array([[1, 0, 0], [0, math.cos(p), -math.sin(p)], [0, math.sin(p), math.cos(p)]])
    return Rx @ Ry


def rectify(img: np.ndarray, detail: dict, cam_enu_origin: tuple[float, float, float], a, b, z0: float, z1: float,
            correction: tuple[float, float] = (0.0, 0.0), out_px_per_m: int | None = None):
    """Warp the facade quad (a->b left to right, z0..z1 ODN) into a front-on crop. Returns (crop, visible_fraction)."""
    import cv2

    h, w = img.shape[:2]
    ce, cn, cz = cam_enu_origin
    quad = np.array([[a[0], a[1], z1], [b[0], b[1], z1], [b[0], b[1], z0], [a[0], a[1], z0]], dtype=np.float64)
    rel = quad - np.array([ce, cn, cz])
    R = delta_rotation(*correction) @ rotation_matrix(detail)
    uv = project(rel, R, focal_px(detail, w, h), w, h, detail)
    if np.isnan(uv).any():
        return None, 0.0, 0.0
    L = math.hypot(b[0] - a[0], b[1] - a[1])
    H = z1 - z0
    ppm = out_px_per_m or PX_PER_M
    W_px, H_px = max(int(L * ppm), 8), max(int(H * ppm), 8)
    dst = np.array([[0, 0], [W_px, 0], [W_px, H_px], [0, H_px]], dtype=np.float32)
    M = cv2.getPerspectiveTransform(uv.astype(np.float32), dst)
    crop = cv2.warpPerspective(img, M, (W_px, H_px), flags=cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT, borderValue=0)
    mask = cv2.warpPerspective(np.full((h, w), 255, np.uint8), M, (W_px, H_px), flags=cv2.INTER_NEAREST, borderValue=0)
    # source resolution: how many real image pixels per metre of facade (the narrower of top and bottom edges)
    src_px_per_m = min(np.linalg.norm(uv[1] - uv[0]), np.linalg.norm(uv[2] - uv[3])) / max(L, 0.1)
    return crop, float((mask > 0).mean()), float(src_px_per_m)


# ---------------------------------------------------------------- models

class FacadeModels:
    def __init__(self):
        import torch
        from transformers import CLIPModel, CLIPProcessor, Owlv2ForObjectDetection, Owlv2Processor

        torch.set_num_threads(max(os.cpu_count() or 1, 1))
        self.torch = torch
        self.owl_p = Owlv2Processor.from_pretrained("google/owlv2-base-patch16-ensemble")
        self.owl = Owlv2ForObjectDetection.from_pretrained("google/owlv2-base-patch16-ensemble").eval()
        self.clip_p = CLIPProcessor.from_pretrained("openai/clip-vit-base-patch32")
        self.clip = CLIPModel.from_pretrained("openai/clip-vit-base-patch32").eval()
        with torch.no_grad():
            t = self.clip_p(text=list(MATERIAL_PROMPTS.values()), return_tensors="pt", padding=True)
            e = self.clip.get_text_features(**t)
            if not isinstance(e, torch.Tensor):
                e = e.pooler_output if hasattr(e, "pooler_output") else e[0]
            self.mat_text = e / e.norm(dim=-1, keepdim=True)

    def _text(self, prompts):
        with self.torch.no_grad():
            t = self.clip_p(text=prompts, return_tensors="pt", padding=True)
            e = self.clip.get_text_features(**t)
            if not isinstance(e, self.torch.Tensor):
                e = e.pooler_output if hasattr(e, "pooler_output") else e[0]
        return e / e.norm(dim=-1, keepdim=True)

    def facade_probability(self, crops: list[np.ndarray]) -> list[float]:
        """P(crop shows a building front) vs car/road/sky/tree/garden wall/hedge (CLIP zero-shot)."""
        from PIL import Image

        if not hasattr(self, "_valid_text"):
            self._valid_text = self._text(VALIDITY_PROMPTS["facade"] + VALIDITY_PROMPTS["other"])
        with self.torch.no_grad():
            px = self.clip_p(images=[Image.fromarray(c) for c in crops], return_tensors="pt")
            e = self.clip.get_image_features(**px)
            if not isinstance(e, self.torch.Tensor):
                e = e.pooler_output if hasattr(e, "pooler_output") else e[0]
            e = e / e.norm(dim=-1, keepdim=True)
            probs = (100.0 * e @ self._valid_text.T).softmax(dim=-1)
        k = len(VALIDITY_PROMPTS["facade"])
        return probs[:, :k].sum(dim=1).tolist()

    def refine_pose(self, img, detail, cam, a, b, z0, z1, yaws=(-4, -2, 0, 2, 4), pitches=(-2, 0, 2)):
        """Pick the small yaw/pitch correction whose crop CLIP rates most facade-like (Mapillary poses drift a few deg)."""
        trials = []
        for yw in yaws:
            for pt in pitches:
                crop, vis, _ = rectify(img, detail, cam, a, b, z0, z1, (yw, pt), out_px_per_m=12)
                if crop is not None and vis >= MIN_VISIBLE:
                    trials.append(((yw, pt), crop))
        if not trials:
            return (0.0, 0.0), 0.0
        probs = self.facade_probability([t[1] for t in trials])
        # prefer the uncorrected pose unless a correction is clearly better
        best = max(range(len(trials)), key=lambda i: probs[i] - 0.015 * (abs(trials[i][0][0]) + 2 * abs(trials[i][0][1])))
        return trials[best][0], probs[best]

    def detect(self, crop: np.ndarray) -> list[dict]:
        from PIL import Image

        im = Image.fromarray(crop)
        with self.torch.no_grad():
            inputs = self.owl_p(text=[DETECT_PROMPTS], images=im, return_tensors="pt")
            out = self.owl(**inputs)
        # OWLv2 pads to a square; boxes are relative to the padded size
        side = max(crop.shape[0], crop.shape[1])
        pp = getattr(self.owl_p, "post_process_grounded_object_detection", None) or self.owl_p.post_process_object_detection
        res = pp(out, threshold=0.12, target_sizes=self.torch.tensor([[side, side]]))[0]
        dets = []
        for s, l, bx in zip(res["scores"].tolist(), res["labels"].tolist(), res["boxes"].tolist()):
            prompt = DETECT_PROMPTS[l]
            kind = "occluder" if prompt in OCCLUDER else ELEMENT[prompt]
            if s >= SCORE_MIN[kind]:
                x0, y0, x1, y1 = [max(0.0, v) for v in bx]
                x1, y1 = min(x1, crop.shape[1]), min(y1, crop.shape[0])
                if x1 - x0 > 4 and y1 - y0 > 4:
                    dets.append({"kind": kind, "score": round(s, 3), "box": [x0, y0, x1, y1]})
        return nms(dets)

    def material(self, patches: list[np.ndarray], wall_colour: str | None = None) -> tuple[str, float]:
        from PIL import Image

        if not patches:
            if wall_colour:
                prior = colour_prior(wall_colour)
                k = int(np.argmax(prior))
                return list(MATERIAL_PROMPTS)[k], float(prior[k]) * 0.5
            return "unknown", 0.0
        with self.torch.no_grad():
            px = self.clip_p(images=[Image.fromarray(p) for p in patches], return_tensors="pt")
            e = self.clip.get_image_features(**px)
            if not isinstance(e, self.torch.Tensor):
                e = e.pooler_output if hasattr(e, "pooler_output") else e[0]
            e = e / e.norm(dim=-1, keepdim=True)
            probs = (100.0 * e @ self.mat_text.T).softmax(dim=-1).mean(dim=0).numpy()
        if wall_colour:
            probs = probs * colour_prior(wall_colour)
            probs = probs / probs.sum()
        k = int(probs.argmax())
        return list(MATERIAL_PROMPTS)[k], float(probs[k])


def iou(a, b) -> float:
    x0, y0, x1, y1 = max(a[0], b[0]), max(a[1], b[1]), min(a[2], b[2]), min(a[3], b[3])
    inter = max(0, x1 - x0) * max(0, y1 - y0)
    ua = (a[2] - a[0]) * (a[3] - a[1]) + (b[2] - b[0]) * (b[3] - b[1]) - inter
    return inter / ua if ua > 0 else 0.0


def nms(dets: list[dict], thr: float = 0.4) -> list[dict]:
    """Class-agnostic NMS among facade elements (a box can't be both a window and a door); occluders separate.

    An element box that largely overlaps a car/van/tree box is dropped (a car windscreen is not a window)."""
    out = []
    for d in sorted(dets, key=lambda d: -d["score"]):
        same_group = [o for o in out if (o["kind"] == "occluder") == (d["kind"] == "occluder")]
        if all(iou(d["box"], o["box"]) < thr for o in same_group):
            out.append(d)
    occ = [o for o in out if o["kind"] == "occluder"]
    return [d for d in out if d["kind"] == "occluder" or all(_inside_frac(d["box"], o["box"]) < 0.5 for o in occ)]


def _inside_frac(a, b) -> float:
    """Fraction of box a that lies inside box b."""
    x0, y0, x1, y1 = max(a[0], b[0]), max(a[1], b[1]), min(a[2], b[2]), min(a[3], b[3])
    inter = max(0, x1 - x0) * max(0, y1 - y0)
    area = (a[2] - a[0]) * (a[3] - a[1])
    return inter / area if area > 0 else 0.0


# ---------------------------------------------------------------- parsing (pure, testable)

def cluster_1d(values: list[float], tol: float) -> list[list[int]]:
    order = sorted(range(len(values)), key=lambda i: values[i])
    groups: list[list[int]] = []
    for i in order:
        if groups and values[i] - values[groups[-1][-1]] <= tol:
            groups[-1].append(i)
        else:
            groups.append([i])
    return groups


def parse_facade(dets: list[dict], width_m: float, height_m: float, massing_storeys: int) -> dict:
    """Turn metric detections into storeys and a left-to-right bay layout."""
    els = []
    for d in dets:
        if d["kind"] == "occluder":
            continue
        x0, y0, x1, y1 = [v / PX_PER_M for v in d["box"]]
        cx, bottom = (x0 + x1) / 2, height_m - y1  # height of element bottom above ground
        w, h = x1 - x0, y1 - y0
        if w > 0.9 * width_m and d["kind"] == "window":
            continue  # a 'window' spanning the whole facade is a misfire
        els.append({"kind": d["kind"], "cx": cx, "bottom": bottom, "top": height_m - y0, "w": w, "h": h, "score": d["score"]})
    # storeys from window rows (bottom sill heights), fall back to massing
    wins = [e for e in els if e["kind"] == "window"]
    rows = cluster_1d([e["bottom"] for e in wins], tol=1.0) if wins else []
    rows = [r for r in rows if len(r) >= 1]
    storeys_seen = len(rows)
    storeys = max(storeys_seen, 1) if storeys_seen else massing_storeys
    row_bottoms = [float(np.median([wins[i]["bottom"] for i in r])) for r in rows]
    # columns (bays) from all element centres
    cols = cluster_1d([e["cx"] for e in els], tol=0.7) if els else []
    bays = []
    for c in cols:
        members = [els[i] for i in c]
        cx = float(np.mean([m["cx"] for m in members]))
        ground_kinds = [m for m in members if m["bottom"] < 1.2]
        ground = "blank"
        if any(m["kind"] == "shopfront" for m in ground_kinds):
            ground = "shopfront"
        elif any(m["kind"] == "garage" for m in ground_kinds):
            ground = "garage"
        elif any(m["kind"] == "door" for m in ground_kinds):
            ground = "door"
        elif any(m["kind"] == "window" and m["w"] > 1.8 for m in ground_kinds):
            ground = "bay_window"
        elif ground_kinds:
            ground = "window"
        upper = []
        for k in range(1, max(storeys, 1)):
            has = any(m["kind"] == "window" and _storey_index(m["bottom"], row_bottoms) == k for m in members)
            upper.append("window" if has else "blank")
        width = float(max(m["w"] for m in members))
        bays.append({"cx": cx, "ground": ground, "upper": upper, "el_w": width})
    bays.sort(key=lambda b: b["cx"])
    # bay widths: split the facade halfway between neighbouring bay centres
    out_bays = []
    for i, bb in enumerate(bays):
        left = 0.0 if i == 0 else (bays[i - 1]["cx"] + bb["cx"]) / 2
        right = width_m if i == len(bays) - 1 else (bb["cx"] + bays[i + 1]["cx"]) / 2
        out_bays.append({"width_m": round(right - left, 2), "ground": bb["ground"], "upper": bb["upper"]})
    counts = Counter(e["kind"] for e in els)
    return {"storeys_seen": storeys_seen, "storeys": storeys, "bays": out_bays, "counts": dict(counts)}


def _storey_index(bottom: float, row_bottoms: list[float]) -> int:
    """0 = ground floor. Rows whose sill is above ~1.8 m count as upper storeys, in order."""
    uppers = sorted(b for b in row_bottoms if b >= 1.8)
    if bottom < 1.8:
        return 0
    return 1 + int(np.argmin([abs(bottom - u) for u in uppers])) if uppers else 1


def wall_patches(crop: np.ndarray, dets: list[dict], n: int = 4, size: int = 64) -> tuple[list[np.ndarray], str | None]:
    """Wall-only patches (not on windows/doors/occluders, not black border) and the median wall colour."""
    h, w = crop.shape[:2]
    mask = np.ones((h, w), bool)
    for d in dets:
        x0, y0, x1, y1 = [int(v) for v in d["box"]]
        mask[max(y0 - 4, 0):y1 + 4, max(x0 - 4, 0):x1 + 4] = False
    mask &= crop.sum(axis=2) > 30  # outside the photo after warping
    mask[: int(h * 0.12)] = False  # eaves / sky / misalignment at the top
    mask[int(h * 0.75):] = False  # pavement, bins, garden walls
    mask[:, : int(w * 0.1)] = False  # neighbours creep in at the sides when the pose is a little off
    mask[:, int(w * 0.9):] = False
    colour = None
    if mask.sum() > 200:
        med = np.median(crop[mask], axis=0).astype(int)
        colour = "#{:02x}{:02x}{:02x}".format(*med)
    patches = []
    if h >= size and w >= size:
        ys, xs = np.nonzero(mask[: h - size, : w - size])
        if len(ys):
            rng = np.random.default_rng(0)
            for k in rng.choice(len(ys), size=min(n * 8, len(ys)), replace=False):
                y, x = ys[k], xs[k]
                if mask[y:y + size, x:x + size].mean() > 0.9:
                    patches.append(crop[y:y + size, x:x + size])
                if len(patches) >= n:
                    break
    return patches, colour


def colour_prior(hex_colour: str) -> np.ndarray:
    """Plausibility of each material (MATERIAL_PROMPTS order) given the median wall colour.

    CLIP alone confuses brick/render/cladding on small dashcam crops; wall colour is a strong, cheap cue in Tameside.
    """
    import colorsys

    r, g, b = (int(hex_colour[i:i + 2], 16) / 255 for i in (1, 3, 5))
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    hue = h * 360
    reddish = (hue < 30 or hue > 340) and s > 0.25
    buff = 25 <= hue <= 55 and s > 0.2 and v > 0.45
    pale = s < 0.18 and v > 0.62
    grey = s < 0.15 and 0.3 <= v <= 0.62
    dark = v < 0.3
    pri = {
        "brick_red": 3.0 if reddish else (1.0 if dark else 0.3),
        "brick_buff": 3.0 if buff else 0.4,
        "brick_painted": 2.0 if pale else 0.5,
        "stone": 1.5 if (dark or buff or grey) else 0.5,
        "pebbledash": 2.5 if grey else (1.0 if pale else 0.3),
        "render": 2.5 if pale else (1.0 if grey else 0.3),
        "cladding": 0.4,
        "glass": 0.3,
    }
    return np.array([pri[k] for k in MATERIAL_PROMPTS])


def occlusion(dets: list[dict], shape) -> float:
    """Fraction of the crop covered by cars/vans/trees."""
    m = np.zeros(shape[:2], bool)
    for d in dets:
        if d["kind"] == "occluder":
            x0, y0, x1, y1 = [int(v) for v in d["box"]]
            m[y0:y1, x0:x1] = True
    return float(m.mean())


def door_colour(crop: np.ndarray, dets: list[dict]) -> str | None:
    doors = [d for d in dets if d["kind"] == "door"]
    if not doors:
        return None
    d = max(doors, key=lambda d: d["score"])
    x0, y0, x1, y1 = [int(v) for v in d["box"]]
    # middle of the door leaf avoids frame and glazing
    px = crop[y0 + (y1 - y0) // 4: y1 - (y1 - y0) // 4, x0 + (x1 - x0) // 4: x1 - (x1 - x0) // 4]
    if px.size == 0:
        return None
    med = np.median(px.reshape(-1, 3), axis=0).astype(int)
    return "#{:02x}{:02x}{:02x}".format(*med)


# ---------------------------------------------------------------- rhythm fitting (pure, testable)

HOUSE_TYPES = ("terrace_redbrick", "semi_detached", "detached")
DEFAULT_UNIT_W = {"terrace_redbrick": 4.8, "semi_detached": 6.5, "detached": 8.0, "shop_terrace": 5.5}
DOOR_BAY_M = 1.5


def fit_pattern(dets: list[dict], width_m: float, height_m: float, archetype: str, units_on_edge: int | None) -> dict:
    """Summarise detections into a repeating pattern. Robust to missed detections: detections vote on style,
    the layout comes from the known number of units (houses) along the facade."""
    els = []
    for d in dets:
        if d["kind"] == "occluder":
            continue
        x0, y0, x1, y1 = [v / PX_PER_M for v in d["box"]]
        els.append({"kind": d["kind"], "cx": (x0 + x1) / 2, "bottom": height_m - y1, "w": x1 - x0})
    ground = [e for e in els if e["bottom"] < 1.4]
    upper = [e for e in els if e["bottom"] >= 1.4 and e["kind"] == "window"]
    shop_w = sum(e["w"] for e in ground if e["kind"] == "shopfront")
    if archetype in HOUSE_TYPES or archetype == "shop_terrace":
        n = units_on_edge or max(1, round(width_m / DEFAULT_UNIT_W.get(archetype, 5.0)))
        uw = width_m / n
        rel = [(e["cx"] % uw) / uw for e in ground if e["kind"] == "door"]
        door_side = "left" if (np.median(rel) if rel else 0.3) < 0.5 else "right"
        wide = [e for e in ground if e["kind"] == "window" and e["w"] > 1.8]
        narrow = [e for e in ground if e["kind"] == "window" and e["w"] <= 1.8]
        ground_window = "bay_window" if len(wide) > len(narrow) else "window"
        upper_per_unit = 2 if (len(upper) / n >= 1.3 or uw >= 5.5) else 1
        shopfront = shop_w > 0.3 * width_m or archetype == "shop_terrace"
        garage = any(e["kind"] == "garage" for e in ground)
        return {"kind": "units", "units": n, "unit_width_m": round(uw, 2), "door_side": door_side, "ground_window": ground_window,
                "upper_per_unit": upper_per_unit, "shopfront": bool(shopfront), "garage": bool(garage),
                "evidence": {"doors": len(rel), "ground_windows": len(wide) + len(narrow), "upper_windows": len(upper)}}
    xs = sorted(e["cx"] for e in els if e["kind"] == "window")
    gaps = [b - a for a, b in zip(xs, xs[1:]) if 1.5 <= b - a <= 6.0]
    spacing = float(np.clip(np.median(gaps), 2.2, 4.5)) if gaps else 3.2
    return {"kind": "grid", "spacing_m": round(spacing, 2), "shopfront": bool(shop_w > 0.3 * width_m),
            "evidence": {"windows": len(xs), "shopfront_m": round(shop_w, 1)}}


def bays_from_pattern(pat: dict, width_m: float, storeys: int, archetype: str) -> list[dict]:
    """Regular bay layout for a facade of this width from a (possibly borrowed) pattern."""
    uppers = max(storeys - 1, 0)
    if pat.get("kind") == "units":
        n = max(1, round(width_m / pat["unit_width_m"])) if pat.get("unit_width_m") else pat["units"]
        uw = width_m / n
        out = []
        for i in range(n):
            door = {"width_m": DOOR_BAY_M, "ground": "door", "upper": ["window" if pat["upper_per_unit"] >= 2 else "blank"] * uppers}
            main_ground = "shopfront" if pat.get("shopfront") else ("garage" if pat.get("garage") and i % 2 else pat["ground_window"])
            main = {"width_m": max(uw - DOOR_BAY_M, 0.8), "ground": main_ground, "upper": ["window"] * uppers}
            # semi pairs are mirrored: doors meet at (or sit away from) the party wall
            left_door = (pat["door_side"] == "left") != (archetype == "semi_detached" and i % 2 == 1)
            out += [door, main] if left_door else [main, door]
        return [{**b, "width_m": round(b["width_m"], 2)} for b in out]
    n = max(1, round(width_m / pat.get("spacing_m", 3.2)))
    g = "shopfront" if pat.get("shopfront") else "window"
    bays = [{"width_m": round(width_m / n, 2), "ground": g, "upper": ["window"] * uppers} for _ in range(n)]
    if not pat.get("shopfront") and n >= 3:
        bays[n // 2]["ground"] = "door"  # main entrance
    return bays


def plausible_wall_colour(hex_colour: str | None) -> str | None:
    """Reject colours that are really sky (blue, bright) or deep shadow/black border."""
    if not hex_colour:
        return None
    import colorsys

    r, g, b = (int(hex_colour[i:i + 2], 16) / 255 for i in (1, 3, 5))
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    if 180 <= h * 360 <= 250 and s > 0.12 and v > 0.45:
        return None
    if v < 0.12:
        return None
    return hex_colour


def _compatible(pat: dict, arch: str) -> bool:
    return (pat.get("kind") == "units") == (arch in HOUSE_TYPES or arch == "shop_terrace")


def default_pattern(arch: str, width_m: float) -> dict:
    if arch in HOUSE_TYPES or arch == "shop_terrace":
        uw = DEFAULT_UNIT_W.get(arch, 5.0)
        return {"kind": "units", "units": max(1, round(width_m / uw)), "unit_width_m": uw, "door_side": "left",
                "ground_window": "bay_window" if arch == "semi_detached" else "window", "upper_per_unit": 2 if uw >= 5.5 else 1,
                "shopfront": arch == "shop_terrace", "garage": False}
    return {"kind": "grid", "spacing_m": 3.2, "shopfront": arch == "retail_modern"}


# ---------------------------------------------------------------- propagation (pure, testable)

def synth_bays(width_m: float, template: list[dict] | None, archetype: str, storeys: int) -> list[dict]:
    """Fill a facade of this width with the template's bay rhythm (or the archetype's default)."""
    if template:
        tw = sum(b["width_m"] for b in template)
        if tw > 0.5:
            reps = max(1, round(width_m / tw))
            scale = width_m / (tw * reps)
            return [{"width_m": round(b["width_m"] * scale, 2), "ground": b["ground"], "upper": list(b["upper"])} for _ in range(reps) for b in template]
    bay = ARCHETYPE_DEFAULT_BAY_M.get(archetype, 3.0)
    n = max(1, round(width_m / bay))
    grounds = ["door" if (archetype in ("terrace_redbrick", "semi_detached", "detached") and i % 2 == 0) else
               ("shopfront" if archetype == "shop_terrace" else "window") for i in range(n)]
    return [{"width_m": round(width_m / n, 2), "ground": g, "upper": ["window"] * max(storeys - 1, 0)} for g in grounds]


def propagate(records: dict[str, dict], observed: dict[tuple[str, int], dict], street_edges: list[dict],
              neighbours: dict[str, list[str]], street_of: dict[str, str]) -> dict[tuple[str, int], dict]:
    """Assign every street-facing edge a facade: observed, or inferred with a basis and confidence."""
    by_bldg = defaultdict(list)
    for (fid, _), f in observed.items():
        by_bldg[fid].append(f)
    by_street_arch = defaultdict(list)
    by_arch = defaultdict(list)
    for (fid, _), f in observed.items():
        arch = records[fid]["archetype"]["id"]
        by_street_arch[(street_of.get(fid), arch)].append(f)
        by_arch[arch].append(f)

    def summarise(fs: list[dict]):
        mats = Counter(f["wall_material"] for f in fs if f.get("wall_material") not in (None, "unknown"))
        cols = [f["wall_colour_srgb"] for f in fs if f.get("wall_colour_srgb")]
        doors = [f["door_colour_srgb"] for f in fs if f.get("door_colour_srgb")]
        tmpl = max((f for f in fs if f.get("pattern") and f.get("bays_source", "observed") == "observed"),
                   key=lambda f: f.get("confidence", 0), default=None)
        return (mats.most_common(1)[0][0] if mats else None, _median_colour(cols), doors, tmpl)

    out = {}
    for e in street_edges:
        key = (e["footprint_id"], e["edge_index"])
        if key in observed:
            out[key] = observed[key]
            continue
        fid = e["footprint_id"]
        rec = records[fid]
        arch = rec["archetype"]["id"]
        storeys = rec["massing"].get("storeys", 2)
        tiers = [
            ("same_building", by_bldg.get(fid, []), 0.6),
            ("neighbours", [f for n in neighbours.get(fid, []) for f in by_bldg.get(n, []) if records[n]["archetype"]["id"] == arch], 0.5),
            ("same_street_and_type", by_street_arch.get((street_of.get(fid), arch), []), 0.4),
            ("area_type_prior", by_arch.get(arch, []), 0.25),
        ]
        for basis, fs, conf in tiers:
            if fs:
                mat, col, doors, tmpl = summarise(fs)
                break
        else:
            basis, conf, mat, col, doors, tmpl = "archetype_default", 0.1, None, None, [], None
        rng = np.random.default_rng(abs(hash(key)) % (2 ** 32))
        out[key] = {
            "edge_index": e["edge_index"], "street_facing": True,
            "wall_material": mat or ARCHETYPE_DEFAULT_MATERIAL.get(arch, "brick_red"),
            **({"wall_colour_srgb": col} if col else {}),
            **({"door_colour_srgb": doors[int(rng.integers(len(doors)))]} if doors else {}),  # vary doors along a street
            "bays": bays_from_pattern(tmpl["pattern"], e["length"], storeys, arch) if (tmpl and _compatible(tmpl["pattern"], arch))
                    else bays_from_pattern(default_pattern(arch, e["length"]), e["length"], storeys, arch),
            "observations": 0, "confidence": conf, "basis": f"inferred:{basis}",
        }
    return out


def _median_colour(cols: list[str]) -> str | None:
    if not cols:
        return None
    arr = np.array([[int(c[i:i + 2], 16) for i in (1, 3, 5)] for c in cols])
    return "#{:02x}{:02x}{:02x}".format(*np.median(arr, axis=0).astype(int))
