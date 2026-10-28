"""Generate the HUD sprites from the HUD.aseprite mockup.

The mockup (Assets/Sprites/UI/HUD/HUD.aseprite) is a single-layer 1920x1080
picture of the whole HUD. Every element is reproduced here 1:1 in size:
plain frames are drawn procedurally from the mockup palette, and the art
(icons, key hints, silhouettes) is cut straight out of the mockup. The script
ends with a self-check that pastes every sprite back at its mockup rect and
reports any pixel that differs.

Usage:  python Tools/gen_hud_sprites.py
Output: Assets/Sprites/UI/HUD/Generated/*.png
"""
import math
import os
import struct
import sys
import zlib

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOCKUP = os.path.join(ROOT, "Assets", "Sprites", "UI", "HUD", "HUD.aseprite")
OUT = os.path.join(ROOT, "Assets", "Sprites", "UI", "HUD", "Generated")

# mockup palette
BG = (155, 173, 183, 255)       # screen background (transparent in sprites)
BORDER = (64, 82, 115, 255)
FILL = (108, 129, 161, 255)
BEIGE = (188, 173, 159, 255)
RED = (181, 89, 69, 255)
WARM = (218, 134, 62, 255)      # warmth bar: the campfire orange from the mockup
WHITE = (255, 255, 255, 255)
TEXT = (241, 246, 240, 255)     # mockup's off-white
LIGHT = (190, 204, 222, 255)
CLEAR = (0, 0, 0, 0)


# frames whose mockup interior shows runtime content (bar fill, label text)
FILLED_AT_RUNTIME = {"bar_xp_frame", "bar_health_frame", "subtab_selected"}


def read_aseprite(path):
    """Composite of frame 0 (visible layers only), as an RGBA image."""
    d = open(path, "rb").read()
    _, magic, frames, w, h, depth = struct.unpack_from("<IHHHHH", d, 0)
    if magic != 0xA5E0 or depth != 32:
        sys.exit("unsupported aseprite file (need RGBA)")
    img = Image.new("RGBA", (w, h), CLEAR)
    layers = []
    pos = 128
    fbytes, _, old_chunks, _, _, new_chunks = struct.unpack_from("<IHHH2sI", d, pos)
    p = pos + 16
    for _ in range(new_chunks or old_chunks):
        size, ctype = struct.unpack_from("<IH", d, p)
        body = d[p + 6:p + size]
        if ctype == 0x2004:                                  # layer
            layers.append(struct.unpack_from("<H", body, 0)[0] & 1)
        elif ctype == 0x2005:                                # cel
            li, x, y, _, ct = struct.unpack_from("<HhhBH", body, 0)
            if ct in (0, 2) and layers[li]:
                cw, ch = struct.unpack_from("<HH", body, 16)
                raw = body[20:] if ct == 0 else zlib.decompress(body[20:])
                img.alpha_composite(Image.frombytes("RGBA", (cw, ch), raw), (x, y))
        p += size
    return img


def frame(w, h, border, inner, bw=2):
    """Rectangle with a `bw`-px border; `inner` may be CLEAR."""
    im = Image.new("RGBA", (w, h), border)
    im.paste(Image.new("RGBA", (w - 2 * bw, h - 2 * bw), inner), (bw, bw))
    return im


def solid(w, h, color):
    return Image.new("RGBA", (w, h), color)


def cut(mock, x, y, w, h, keep=None):
    """Crop a mockup rect. Background turns transparent; with `keep`, only the
    listed colours survive (used to lift an icon off a filled tab/slot)."""
    im = mock.crop((x, y, x + w, y + h))
    px = im.load()
    for j in range(h):
        for i in range(w):
            c = px[i, j]
            if c == BG or (keep is not None and c not in keep):
                px[i, j] = CLEAR
    return im


def trimmed(im):
    """Crop to the opaque bounds; returns (image, (dx, dy)) offset into the source."""
    box = im.getbbox()
    return im.crop(box), (box[0], box[1])


def whirlwind_icon():
    """24x24 tornado: stacked rings narrowing downward, near halves bright, far
    halves dimmer, with a 1px dark outline so it reads on the slot fill."""
    im = Image.new("RGBA", (24, 24), CLEAR)
    px = im.load()
    for i in range(6):
        cy = 3.5 + i * 3.4
        hw = 10.5 - i * 1.65
        hh = 1.6 - i * 0.12
        cx = 11.5 + math.sin(i * 1.3) * 1.4     # rings wobble sideways like a swirl
        for k in range(360):
            a = math.radians(k)
            x, y = round(cx + hw * math.cos(a)), round(cy + hh * math.sin(a))
            if not (0 <= x < 24 and 0 <= y < 24):
                continue
            near = math.sin(a) > 0
            if near or px[x, y] != TEXT:
                px[x, y] = TEXT if near else LIGHT
    out = im.copy()
    op = out.load()
    for y in range(24):
        for x in range(24):
            if px[x, y][3] == 0 and any(0 <= x + dx < 24 and 0 <= y + dy < 24 and px[x + dx, y + dy][3]
                                        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                op[x, y] = BORDER
    return out


def main():
    mock = read_aseprite(MOCKUP)
    os.makedirs(OUT, exist_ok=True)

    # name -> (image, (x, y) in mockup for the self-check, or None)
    sprites = {}

    def add(name, im, at=None):
        sprites[name] = (im, at)

    # --- procedural frames (exact mockup sizes and border widths) ---
    add("bar_xp_frame", frame(115, 8, BORDER, CLEAR, bw=1), (87, 21))
    add("bar_xp_fill", solid(113, 6, FILL))
    add("bar_health_frame", frame(240, 16, BORDER, CLEAR), (87, 31))
    add("bar_health_fill", solid(236, 12, RED))
    add("bar_warmth_fill", solid(236, 12, WARM))
    add("tab_selected", frame(64, 32, BORDER, CLEAR))
    add("tab_unselected", solid(64, 32, BORDER))
    add("subtab_selected", frame(56, 26, BORDER, CLEAR), (588, 85))
    add("subtab_unselected", solid(56, 26, BORDER))
    add("slot_64", frame(64, 64, BORDER, FILL), (655, 114))
    add("slot_64x46", frame(64, 46, BORDER, FILL))
    add("node_small", frame(64, 32, BORDER, FILL))
    add("tooltip_frame", frame(126, 77, BORDER, FILL), (162, 444))   # 9-sliced: sized to its text
    add("ui_pixel", solid(4, 4, WHITE))
    add("icon_spell_whirlwind", whirlwind_icon())

    # --- cut from the mockup ---
    add("hand_frame_selected", cut(mock, 736, 968, 96, 84), (736, 968))
    add("hand_frame", cut(mock, 1040, 988, 96, 64), (1040, 988))
    add("action_slot", cut(mock, 848, 988, 80, 64), (848, 988))

    # tab icons: the fill-coloured glyph inside an unselected (solid) tab
    for name, (x, y) in {"icon_tab_player": (588, 50),
                         "icon_tab_crafting": (154, 49),
                         "icon_tab_skills": (220, 49)}.items():
        im, (dx, dy) = trimmed(cut(mock, x + 2, y + 2, 60, 28, keep=[FILL]))
        add(name, im, (x + 2 + dx, y + 2 + dy))

    # equipment placeholders: everything inside the slot that isn't slot fill
    for name, (x, y, w, h) in {"equip_ph_head": (87, 84, 64, 64),
                               "equip_ph_chest": (86, 151, 64, 64),
                               "equip_ph_legs": (86, 217, 64, 64),
                               "equip_ph_ring": (153, 137, 64, 46),
                               "equip_ph_amulet": (153, 185, 64, 46)}.items():
        inner = cut(mock, x + 2, y + 2, w - 4, h - 4)
        px = inner.load()
        for j in range(inner.height):
            for i in range(inner.width):
                if px[i, j] == FILL:
                    px[i, j] = CLEAR
        im, (dx, dy) = trimmed(inner)
        add(name, im, (x + 2 + dx, y + 2 + dy))

    im, (dx, dy) = trimmed(cut(mock, 1320, 115, 50, 45, keep=[FILL]))
    add("icon_weapon_axe", im, (1320 + dx, 115 + dy))

    # key / mouse hints
    for name, (x, y, w, h) in {"hint_mouse_lmb": (772, 943, 17, 22),
                               "hint_mouse_rmb": (1083, 962, 17, 22),
                               "key_1": (877, 962, 23, 22),
                               "key_2": (973, 962, 23, 22),
                               "key_q": (769, 1055, 23, 22)}.items():
        add(name, cut(mock, x, y, w, h), (x, y))
    for name, (x, y, w, h) in {"hint_scroll": (706, 998, 28, 44),
                               "hint_tab": (900, 1052, 72, 28)}.items():
        im, (dx, dy) = trimmed(cut(mock, x, y, w, h))
        add(name, im, (x + dx, y + dy))

    for name, (im, _) in sprites.items():
        im.save(os.path.join(OUT, name + ".png"))

    # --- self-check: every placed sprite must reproduce its mockup pixels ---
    bad = 0
    for name, (im, at) in sprites.items():
        if at is None:
            continue
        x, y = at
        ref = mock.crop((x, y, x + im.width, y + im.height))
        rp, sp = ref.load(), im.load()
        diff = sum(1 for j in range(im.height) for i in range(im.width)
                   if sp[i, j][3] and sp[i, j] != rp[i, j])
        missing = sum(1 for j in range(im.height) for i in range(im.width)
                      if not sp[i, j][3] and rp[i, j] != BG
                      and name not in FILLED_AT_RUNTIME
                      and not name.startswith(("icon_", "equip_ph_")))
        status = "ok" if diff == 0 and missing == 0 else "DIFF"
        if status != "ok":
            bad += 1
        print(f"{status:4} {name:22} {im.width}x{im.height} at {at} diff={diff} missing={missing}")
    for name, (im, at) in sprites.items():
        if at is None:
            print(f"gen  {name:22} {im.width}x{im.height}")
    print(f"{len(sprites)} sprites -> {OUT}" + (f"  ({bad} mismatches)" if bad else ""))


if __name__ == "__main__":
    main()
