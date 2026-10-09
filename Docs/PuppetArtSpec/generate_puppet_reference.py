# -*- coding: utf-8 -*-
"""《影韵》皮影角色美术参考图生成器。

数据来源为 Assets/Scripts/YingYun/Unity/View/ShadowPuppetPresenter.cs 的
BuildPuppetStage()／CreateLimb()／BuildHand()／BuildSleeveTail()／BuildFoot()，
以及 DanceChoreography.cs 的 DanceJoint 定义。坐标与尺寸单位是 Unity 世界单位。

产出：
  01-角色整体与比例参考.png
  02-分片清单.png
  03-关节层级与竹杆传动.png
  Puppet-角色装配.svg
"""

import math
import os

from PIL import Image, ImageDraw, ImageFont


HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
CJK_FONT = os.path.join(
    PROJECT_ROOT, "Assets", "Resources", "YingYun", "Fonts", "NotoSansSC-VF.ttf"
)

# 作者单位：每 1 个 Unity 世界单位 = 多少个像素（建议值，1x 输出）
AUTHOR_PPU = 400.0
# 参考照相机：场景 YingYun_Gameplay.unity 为正交、orthographic size = 5，屏幕高 10 世界单位
SCREEN_HEIGHT_UNITS = 10.0
CAMERA_PPU_1080 = 1080.0 / SCREEN_HEIGHT_UNITS

SHADOW = (0.12, 0.025, 0.018, 0.96)
ACCENT = (0.70, 0.12, 0.045, 0.98)
CUTWORK = (0.92, 0.48, 0.08, 0.98)

INK = (0.10, 0.07, 0.05, 1.0)
MUTED = (0.45, 0.36, 0.30, 1.0)
GUIDE = (0.72, 0.66, 0.58, 1.0)
PAPER = (0.965, 0.945, 0.90, 1.0)
CARD = (1.0, 0.99, 0.965, 1.0)
RULE = (0.80, 0.74, 0.66, 1.0)
PIVOT = (0.05, 0.45, 0.85, 1.0)


def rgba(color, alpha=None):
    """把 0–1 的浮点颜色转成 0–255 整数元组；对已转换过的值保持幂等。"""
    comps = color[:3]
    if max(comps) > 1.0:
        r, g, b = comps
    else:
        r, g, b = (c * 255.0 for c in comps)
    a = color[3] if alpha is None else alpha
    alpha_byte = a if a > 1.0 else a * 255.0
    return (int(round(r)), int(round(g)), int(round(b)), int(round(alpha_byte)))


def part(code, cn, parent, offset, size, shape, color, order, rotation=0.0):
    return {
        "code": code,
        "cn": cn,
        "parent": parent,
        "offset": offset,
        "size": size,
        "shape": shape,
        "color": color,
        "order": order,
        "rotation": rotation,
    }


# 关节世界坐标（_visualRoot 空间，休止姿；腿採落地目标 ±0.34 / y=-2.08）
JOINTS = [
    ("Joint Pelvis", "骨盆（根）", (0.0, -0.55), None),
    ("Joint Waist", "腰", (0.0, -0.40), "Joint Pelvis"),
    ("Joint Neck", "颈", (0.0, 0.93), "Joint Waist"),
    ("Chest Rod Socket", "胸前杆座", (0.0, 0.45), "Joint Waist"),
    ("Joint Left Shoulder", "左肩", (-0.48, 0.57), "Joint Waist"),
    ("Joint Left Elbow", "左肘", (-0.48, -0.25), "Joint Left Shoulder"),
    ("Joint Left Wrist", "左腕", (-0.48, -0.97), "Joint Left Elbow"),
    ("Joint Left Finger Fan", "左手掌扇", (-0.60, -1.19), "Joint Left Wrist"),
    ("Joint Left Sleeve Cuff", "左袖口", (-0.60, -0.55), "Joint Left Elbow"),
    ("Joint Left Sleeve Tail", "左袖尾", (-0.60, -0.84), "Joint Left Sleeve Cuff"),
    ("Joint Right Shoulder", "右肩", (0.48, 0.57), "Joint Waist"),
    ("Joint Right Elbow", "右肘", (0.48, -0.25), "Joint Right Shoulder"),
    ("Joint Right Wrist", "右腕", (0.48, -0.97), "Joint Right Elbow"),
    ("Joint Right Finger Fan", "右手掌扇", (0.60, -1.19), "Joint Right Wrist"),
    ("Joint Right Sleeve Cuff", "右袖口", (0.60, -0.55), "Joint Right Elbow"),
    ("Joint Right Sleeve Tail", "右袖尾", (0.60, -0.84), "Joint Right Sleeve Cuff"),
    ("Joint Left Hip", "左髋", (-0.27, -0.47), "Joint Pelvis"),
    ("Joint Left Knee", "左膝", (-0.34, -1.35), "Joint Left Hip"),
    ("Joint Left Ankle", "左踝", (-0.34, -2.08), "Joint Left Knee"),
    ("Joint Right Hip", "右髋", (0.27, -0.47), "Joint Pelvis"),
    ("Joint Right Knee", "右膝", (0.34, -1.35), "Joint Right Hip"),
    ("Joint Right Ankle", "右踝", (0.34, -2.08), "Joint Right Knee"),
]

# 角色分片（左侧列出，右侧为 X 镜像同一张图）
PARTS = [
    part("Torso", "躯干", "Joint Waist", (0.0, 0.53), (0.92, 1.48), "ellipse", SHADOW, 3),
    part("Waist Ornament", "腰饰", "Joint Waist", (0.0, -0.03), (1.02, 0.20), "rect", ACCENT, 4),
    part("Robe Skirt", "下裳", "Joint Pelvis", (0.0, -0.28), (1.28, 0.72), "ellipse", SHADOW, 3),
    # 下裳前襟是下裳的子物件，会被下裳的 (1.28, 0.72) 缩放，故此处已换算成最终显示尺寸
    part("Robe Front Trim", "下裳前襟", "Joint Pelvis", (0.36, -0.31), (0.15, 0.37), "rect", ACCENT, 4),
    part("Robe Hem", "下裳摆边", "Joint Pelvis", (0.0, -0.58), (1.36, 0.14), "rect", ACCENT, 4),
    part("Chest Cutout", "胸口镂空", "Joint Waist", (0.0, 0.63), (0.48, 0.18), "ellipse", (0.78, 0.28, 0.055, 0.72), 4),
    part("Head", "头", "Joint Neck", (0.0, 0.28), (0.62, 0.72), "ellipse", SHADOW, 5),
    part("Profile Nose", "侧脸鼻", "Joint Neck", (0.33, 0.30), (0.22, 0.16), "ellipse", SHADOW, 6),
    part("Profile Eye", "侧脸眼", "Joint Neck", (0.19, 0.38), (0.10, 0.06), "ellipse", ACCENT, 7),
    part("Head Crown", "头冠", "Joint Neck", (0.0, 0.70), (0.78, 0.18), "rect", ACCENT, 6),
    part("Crown Wing Left", "冠翅（左）", "Joint Neck", (-0.43, 0.78), (0.68, 0.10), "rect", ACCENT, 6, 12.0),
    part("Crown Wing Right", "冠翅（右）", "Joint Neck", (0.43, 0.78), (0.68, 0.10), "rect", ACCENT, 6, -12.0),
    part("Crown Jewel", "冠珠", "Joint Neck", (0.0, 0.86), (0.18, 0.18), "ellipse", CUTWORK, 7),
    part("Left Upper Arm", "上臂", "Joint Left Shoulder", (0.0, -0.41), (0.20, 0.82), "ellipse", SHADOW, 5),
    part("Left Flowing Sleeve", "肩袖（大片）", "Joint Left Shoulder", (-0.12, -0.48), (0.48, 0.82), "ellipse", SHADOW, 4),
    part("Left Forearm", "前臂", "Joint Left Elbow", (0.0, -0.36), (0.17, 0.72), "ellipse", SHADOW, 6),
    part("Sleeve Cuff Plate", "袖口片", "Joint Left Sleeve Cuff", (0.0, -0.16), (0.52, 0.38), "ellipse", SHADOW, 7),
    part("Trailing Water Sleeve", "水袖拖尾", "Joint Left Sleeve Tail", (-0.04, -0.23), (0.31, 0.70), "ellipse", (0.32, 0.045, 0.025, 0.82), 8),
    part("Sleeve Tail Cutwork", "水袖镂花", "Joint Left Sleeve Tail", (-0.04, -0.47), (0.19, 0.09), "ellipse", ACCENT, 9),
    part("Left Palm Plate", "手掌片", "Joint Left Wrist", (-0.08, -0.12), (0.23, 0.27), "ellipse", SHADOW, 8),
    part("Finger Silhouette", "并指手型", "Joint Left Finger Fan", (-0.08, -0.12), (0.12, 0.30), "ellipse", SHADOW, 8),
    part("Single Finger Silhouette", "单指（剑指）手型", "Joint Left Wrist", (-0.22, -0.30), (0.075, 0.52), "ellipse", SHADOW, 9),
    part("Left Thigh", "大腿", "Joint Left Hip", (0.0, -0.44), (0.24, 0.88), "ellipse", SHADOW, 3),
    part("Left Shin", "小腿", "Joint Left Knee", (0.0, -0.41), (0.19, 0.82), "ellipse", SHADOW, 4),
    part("Left Foot Plate", "脚板", "Joint Left Ankle", (-0.19, -0.04), (0.48, 0.15), "ellipse", SHADOW, 6),
    part("Limb End Cap", "关节端帽（自动生成，共 10 处）", "各肢段末端", (0.0, 0.0), (0.29, 0.29), "ellipse", ACCENT, 7),
    part("Joint Pin", "关节铆钉（自动生成，共 15 处）", "各关节", (0.0, 0.0), (0.14, 0.14), "ellipse", ACCENT, 7),
]

ROD_TARGETS = {
    "Bamboo Control Rod 0": "Joint Left Wrist",
    "Bamboo Control Rod 1": "Head",
    "Bamboo Control Rod 2": "Joint Right Wrist",
    "Bamboo Control Rod 3": "Joint Left Ankle",
    "Bamboo Control Rod 4": "Chest Rod Socket",
    "Bamboo Control Rod 5": "Joint Right Ankle",
}

ROD_GRIP_POINTS = [
    (-3.45, 1.35),
    (-2.75, 2.75),
    (3.45, 1.35),
    (-3.10, -2.65),
    (0.55, -3.05),
    (3.10, -2.65),
]


def world_of(part_entry):
    """返回分片中心的世界坐标。"""
    parent_pos = {name: pos for name, _, pos, _ in JOINTS}
    base = parent_pos.get(part_entry["parent"], (0.0, -0.55))
    return (base[0] + part_entry["offset"][0], base[1] + part_entry["offset"][1])


def load_font(size):
    for candidate in (CJK_FONT, r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf"):
        if os.path.exists(candidate):
            try:
                return ImageFont.truetype(candidate, size)
            except Exception:
                continue
    return ImageFont.load_default()


class Sheet:
    """世界坐标 → 画布像素坐标的转换器（y 轴翻转）。"""

    def __init__(self, image, origin, ppu):
        self.image = image
        self.draw = ImageDraw.Draw(image, "RGBA")
        self.origin = origin
        self.ppu = ppu

    def px(self, point):
        return (
            self.origin[0] + point[0] * self.ppu,
            self.origin[1] - point[1] * self.ppu,
        )

    def shape(self, center, size, shape, fill, rotation=0.0, outline=None, width=2):
        fill = None if fill is None else rgba(fill)
        outline = None if outline is None else rgba(outline)
        cx, cy = self.px(center)
        w = size[0] * self.ppu
        h = size[1] * self.ppu
        box = [cx - w / 2.0, cy - h / 2.0, cx + w / 2.0, cy + h / 2.0]
        if shape == "ellipse":
            self.draw.ellipse(box, fill=fill, outline=outline, width=width)
        elif abs(rotation) < 0.001:
            self.draw.rectangle(box, fill=fill, outline=outline, width=width)
        else:
            self.draw.polygon(self.rotated_box(box, rotation), fill=fill, outline=outline, width=width)

    def rotated_box(self, box, rotation):
        cx = (box[0] + box[2]) / 2.0
        cy = (box[1] + box[3]) / 2.0
        pts = [(box[0], box[1]), (box[2], box[1]), (box[2], box[3]), (box[0], box[3])]
        rad = math.radians(rotation)
        cos_a, sin_a = math.cos(rad), math.sin(rad)
        out = []
        for x, y in pts:
            dx, dy = x - cx, y - cy
            out.append((cx + dx * cos_a + dy * sin_a, cy - dx * sin_a + dy * cos_a))
        return out

    def line(self, a, b, fill=INK, width=2):
        self.draw.line([self.px(a), self.px(b)], fill=rgba(fill), width=width)

    def dot(self, center, radius_px, fill, outline=None):
        fill = rgba(fill)
        outline = None if outline is None else rgba(outline)
        cx, cy = self.px(center)
        self.draw.ellipse(
            [cx - radius_px, cy - radius_px, cx + radius_px, cy + radius_px],
            fill=fill,
            outline=outline,
            width=2,
        )

    def text(self, xy, body, font, fill=INK, anchor="la"):
        fill = rgba(fill)
        if "\n" in body:
            # 多行文字在未安装 Raqm 的 Pillow 上不支持 anchor，逐行计算避免例外
            line_height = font.size + 8
            lines = body.split("\n")
            total = line_height * (len(lines) - 1)
            start_y = xy[1] - total / 2.0 if anchor[1] == "m" else xy[1]
            for offset, line in enumerate(lines):
                self.draw.text((xy[0], start_y + offset * line_height), line,
                               font=font, fill=fill, anchor=anchor[0] + "a")
            return
        self.draw.text(xy, body, font=font, fill=fill, anchor=anchor)


def character_bounds():
    xs, ys = [], []
    for entry in PARTS:
        if entry["code"].startswith(("Joint Pin", "Limb End Cap")):
            continue
        cx, cy = world_of(entry)
        half_w = entry["size"][0] / 2.0
        half_h = entry["size"][1] / 2.0
        if abs(entry["rotation"]) > 0.001:
            rad = math.radians(entry["rotation"])
            span_x = half_w * abs(math.cos(rad)) + half_h * abs(math.sin(rad))
            span_y = half_w * abs(math.sin(rad)) + half_h * abs(math.cos(rad))
        else:
            span_x, span_y = half_w, half_h
        xs.extend([cx - span_x, cx + span_x])
        ys.extend([cy - span_y, cy + span_y])
    return (min(xs), min(ys), max(xs), max(ys))


def draw_character(sheet, outline_only=False, labels=False, label_font=None):
    for entry in sorted(PARTS, key=lambda item: item["order"]):
        if entry["code"].startswith("Limb End Cap"):
            continue
        center = world_of(entry)
        if entry["code"] == "Limb End Cap":
            continue
        fill = None if outline_only else rgba(entry["color"])
        outline = rgba(entry["color"]) if outline_only else rgba(INK, 0.55)
        sheet.shape(center, entry["size"], entry["shape"], fill, entry["rotation"], outline=outline, width=2)

    # 肢体端帽与关节铆钉
    for name, _, pos, _ in JOINTS:
        if name in ("Joint Left Elbow", "Joint Left Wrist", "Joint Left Knee", "Joint Left Ankle",
                    "Joint Right Elbow", "Joint Right Wrist", "Joint Right Knee", "Joint Right Ankle"):
            cap = 0.29 if name.endswith(("Elbow", "Knee")) else 0.25
            sheet.shape(pos, (cap, cap), "ellipse",
                        None if outline_only else rgba(ACCENT),
                        outline=rgba(ACCENT) if outline_only else rgba(INK, 0.45), width=2)
        sheet.shape(pos, (0.14, 0.14), "ellipse",
                    None if outline_only else rgba(ACCENT),
                    outline=rgba(ACCENT) if outline_only else rgba(INK, 0.45), width=2)

    if labels and label_font is not None:
        for name, cn, pos, _ in JOINTS:
            sheet.text((sheet.px((pos[0] + 0.12, pos[1] + 0.10))[0],
                        sheet.px((pos[0] + 0.12, pos[1] + 0.10))[1]), cn, label_font, MUTED, "lm")


def character_layers():
    """组装态分片，按层次由低到高排列。"""
    layers = []
    for entry in sorted(PARTS, key=lambda item: item["order"]):
        if entry["code"].startswith(("Limb End Cap", "Joint Pin")):
            continue
        layers.append({
            "kind": entry["shape"],
            "center": world_of(entry),
            "size": entry["size"],
            "color": entry["color"],
            "rotation": entry["rotation"],
        })
    return layers


def ring_layers():
    """仅描边的关节端帽与铆钉，避免盖住剪影。"""
    layers = []
    for name, _, pos, _ in JOINTS:
        if name in ("Joint Left Elbow", "Joint Left Wrist", "Joint Left Knee", "Joint Left Ankle",
                    "Joint Right Elbow", "Joint Right Wrist", "Joint Right Knee", "Joint Right Ankle"):
            cap = 0.29 if name.endswith(("Elbow", "Knee")) else 0.25
            layers.append({"kind": "ring", "center": pos, "size": (cap, cap),
                           "color": ACCENT, "rotation": 0.0, "stroke": 3})
        layers.append({"kind": "ring", "center": pos, "size": (0.14, 0.14),
                       "color": ACCENT, "rotation": 0.0, "stroke": 3})
    return layers


def render_layers(layers, bounds, ppu, pad):
    """把世界坐标图层画进独立子图，避免与版面互相干扰。"""
    min_x, min_y, max_x, max_y = bounds
    width = int(round((max_x - min_x + pad * 2) * ppu))
    height = int(round((max_y - min_y + pad * 2) * ppu))
    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    sheet = Sheet(image, ((pad - min_x) * ppu, (max_y + pad) * ppu), ppu)
    for layer in layers:
        if layer["kind"] == "ring":
            sheet.shape(layer["center"], layer["size"], "ellipse", None,
                        layer["rotation"], outline=rgba(layer["color"], 0.75), width=layer["stroke"])
            continue
        sheet.shape(layer["center"], layer["size"], layer["kind"], rgba(layer["color"]),
                    layer["rotation"], outline=rgba(INK, 0.55), width=3)
    return image


def build_overview():
    ppu = AUTHOR_PPU
    bounds = character_bounds()
    min_x, min_y, max_x, max_y = bounds
    height_units = max_y - min_y
    width_units = max_x - min_x

    figure = render_layers(character_layers() + ring_layers(), bounds, ppu, 0.40)

    panel_w = 980
    margin = 40
    top = 190
    canvas_w = margin * 2 + figure.width + panel_w
    canvas_h = max(figure.height + top + 90, 1500)
    image = Image.new("RGBA", (canvas_w, canvas_h), rgba(PAPER))
    draw = ImageDraw.Draw(image, "RGBA")

    title_font = load_font(48)
    subtitle_font = load_font(25)
    label_font = load_font(26)
    small_font = load_font(22)

    draw.text((margin, 40), "《影韵》皮影角色 · 整体造型与比例参考（程序灰盒实尺）",
              font=title_font, fill=rgba(INK))
    draw.text((margin + 2, 104),
              "单位：Unity 世界单位　·　本图 1 单位 = 400 px（与 02 分片清单的作者像素一致）",
              font=subtitle_font, fill=rgba(MUTED))
    draw.text((margin + 2, 140),
              "面向：右（侧脸鼻朝 +X）　·　躯干与下裳会以 localScale.x = ±1 镜像，左右需能互换",
              font=subtitle_font, fill=rgba(MUTED))

    paste_x, paste_y = margin, top
    image.alpha_composite(figure, (paste_x, paste_y))

    def to_px(point):
        return (paste_x + (point[0] - (min_x - 0.40)) * ppu,
                paste_y + ((max_y + 0.40) - point[1]) * ppu)

    dim = (0.15, 0.35, 0.65, 1.0)
    left_px = to_px((min_x, max_y))
    right_px = to_px((max_x, min_y))
    draw.line([to_px((min_x, max_y)), to_px((min_x, min_y))], fill=rgba(dim), width=3)
    draw.line([to_px((max_x, max_y)), to_px((max_x, min_y))], fill=rgba(dim), width=3)
    draw.line([to_px((min_x, max_y)), to_px((max_x, max_y))], fill=rgba(dim), width=3)
    draw.line([to_px((min_x, min_y)), to_px((max_x, min_y))], fill=rgba(dim), width=3)
    draw.text((right_px[0] + 18, (left_px[1] + right_px[1]) / 2.0),
              f"总高 {height_units:.2f} 单位",
              font=label_font, fill=rgba(INK), anchor="lm")
    draw.text(((left_px[0] + right_px[0]) / 2.0, to_px((0.0, max_y))[1] - 52),
              f"总宽 {width_units:.2f} 单位", font=label_font, fill=rgba(INK), anchor="mm")

    # 右侧说明栏
    panel_x = paste_x + figure.width + 50
    panel_y = top + 6
    draw.text((panel_x, panel_y), "颜色定义（灰盒色，仅示意层次）", font=label_font, fill=rgba(INK))
    panel_y += 50
    for color, name in [
        (SHADOW, "主体剪影 ShadowColor　皮料实体"),
        (ACCENT, "装饰／端帽／铆钉 AccentColor"),
        ((0.32, 0.045, 0.025, 0.82), "水袖拖尾（半透明）"),
        ((0.78, 0.28, 0.055, 0.72), "胸口镂空（半透明）"),
    ]:
        draw.rectangle([panel_x, panel_y, panel_x + 44, panel_y + 44],
                       fill=rgba(color), outline=rgba(INK, 0.45), width=2)
        draw.text((panel_x + 60, panel_y + 22), name, font=small_font, fill=rgba(INK), anchor="lm")
        panel_y += 66

    panel_y += 26
    draw.text((panel_x, panel_y), "尺码与数量", font=label_font, fill=rgba(INK))
    panel_y += 48
    unique = [p for p in PARTS if not p["code"].startswith(("Joint Pin", "Limb End Cap"))]
    for line in [
        f"角色分片 {len(unique)} 种，左右共用同一张图（镜像使用）",
        f"总高 {height_units:.2f} 单位　总宽 {width_units:.2f} 单位",
        f"1080p 实际占屏：{width_units * CAMERA_PPU_1080:.0f} × {height_units * CAMERA_PPU_1080:.0f} px",
        f"建议作者尺寸：{width_units * AUTHOR_PPU:.0f} × {height_units * AUTHOR_PPU:.0f} px（400 px/单位）",
        "肢体端帽 10 处、关节铆钉 15 处由程序生成，不必单独出图",
        "但端帽与铆钉的颜色须与皮料协调，避免盖住关节",
    ]:
        draw.text((panel_x, panel_y), line, font=small_font, fill=rgba(MUTED))
        panel_y += 34

    panel_y += 26
    draw.text((panel_x, panel_y), "旋转余量（美术必须预留）", font=label_font, fill=rgba(INK))
    panel_y += 48
    for line in [
        "腰／颈／肩／肘／腕／髋／膝／踝全部会转动",
        "上臂 0.82、前臂 0.72、大腿 0.88、小腿 0.82 单位，只沿长轴旋转",
        "冠翅固定 ±12°，水袖拖尾会随惯性摆动，需可弯曲或分层",
        "关节重叠处不要留单层硬边，避免旋转时露出缝隙",
        "边缘建议多留 2–3 px，旋转时不露底",
    ]:
        draw.text((panel_x, panel_y), line, font=small_font, fill=rgba(MUTED))
        panel_y += 34

    # 1080p 屏占比示意（放在右栏，不压住角色图）
    panel_y += 30
    draw.text((panel_x, panel_y), "1080p 屏占比（16:9，正交 size 5）", font=label_font, fill=rgba(INK))
    panel_y += 46
    inset_w = panel_w - 60
    inset_h = int(inset_w * 9.0 / 16.0)
    inset_x, inset_y = panel_x, panel_y
    draw.rectangle([inset_x, inset_y, inset_x + inset_w, inset_y + inset_h],
                   fill=rgba((0.10, 0.07, 0.05, 0.06)), outline=rgba(INK, 0.7), width=3)
    scale = (CAMERA_PPU_1080 / ppu) * (inset_w / 1920.0)
    mini = figure.resize((max(1, int(figure.width * scale)), max(1, int(figure.height * scale))), Image.LANCZOS)
    image.alpha_composite(mini, (int(inset_x + (inset_w - mini.width) / 2.0),
                                 int(inset_y + (inset_h - mini.height) / 2.0)))
    draw.text((inset_x, inset_y + inset_h + 12),
              f"角色在 1080p 上约占 {width_units * CAMERA_PPU_1080:.0f} × {height_units * CAMERA_PPU_1080:.0f} px，"
              "细节请以 4K 或缩放后仍清楚为准。",
              font=small_font, fill=rgba(MUTED))

    image.convert("RGB").save(os.path.join(HERE, "01-角色整体与比例参考.png"), quality=95)
    return height_units, width_units


def build_part_sheet():
    ppu = 300.0
    columns = 4
    cell_w = 640
    cell_h = 440
    rows = (len(PARTS) + columns - 1) // columns
    header = 150
    image = Image.new("RGBA", (columns * cell_w, header + rows * cell_h + 40), rgba(CARD))
    draw = ImageDraw.Draw(image, "RGBA")
    title_font = load_font(40)
    name_font = load_font(25)
    tiny_font = load_font(19)

    draw.text((32, 28), "《影韵》皮影角色 · 分片清单（每片一张 PNG，中心 pivot）", font=title_font, fill=rgba(INK))
    draw.text((34, 84), "每格：形状／相对大小／中心轴心（蓝色十字）／尺寸换算。命名沿用下表代码名称，左右两侧共用同一张图。",
              font=tiny_font, fill=rgba(MUTED))
    draw.text((34, 112), "轴心一律取图片正中心（0.5, 0.5），与程序一致；不要预先旋转，旋转由程序套用。",
              font=tiny_font, fill=rgba(MUTED))

    for index, entry in enumerate(PARTS):
        col = index % columns
        row = index // columns
        x0 = col * cell_w
        y0 = header + row * cell_h
        draw.rectangle([x0 + 12, y0 + 12, x0 + cell_w - 12, y0 + cell_h - 12],
                       outline=rgba(RULE), width=2, fill=rgba((1.0, 1.0, 0.995, 1.0)))

        max_w = (cell_w - 160) / ppu
        max_h = (cell_h - 190) / ppu
        scale = min(1.0, max_w / max(entry["size"][0], 1e-4), max_h / max(entry["size"][1], 1e-4))
        cx = x0 + cell_w / 2.0
        cy = y0 + 158
        w = entry["size"][0] * ppu * scale
        h = entry["size"][1] * ppu * scale
        box = [cx - w / 2.0, cy - h / 2.0, cx + w / 2.0, cy + h / 2.0]
        if entry["shape"] == "ellipse":
            draw.ellipse(box, fill=rgba(entry["color"]), outline=rgba(INK, 0.5), width=2)
        else:
            draw.rectangle(box, fill=rgba(entry["color"]), outline=rgba(INK, 0.5), width=2)

        draw.rectangle(box, outline=rgba((0.15, 0.35, 0.65, 0.85)), width=1)
        draw.line([cx - 16, cy, cx + 16, cy], fill=rgba(PIVOT), width=3)
        draw.line([cx, cy - 16, cx, cy + 16], fill=rgba(PIVOT), width=3)

        draw.text((x0 + 26, y0 + 268), entry["cn"], font=name_font, fill=rgba(INK))
        draw.text((x0 + 26, y0 + 304), entry["code"], font=tiny_font, fill=rgba(MUTED))
        draw.text((x0 + 26, y0 + 332),
                  f"{entry['size'][0]:.3f} × {entry['size'][1]:.3f} 单位", font=tiny_font, fill=rgba(MUTED))
        draw.text((x0 + 26, y0 + 358),
                  f"{entry['size'][0] * AUTHOR_PPU:.0f} × {entry['size'][1] * AUTHOR_PPU:.0f} px @400/单位",
                  font=tiny_font, fill=rgba(MUTED))
        draw.text((x0 + 26, y0 + 384), f"挂点 {entry['parent']} · 层次 {entry['order']}",
                  font=tiny_font, fill=rgba(MUTED))

    image.convert("RGB").save(os.path.join(HERE, "02-分片清单.png"), quality=95)


def build_skeleton():
    ppu = 210.0
    bounds = (-3.65, -3.35, 3.65, 3.05)
    width = int(round((bounds[2] - bounds[0]) * ppu))
    height = int(round((bounds[3] - bounds[1]) * ppu))
    figure = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    sheet = Sheet(figure, ((-bounds[0]) * ppu, bounds[3] * ppu), ppu)

    for entry in character_layers():
        sheet.shape(entry["center"], entry["size"], entry["kind"],
                    rgba(entry["color"], 0.18), entry["rotation"],
                    outline=rgba(INK, 0.22), width=1)

    positions = {name: pos for name, _, pos, _ in JOINTS}
    number_font = load_font(19)
    badge_font = load_font(20)

    for name, cn, pos, parent in JOINTS:
        if parent and parent in positions:
            sheet.line(positions[parent], pos, (0.22, 0.34, 0.62, 0.9), 3)

    rod_color = (0.38, 0.16, 0.055, 0.9)
    for index, target in enumerate(ROD_TARGETS.values()):
        grip = ROD_GRIP_POINTS[index]
        target_pos = positions.get(target, (0.0, 0.93))
        sheet.line(grip, target_pos, rod_color, 4)
    for index, grip in enumerate(ROD_GRIP_POINTS):
        sheet.shape(grip, (0.46, 0.12), "ellipse", rod_color, 0.0,
                    outline=rgba(INK, 0.55), width=2)
        label_pos = (grip[0], grip[1] - 0.30 if grip[1] > 0 else grip[1] + 0.30)
        sheet.text(sheet.px(label_pos), f"杆{index}", badge_font, INK, "mm")

    for index, (name, cn, pos, parent) in enumerate(JOINTS, start=1):
        offset = (0.16, 0.15) if pos[0] > 0.01 else ((-0.16, 0.15) if pos[0] < -0.01 else (0.0, 0.22))
        anchor = "mm"
        center = (pos[0] + offset[0], pos[1] + offset[1])
        cx, cy = sheet.px(center)
        radius = 15
        sheet.draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius],
                           fill=rgba((1.0, 0.83, 0.28, 1.0)), outline=rgba(INK, 0.8), width=2)
        sheet.text((cx, cy), str(index), number_font, INK, anchor)

    panel_w = 900
    margin = 40
    top = 170
    canvas = Image.new("RGBA", (margin * 2 + figure.width + panel_w, max(figure.height + top + 60, 1400)),
                       rgba(PAPER))
    draw = ImageDraw.Draw(canvas, "RGBA")
    title_font = load_font(46)
    label_font = load_font(26)
    small_font = load_font(22)

    draw.text((margin, 36), "《影韵》皮影角色 · 关节层级与竹杆传动", font=title_font, fill=rgba(INK))
    draw.text((margin + 2, 98), "美术需要知道：哪些部件会转动、转到什么程度，以及六根竹杆分别拉动哪里。",
              font=small_font, fill=rgba(MUTED))
    canvas.alpha_composite(figure, (margin, top))

    panel_x = margin + figure.width + 46
    panel_y = top
    draw.text((panel_x, panel_y), "关节编号（父 → 子）", font=label_font, fill=rgba(INK))
    panel_y += 46
    parent_cn = {name: cn for name, cn, _, _ in JOINTS}
    for index, (name, cn, _, parent) in enumerate(JOINTS, start=1):
        chain = f"{index:>2}. {cn}"
        if parent:
            chain += f"　← {parent_cn.get(parent, parent)}"
        else:
            chain += "　（根节点）"
        draw.text((panel_x, panel_y), chain, font=small_font, fill=rgba(INK))
        panel_y += 31

    panel_y += 22
    draw.text((panel_x, panel_y), "竹杆传动对应", font=label_font, fill=rgba(INK))
    panel_y += 44
    for index, target in enumerate(ROD_TARGETS.values()):
        draw.text((panel_x, panel_y), f"杆{index} → {parent_cn.get(target, '头部（无关节）')}",
                  font=small_font, fill=rgba(INK))
        panel_y += 30

    panel_y += 22
    for line in [
        "上臂 0.82、前臂 0.72、大腿 0.88、小腿 0.82 单位，只沿长轴旋转。",
        "肩、肘、腕、髋、膝、踝需大角度旋转；冠翅固定 ±12°。",
        "躯干与下裳会被镜像（转身动作），左右必须能互换。",
        "水袖拖尾有独立摆动，建议拆成 2–3 层或可变形网格。",
    ]:
        draw.text((panel_x, panel_y), line, font=small_font, fill=rgba(MUTED))
        panel_y += 32

    canvas.convert("RGB").save(os.path.join(HERE, "03-关节层级与竹杆传动.png"), quality=95)

def save_svg():
    min_x, min_y, max_x, max_y = character_bounds()
    pad = 0.8
    vb = (min_x - pad, -(max_y + pad), (max_x - min_x) + pad * 2, (max_y - min_y) + pad * 2)
    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{vb[0]:.3f} {vb[1]:.3f} {vb[2]:.3f} {vb[3]:.3f}">',
        "<desc>《影韵》皮影角色分片装配图。每个 &lt;g&gt; 的 id 对应 ShadowPuppetPresenter 里的 GameObject 名称，"
        "圆心／矩形中心即世界坐标，也是美术图的 pivot。</desc>",
        '<g id="Character" fill="none" stroke="#1a0f0a" stroke-width="0.012">',
    ]
    for entry in sorted(PARTS, key=lambda item: item["order"]):
        if entry["code"].startswith(("Joint Pin", "Limb End Cap")):
            continue
        cx, cy = world_of(entry)
        w, h = entry["size"]
        transform = f'translate({cx:.4f}, {cy:.4f})'
        if abs(entry["rotation"]) > 0.001:
            transform += f' rotate({-entry["rotation"]:.3f})'
        lines.append(
            f'  <g id="{entry["code"]}" data-layer="{entry["order"]}" data-cn="{entry["cn"]}" transform="{transform}">'
        )
        if entry["shape"] == "ellipse":
            lines.append(f'    <ellipse cx="0" cy="0" rx="{w / 2:.4f}" ry="{h / 2:.4f}"/>')
        else:
            lines.append(
                f'    <rect x="{-w / 2:.4f}" y="{-h / 2:.4f}" width="{w:.4f}" height="{h:.4f}"/>'
            )
        lines.append("  </g>")
    lines.append("</g>")
    lines.append('<g id="Joints" fill="#ffd54a" stroke="#1a0f0a" stroke-width="0.01">')
    for name, cn, pos, _ in JOINTS:
        lines.append(f'  <circle id="{name}" data-cn="{cn}" cx="{pos[0]:.4f}" cy="{pos[1]:.4f}" r="0.07"/>')
    lines.append("</g>")
    lines.append("</svg>")
    with open(os.path.join(HERE, "Puppet-角色装配.svg"), "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines))


STAGE_PARTS = [
    part("Warm Backlight", "暖色背光", "Stage", (0.0, 0.0), (5.40, 6.15), "ellipse", (1.0, 0.73, 0.32, 0.22), -7),
    part("Translucent Paper Screen", "半透明纸幕", "Stage", (0.0, 0.0), (4.65, 5.55), "ellipse", (1.0, 0.88, 0.60, 0.88), -6),
    part("Stage Header", "戏台横梁", "Stage", (0.0, 3.02), (5.65, 0.22), "rect", (0.28, 0.055, 0.025, 0.98), -4),
    part("Stage Left Post", "戏台左立柱", "Stage", (-2.72, 0.0), (0.20, 6.15), "rect", (0.28, 0.055, 0.025, 0.98), -4),
    part("Stage Right Post", "戏台右立柱", "Stage", (2.72, 0.0), (0.20, 6.15), "rect", (0.28, 0.055, 0.025, 0.98), -4),
    part("Stage Foot", "戏台底座", "Stage", (0.0, -3.02), (5.65, 0.24), "rect", (0.28, 0.055, 0.025, 0.98), -4),
    part("Scenery Left Mountain", "左侧远山", "Stage", (-1.72, -2.25), (1.75, 0.52), "ellipse", (0.30, 0.075, 0.035, 0.22), -3),
    part("Scenery Right Mountain", "右侧远山", "Stage", (1.55, -2.32), (2.25, 0.44), "ellipse", (0.30, 0.075, 0.035, 0.22), -3),
    part("Foot Contact Line", "脚位接触线", "Stage", (0.0, -2.15), (2.75, 0.055), "rect", (0.25, 0.075, 0.025, 0.58), 2),
    part("Traditional Shadow Play Background", "戏台背景图（已有资产）", "Stage", (0.0, 0.0), (10.0, 10.0), "rect", (0.55, 0.42, 0.30, 0.35), -20),
    part("Bamboo Control Rod", "竹制操偶杆 ×6（线状）", "Stage", (0.0, 0.0), (0.075, 0.075), "ellipse", (0.38, 0.16, 0.055, 0.98), 9),
    part("Bamboo Grip", "竹杆握把 ×6", "Stage", (0.0, 0.0), (0.46, 0.12), "ellipse", (0.38, 0.16, 0.055, 0.98), 10),
]


def build_stage():
    ppu = 190.0
    bounds = (-3.30, -3.45, 3.30, 3.40)
    width = int(round((bounds[2] - bounds[0]) * ppu))
    height = int(round((bounds[3] - bounds[1]) * ppu))
    figure = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    sheet = Sheet(figure, ((-bounds[0]) * ppu, bounds[3] * ppu), ppu)

    # 背景图（现有资产）作为底层淡色矩形
    sheet.shape((0.0, 0.0), (bounds[2] - bounds[0], bounds[3] - bounds[1]), "rect",
                (0.55, 0.42, 0.30, 0.22), 0.0, outline=rgba(INK, 0.25), width=2)
    for entry in sorted(STAGE_PARTS, key=lambda item: item["order"]):
        if entry["code"] in ("Bamboo Control Rod", "Bamboo Grip",
                             "Traditional Shadow Play Background"):
            continue
        sheet.shape(entry["offset"], entry["size"], entry["shape"], rgba(entry["color"]),
                    outline=rgba(INK, 0.35), width=2)

    positions = {name: pos for name, _, pos, _ in JOINTS}
    rod_color = (0.38, 0.16, 0.055, 0.85)
    for index, target in enumerate(ROD_TARGETS.values()):
        grip = ROD_GRIP_POINTS[index]
        sheet.line(grip, positions.get(target, (0.0, 0.93)), rod_color, 4)
    for index, grip in enumerate(ROD_GRIP_POINTS):
        sheet.shape(grip, (0.46, 0.12), "ellipse", rod_color, 0.0,
                    outline=rgba(INK, 0.5), width=2)

    for entry in character_layers():
        sheet.shape(entry["center"], entry["size"], entry["kind"], rgba(entry["color"], 0.9),
                    entry["rotation"], outline=rgba(INK, 0.45), width=2)

    panel_w = 860
    margin = 40
    top = 170
    canvas = Image.new("RGBA", (margin * 2 + figure.width + panel_w, max(figure.height + top + 60, 1400)),
                       rgba(PAPER))
    draw = ImageDraw.Draw(canvas, "RGBA")
    title_font = load_font(46)
    label_font = load_font(26)
    small_font = load_font(22)
    tiny_font = load_font(20)

    draw.text((margin, 36), "《影韵》戏台与道具 · 灰盒实尺与美术清单", font=title_font, fill=rgba(INK))
    draw.text((margin + 2, 98), "与角色同一坐标系；戏台外框宽 5.65、高 6.15 单位，脚位落地线在 y = -2.08。",
              font=small_font, fill=rgba(MUTED))
    canvas.alpha_composite(figure, (margin, top))

    panel_x = margin + figure.width + 46
    panel_y = top
    draw.text((panel_x, panel_y), "戏台／道具分片清单", font=label_font, fill=rgba(INK))
    panel_y += 30
    draw.text((panel_x, panel_y), "名称 / 代码　　　　　　　　尺寸（单位）　层次", font=tiny_font, fill=rgba(MUTED))
    panel_y += 36
    for entry in STAGE_PARTS:
        draw.text((panel_x, panel_y), entry["cn"], font=small_font, fill=rgba(INK))
        draw.text((panel_x, panel_y + 24), entry["code"], font=tiny_font, fill=rgba(MUTED))
        draw.text((panel_x + 520, panel_y + 10),
                  f"{entry['size'][0]:.2f} × {entry['size'][1]:.2f}",
                  font=small_font, fill=rgba(INK), anchor="lm")
        draw.text((panel_x + 720, panel_y + 10), str(entry["order"]),
                  font=small_font, fill=rgba(MUTED), anchor="lm")
        panel_y += 60

    panel_y += 16
    for line in [
        "说明",
        "背光与纸幕目前是两张椭圆，以颜色与透明度叠加；可改成带纸纹的矩形，",
        "但外框不要超过上表尺寸，否则会盖住戏台框。",
        "背景图沿用现有 Assets/Resources/YingYun/backgroundpicture.jpg，不必重画。",
        "竹杆由 LineRenderer 画线，不是贴图；只有握把需要出图。",
        "远山绘制时不要压过 y = -2.08 的落地线，否则会遮住脚板。",
    ]:
        draw.text((panel_x, panel_y), line, font=small_font, fill=rgba(MUTED))
        panel_y += 32

    canvas.convert("RGB").save(os.path.join(HERE, "04-戏台与道具清单.png"), quality=95)


def snake(name):
    out = []
    for index, ch in enumerate(name):
        if ch == " ":
            out.append("_")
        elif ch.isupper() and index > 0 and not name[index - 1].isupper() and name[index - 1] != " ":
            out.append("_" + ch.lower())
        else:
            out.append(ch.lower())
    return "".join(out).replace("_", "_")


def export_csv():
    import csv

    path = os.path.join(HERE, "分片规格表.csv")
    with open(path, "w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow([
            "序号", "中文名", "代码对象名", "建议文件名", "挂接关节",
            "世界宽", "世界高", "作者像素宽@400", "作者像素高@400",
            "sortingOrder", "形状", "左右镜像", "备注",
        ])
        for index, entry in enumerate(PARTS, start=1):
            code = entry["code"]
            mirrored = "是" if ("Left" in code or "Right" in code or code in (
                "Torso", "Robe Skirt", "Robe Hem", "Robe Front Trim",
                "Chest Cutout", "Waist Ornament", "Head", "Head Crown",
                "Crown Jewel", "Profile Nose", "Profile Eye", "Trailing Water Sleeve",
                "Sleeve Tail Cutwork", "Sleeve Cuff Plate", "Limb End Cap", "Joint Pin",
            )) else "否"
            note = ""
            if code.startswith(("Joint Pin", "Limb End Cap")):
                note = "程序自动生成，只需确认与皮料颜色协调"
            elif code == "Robe Front Trim":
                note = "下裳子物件，尺寸已换算为最终显示值"
            elif code == "Single Finger Silhouette":
                note = "只在「單指（剑指）」动作出现，可单独出图"
            writer.writerow([
                index, entry["cn"], code, "puppet_" + snake(code) + ".png", entry["parent"],
                f"{entry['size'][0]:.3f}", f"{entry['size'][1]:.3f}",
                f"{entry['size'][0] * AUTHOR_PPU:.0f}", f"{entry['size'][1] * AUTHOR_PPU:.0f}",
                entry["order"], entry["shape"], mirrored, note,
            ])
        for index, entry in enumerate(STAGE_PARTS, start=101):
            writer.writerow([
                index, entry["cn"], entry["code"], "stage_" + snake(entry["code"]) + ".png", "场景",
                f"{entry['size'][0]:.3f}", f"{entry['size'][1]:.3f}",
                f"{entry['size'][0] * AUTHOR_PPU:.0f}", f"{entry['size'][1] * AUTHOR_PPU:.0f}",
                entry["order"], entry["shape"], "否",
                "背景图沿用现有资产" if entry["code"].startswith("Traditional") else "",
            ])
    return path


def main():
    height_units, width_units = build_overview()
    build_part_sheet()
    build_skeleton()
    build_stage()
    save_svg()
    csv_path = export_csv()
    print(f"总体尺寸：宽 {width_units:.3f} 单位 × 高 {height_units:.3f} 单位")
    print(f"1080p 实际像素：{width_units * CAMERA_PPU_1080:.0f} × {height_units * CAMERA_PPU_1080:.0f} px")
    print(f"作者尺寸（400 px/单位）：{width_units * AUTHOR_PPU:.0f} × {height_units * AUTHOR_PPU:.0f} px")
    print(f"角色分片种类：{len([p for p in PARTS if not p['code'].startswith(('Joint Pin', 'Limb End Cap'))])}")
    print("规格表：", csv_path)
    print("已输出到", HERE)


if __name__ == "__main__":
    main()
