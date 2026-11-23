#!/usr/bin/env python3
# Inferus - map diff helper for the Map Diff workflow (.github/workflows/map-diff.yml).
"""Compare two versions of SS14 map/grid YAML files and build zoomed before/after images.

Works on Content.MapRenderer output: one PNG per grid, 32 px per tile, north up, the
image spans the grid's tile bounds (min x on the left, max y on the top).

Usage (paths are relative to the repo root unless absolute):

  map_diff.py --repo . --changes changes.tsv \
      --base-src /tmp/mapdiff/src/base --base-renders /tmp/mapdiff/render/base \
      --head-renders /tmp/mapdiff/render/head --out /tmp/mapdiff/out

changes.tsv lines: <status A|M|D>\t<Resources/Maps/... path>
Base sources live at <base-src>/<path>; head sources are read from the repo checkout.
Renders live at <renders>/<file stem>/<file stem>-<grid index>.png.

Writes <out>/images/*.png and <out>/comment.md. Image links in comment.md use the
placeholder {{IMAGE_BASE}} that the comment workflow replaces with the hosting URL.
Needs: pyyaml, Pillow.
"""

import argparse
import base64
import glob
import json
import math
import os
import re
import struct
import sys
from collections import Counter

import yaml
from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None
PX = 32                 # Content.MapRenderer: TilePainter.TileImageSize (EyeManager.PixelsPerMeter)
CLUSTER_GAP = 6         # tiles; changes closer than this merge into one area
PAD = 5                 # tiles of context around each area
MIN_SPAN = 14           # minimum crop size in tiles
MAX_AREAS = 8           # per map; the rest are merged into the last one
MAX_PANEL_PX = 1600     # each before/after panel is scaled down only past this (32 px per tile otherwise)
UID_KEYS = {"devices", "linkedPorts", "deviceLists", "configurators", "outputs", "inputs",
            "sinks", "sources", "containers", "ents", "entities", "lastSignals"}


class TolerantLoader(yaml.SafeLoader):
    pass


def _any_tag(loader, suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node, deep=True)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node, deep=True)
    return loader.construct_scalar(node)


TolerantLoader.add_multi_constructor("!", _any_tag)


def load(path):
    with open(path, encoding="utf-8") as f:
        return yaml.load(f, Loader=TolerantLoader) or {}


def vec(s):
    if s is None:
        return (0.0, 0.0)
    a, b = str(s).split(",")
    return (float(a), float(b))


def strip_uids(v):
    if isinstance(v, dict):
        return {k: strip_uids(x) for k, x in v.items() if k not in UID_KEYS}
    if isinstance(v, list):
        return [strip_uids(x) for x in v]
    return v


class MapData:
    """Tiles, entities and decals per grid, in grid-local tile coordinates."""

    def __init__(self, path):
        doc = load(path)
        tilemap = {int(k): v for k, v in (doc.get("tilemap") or {}).items()}
        self.grids = [int(g) for g in (doc.get("grids") or [])]
        self.tiles = {g: {} for g in self.grids}
        self.decals = {g: Counter() for g in self.grids}
        self.ents = {g: Counter() for g in self.grids}
        self.ent_pos = {g: {} for g in self.grids}
        self.beacons = {g: [] for g in self.grids}
        self.names = {}
        rows = []
        for group in doc.get("entities") or []:
            proto = group.get("proto") or ""
            for e in group.get("entities") or []:
                comps = {c.get("type"): c for c in (e.get("components") or [])}
                rows.append((proto, int(e["uid"]), comps))
        parent = {}
        local = {}
        for proto, uid, comps in rows:
            xf = comps.get("Transform") or {}
            p = xf.get("parent")
            parent[uid] = int(p) if p not in (None, "invalid") else None
            local[uid] = (vec(xf.get("pos")), xf.get("rot", "0 rad"))
        for proto, uid, comps in rows:
            if uid in self.tiles:
                self._read_grid(uid, comps, tilemap)
                meta = comps.get("MetaData") or {}
                self.names[uid] = meta.get("name")
                continue
            if "DecalChunk" in comps:  # newer maps keep decals in chunk entities rooted on the grid
                root = (comps.get("ChunkEntity") or {}).get("root", parent.get(uid))
                if root is not None and int(root) in self.decals:
                    self._read_decals(int(root), (comps["DecalChunk"] or {}).get("decals") or {})
                continue
            if not proto:
                continue
            # Walk up to the entity sitting directly on a grid (contained items use their holder).
            cur, seen = uid, 0
            while parent.get(cur) is not None and parent[cur] not in self.tiles and seen < 32:
                cur = parent[cur]
                seen += 1
            grid = parent.get(cur)
            if grid not in self.tiles:
                continue
            pos = local[cur][0]
            tile = (math.floor(pos[0]), math.floor(pos[1]))
            body = {k: strip_uids(v) for k, v in comps.items() if k not in ("Transform",)}
            sig = (proto, round(pos[0], 2), round(pos[1], 2), str(local[uid][1]),
                   json.dumps(body, sort_keys=True, default=str))
            self.ents[grid][sig] += 1
            self.ent_pos[grid].setdefault(sig, tile)
            if proto.startswith("DefaultStationBeacon") or "WarpPoint" in comps or "NavMapBeacon" in comps:
                self.beacons[grid].append((tile, beacon_name(proto, comps)))

    def _read_grid(self, uid, comps, tilemap):
        chunks = (comps.get("MapGrid") or {}).get("chunks") or {}
        for key, ch in chunks.items():
            cx, cy = (int(v) for v in str(ch.get("ind", key)).split(","))
            raw = base64.b64decode(ch["tiles"])
            step = 7 if ch.get("version", 7) >= 7 else 6
            for i in range(len(raw) // step):
                tid = struct.unpack_from("<i", raw, i * step)[0]
                name = tilemap.get(tid, f"#{tid}")
                if name == "Space":
                    continue
                x, y = cx * 16 + i % 16, cy * 16 + i // 16
                self.tiles[uid][(x, y)] = name + ":" + raw[i * step + 4:i * step + step].hex()
        dg = comps.get("DecalGrid") or {}
        self._read_decals(uid, dg.get("chunkCollection") or dg.get("decals") or {})

    def _read_decals(self, uid, coll):
        for node in coll.get("nodes") or []:
            n = node.get("node") or {}
            for _, p in (node.get("decals") or {}).items():
                x, y = vec(p)
                sig = (n.get("id"), n.get("color"), str(n.get("angle")), round(x, 2), round(y, 2))
                self.decals[uid][sig] += 1

    def bounds(self, g):
        t = self.tiles.get(g) or {}
        if not t:
            return None
        xs = [p[0] for p in t]
        ys = [p[1] for p in t]
        return min(xs), min(ys), max(xs), max(ys)


def beacon_name(proto, comps):
    for key, field in (("NavMapBeacon", "text"), ("WarpPoint", "location")):
        v = (comps.get(key) or {}).get(field)
        if v:
            return str(v)
    meta = (comps.get("MetaData") or {}).get("name")
    if meta:
        return str(meta)
    name = proto.replace("DefaultStationBeacon", "") or proto
    return re.sub(r"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ", name)


def diff_grid(base, head, gb, gh):
    """Return changed tile coords plus counts."""
    pts = set()
    tb = base.tiles.get(gb, {}) if base else {}
    th = head.tiles.get(gh, {}) if head else {}
    tile_changes = 0
    for p in set(tb) | set(th):
        if tb.get(p) != th.get(p):
            pts.add(p)
            tile_changes += 1
    eb = base.ents.get(gb, Counter()) if base else Counter()
    eh = head.ents.get(gh, Counter()) if head else Counter()
    removed, added = eb - eh, eh - eb
    for sig in removed:
        pts.add(base.ent_pos[gb][sig])
    for sig in added:
        pts.add(head.ent_pos[gh][sig])
    db = base.decals.get(gb, Counter()) if base else Counter()
    dh = head.decals.get(gh, Counter()) if head else Counter()
    dchg = (db - dh) + (dh - db)
    for sig in dchg:
        pts.add((math.floor(sig[3]), math.floor(sig[4])))
    removed_protos = Counter(s[0] for s in removed.elements())
    added_protos = Counter(s[0] for s in added.elements())
    moved = removed_protos & added_protos  # same prototype gone in one place, new in another
    added_protos -= moved
    removed_protos -= moved
    return pts, {"tiles": tile_changes, "added": sum(added_protos.values()), "removed": sum(removed_protos.values()),
                 "moved": sum(moved.values()), "decals": sum(dchg.values()), "added_protos": added_protos,
                 "removed_protos": removed_protos, "moved_protos": moved}


def cluster(points):
    pts = sorted(points)
    parent = list(range(len(pts)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    index = {}
    for i, (x, y) in enumerate(pts):
        index.setdefault((x // CLUSTER_GAP, y // CLUSTER_GAP), []).append(i)
    for (bx, by), members in index.items():
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for j in index.get((bx + dx, by + dy), []):
                    for i in members:
                        a, b = pts[i], pts[j]
                        if max(abs(a[0] - b[0]), abs(a[1] - b[1])) <= CLUSTER_GAP:
                            ri, rj = find(i), find(j)
                            if ri != rj:
                                parent[ri] = rj
    groups = {}
    for i, p in enumerate(pts):
        groups.setdefault(find(i), []).append(p)
    boxes = []
    for g in groups.values():
        xs = [p[0] for p in g]
        ys = [p[1] for p in g]
        boxes.append([min(xs), min(ys), max(xs), max(ys), len(g), g])
    boxes.sort(key=lambda b: -b[4])
    if len(boxes) > MAX_AREAS:
        rest = boxes[MAX_AREAS - 1:]
        merged = [min(b[0] for b in rest), min(b[1] for b in rest), max(b[2] for b in rest),
                  max(b[3] for b in rest), sum(b[4] for b in rest), [p for b in rest for p in b[5]]]
        boxes = boxes[:MAX_AREAS - 1] + [merged]
    return boxes


def area_names(box, beacons):
    if not beacons:
        return []
    cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
    inside = [n for (x, y), n in beacons if box[0] - PAD <= x <= box[2] + PAD and box[1] - PAD <= y <= box[3] + PAD]
    if inside:
        return sorted(set(inside))[:3]
    (x, y), n = min(beacons, key=lambda b: (b[0][0] - cx) ** 2 + (b[0][1] - cy) ** 2)
    return [f"near {n}"]


def find_render(render_dir, stem, bounds):
    """Pick the grid image whose size matches the grid's tile bounds."""
    if bounds is None:
        return None
    w = (bounds[2] - bounds[0] + 1) * PX
    h = (bounds[3] - bounds[1] + 1) * PX
    files = sorted(glob.glob(os.path.join(render_dir, stem, f"{stem}-*.png")))
    for f in files:
        with Image.open(f) as im:
            if im.size == (w, h):
                return f
    return files[0] if len(files) == 1 else None


def crop(img_path, bounds, box):
    """Crop tile rect box=(x0,y0,x1,y1) from a render whose grid tile bounds are bounds."""
    x0, y0, x1, y1 = box
    w, h = x1 - x0 + 1, y1 - y0 + 1
    canvas = Image.new("RGBA", (w * PX, h * PX), (12, 12, 20, 255))
    if img_path and bounds:
        with Image.open(img_path) as im:
            im = im.convert("RGBA")
            left = (x0 - bounds[0]) * PX
            top = (bounds[3] - y1) * PX
            part = im.crop((left, top, left + w * PX, top + h * PX))
            canvas.alpha_composite(part)
    return canvas


def outline(img, rect, area):
    """Draw one box around all of an area's changes (area = x0, y0, x1, y1 in tiles)."""
    d = ImageDraw.Draw(img)
    cx0, _, _, cy1 = rect
    m = 3
    left = (area[0] - cx0) * PX - m
    top = (cy1 - area[3]) * PX - m
    right = (area[2] - cx0 + 1) * PX + m - 1
    bottom = (cy1 - area[1] + 1) * PX + m - 1
    d.rectangle([left - 2, top - 2, right + 2, bottom + 2], outline=(0, 0, 0, 200), width=2)
    d.rectangle([left, top, right, bottom], outline=(255, 60, 60, 255), width=3)
    return img


def font(size):
    try:
        return ImageFont.load_default(size=size)
    except TypeError:  # Pillow < 10.1
        return ImageFont.load_default()


def fit(img, limit=MAX_PANEL_PX):
    s = min(1.0, limit / max(img.size))
    if s < 1.0:
        img = img.resize((max(1, int(img.width * s)), max(1, int(img.height * s))), Image.LANCZOS)
    return img


def panel(before, after):
    imgs = [fit(before), fit(after)]
    labels = ["Before", "After"]
    gap, head = 12, 40
    W = sum(i.width for i in imgs) + gap * (len(imgs) + 1)
    H = max(i.height for i in imgs) + head + gap
    out = Image.new("RGBA", (W, H), (30, 30, 40, 255))
    d = ImageDraw.Draw(out)
    f = font(24)
    x = gap
    for im, lab in zip(imgs, labels):
        d.text((x, 8), lab, fill=(240, 240, 240, 255), font=f)
        out.alpha_composite(im, (x, head))
        x += im.width + gap
    return out


def overview(img_path, bounds, boxes):
    if not img_path:
        return None
    with Image.open(img_path) as im:
        im = im.convert("RGBA")
    d = ImageDraw.Draw(im)
    scale = min(1.0, 1400 / max(im.size))
    line = max(4, int(4 / scale))          # about 4 px wide after downscaling
    f = font(max(20, int(28 / scale)))
    for i, b in enumerate(boxes, 1):
        x0, y0, x1, y1 = b[0] - 3, b[1] - 3, b[2] + 3, b[3] + 3
        r = [(x0 - bounds[0]) * PX, (bounds[3] - y1) * PX, (x1 - bounds[0] + 1) * PX, (bounds[3] - y0 + 1) * PX]
        d.rectangle(r, outline=(255, 60, 60, 255), width=line)
        d.text((r[2] + line, r[1]), str(i), fill=(255, 60, 60, 255), font=f, stroke_width=max(2, line // 2),
               stroke_fill=(0, 0, 0, 255))
    return fit(im, 1400)


def game_maps(repo):
    """mapPath (/Maps/...) -> (id, mapName)."""
    out = {}
    for f in glob.glob(os.path.join(repo, "Resources", "Prototypes", "**", "*.yml"), recursive=True):
        try:
            with open(f, encoding="utf-8") as fh:
                text = fh.read()
            if "gameMap" not in text:
                continue
            docs = yaml.load(text, Loader=TolerantLoader) or []
        except Exception:
            continue
        for d in docs if isinstance(docs, list) else []:
            if isinstance(d, dict) and d.get("type") == "gameMap" and d.get("mapPath"):
                out[str(d["mapPath"])] = (d.get("id"), d.get("mapName"))
    return out


def safe(s):
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", s)


def summarize(counter, n=6):
    items = counter.most_common(n)
    more = sum(counter.values()) - sum(c for _, c in items)
    s = ", ".join(f"`{p}`" + (f" x{c}" if c > 1 else "") for p, c in items)
    return s + (f", +{more} more" if more > 0 else "")


def process(args, status, path, gmaps, lines):
    stem = os.path.splitext(os.path.basename(path))[0]
    head_path = os.path.join(args.repo, path)
    base_path = os.path.join(args.base_src, path)
    base = MapData(base_path) if status != "A" and os.path.exists(base_path) else None
    head = MapData(head_path) if status != "D" and os.path.exists(head_path) else None
    res_path = "/" + path.split("Resources/", 1)[-1]
    gid, gname = gmaps.get(res_path, (None, None))
    title = f"{gname} (`{gid}`)" if gid else f"`{stem}`"
    lines.append(f"### {title}")
    lines.append(f"`{path}` - " + {"A": "new map", "D": "deleted map", "M": "modified"}.get(status, status))
    lines.append("")
    if head is None:
        lines.append("This map file was removed.\n")
        return
    bgrids = base.grids if base else []
    for gi, gh in enumerate(head.grids):
        gb = bgrids[gi] if gi < len(bgrids) else None
        hb = head.bounds(gh)
        bb = base.bounds(gb) if base and gb is not None else None
        himg = find_render(args.head_renders, stem, hb)
        bimg = find_render(args.base_renders, stem, bb) if bb else None
        pts, stats = diff_grid(base if gb is not None else None, head, gb, gh)
        gname_ = head.names.get(gh) or ("Main grid" if gi == 0 else f"Grid {gi}")
        if len(head.grids) > 1:
            lines.append(f"#### {gname_}")
        if not pts:
            lines.append("No visible changes on this grid.\n")
            continue
        lines.append(f"Tiles changed: {stats['tiles']} | entities added: {stats['added']}, removed: "
                     f"{stats['removed']}, moved or edited: {stats['moved']} | decals changed: {stats['decals']}")
        if stats["added_protos"]:
            lines.append(f"- Added: {summarize(stats['added_protos'])}")
        if stats["removed_protos"]:
            lines.append(f"- Removed: {summarize(stats['removed_protos'])}")
        if stats["moved_protos"]:
            lines.append(f"- Moved or edited: {summarize(stats['moved_protos'])}")
        lines.append("")
        if himg is None:
            lines.append("Render missing for this grid, see the workflow log.\n")
            continue
        boxes = cluster(pts) if base is not None else [[*hb, len(pts), list(pts)]]
        if base is None:
            ov = overview(himg, hb, [])
            name = f"{safe(stem)}-{gi}-new.png"
            ov.save(os.path.join(args.out, "images", name))
            lines.append(f"![{stem} new]({{{{IMAGE_BASE}}}}/{name})\n")
            continue
        ov = overview(himg, hb, boxes)
        if ov is not None and len(boxes) > 1:
            name = f"{safe(stem)}-{gi}-overview.png"
            ov.save(os.path.join(args.out, "images", name))
            lines.append("<details><summary>Overview of changed areas</summary>\n")
            lines.append(f"![{stem} overview]({{{{IMAGE_BASE}}}}/{name})\n")
            lines.append("</details>\n")
        beacons = head.beacons.get(gh, []) + (base.beacons.get(gb, []) if gb is not None else [])
        for ai, b in enumerate(boxes, 1):
            x0, y0, x1, y1 = b[0] - PAD, b[1] - PAD, b[2] + PAD, b[3] + PAD
            if x1 - x0 + 1 < MIN_SPAN:
                grow = MIN_SPAN - (x1 - x0 + 1)
                x0 -= grow // 2
                x1 += grow - grow // 2
            if y1 - y0 + 1 < MIN_SPAN:
                grow = MIN_SPAN - (y1 - y0 + 1)
                y0 -= grow // 2
                y1 += grow - grow // 2
            rect = (x0, y0, x1, y1)
            before = crop(bimg, bb, rect)
            after = crop(himg, hb, rect)
            outline(before, rect, b)
            outline(after, rect, b)
            name = f"{safe(stem)}-{gi}-area{ai}.png"
            panel(before, after).save(os.path.join(args.out, "images", name))
            where = ", ".join(area_names(b, beacons)) or "unnamed area"
            lines.append(f"**Area {ai}: {where}** - tiles ({b[0]}, {b[1]}) to ({b[2]}, {b[3]}), {b[4]} changed tiles")
            lines.append("")
            lines.append(f"![{stem} area {ai}]({{{{IMAGE_BASE}}}}/{name})")
            lines.append("")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", default=".")
    ap.add_argument("--changes", required=True)
    ap.add_argument("--base-src", required=True)
    ap.add_argument("--base-renders", required=True)
    ap.add_argument("--head-renders", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()
    os.makedirs(os.path.join(args.out, "images"), exist_ok=True)
    gmaps = game_maps(args.repo)
    lines = ["<!-- Map Diff Bot -->", "## Map Diff Bot", ""]
    count = 0
    with open(args.changes, encoding="utf-8") as f:
        for raw in f:
            if not raw.strip():
                continue
            status, path = raw.rstrip("\n").split("\t", 1)
            status = status[0]
            if not (path.startswith("Resources/Maps/") and path.endswith(".yml")):
                continue
            try:
                process(args, status, path, gmaps, lines)
            except Exception as ex:  # keep going for the other maps
                lines.append(f"Could not diff `{path}`: {type(ex).__name__}: {ex}\n")
            count += 1
    if count == 0:
        lines.append("No map files changed.")
    lines.append("The red box in each area surrounds everything that changed there (tiles, entities and decals). Rendered with Content.MapRenderer.")
    with open(os.path.join(args.out, "comment.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print("\n".join(lines))
    return 0


if __name__ == "__main__":
    sys.exit(main())
