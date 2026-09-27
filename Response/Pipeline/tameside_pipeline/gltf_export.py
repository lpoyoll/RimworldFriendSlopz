"""Export a zone as glTF (.glb) files for a first look in Unreal (drag into the Content Browser).

Coordinates are metres relative to the world origin (config `origin`, height 0 = ODN), in glTF's right-handed Y-up frame:
x = east, y = up, z = south. Placed at UE (0,0,0) these line up with docs/06 (UE X east, Y south, Z up).
Colours are grouped into a small palette of materials (glTF baseColorFactor), which every importer honours.
This is a placeholder view of the pipeline output; the real importer builds Landscape + Nanite kits.
"""
from __future__ import annotations

import math
from pathlib import Path

import numpy as np

QUANT = 12  # colour rounding step (0-255) when grouping faces into materials


def _soup_to_scene(mesh_dict: dict, name: str, double_sided: bool = True):
    import trimesh
    from trimesh.visual.material import PBRMaterial

    pos = np.asarray(mesh_dict["pos"], dtype=np.float32).reshape(-1, 3)
    col = np.asarray(mesh_dict["col"], dtype=np.float32).reshape(-1, 3)
    if not len(pos):
        return None
    faces_col = (np.clip(col[0::3], 0, 1) * 255 / QUANT).round().astype(int) * QUANT
    keys, inv = np.unique(faces_col, axis=0, return_inverse=True)
    scene = trimesh.Scene()
    for k, rgb in enumerate(keys):
        tri = np.nonzero(inv.ravel() == k)[0]
        v = pos.reshape(-1, 3, 3)[tri].reshape(-1, 3)
        f = np.arange(len(v)).reshape(-1, 3)
        m = trimesh.Trimesh(vertices=v, faces=f, process=True)  # process merges duplicate vertices
        mat = PBRMaterial(name=f"{name}_{rgb[0]:02x}{rgb[1]:02x}{rgb[2]:02x}", baseColorFactor=[*(np.clip(rgb, 0, 255) / 255).tolist(), 1.0],
                          metallicFactor=0.0, roughnessFactor=0.85, doubleSided=double_sided)
        m.visual = trimesh.visual.TextureVisuals(material=mat)
        scene.add_geometry(m, geom_name=f"{name}_{k}")
    return scene


def _terrain_soup(t: dict) -> dict:
    nx, nz = t["nx"], t["nz"]
    h = np.asarray(t["h"], dtype=np.float32).reshape(nz, nx)
    xs = t["x0"] + np.arange(nx) * t["step"]
    zs = t["z0"] + np.arange(nz) * t["step"]
    X, Z = np.meshgrid(xs, zs)
    V = np.stack([X, h, Z], axis=-1)
    a, b, c, d = V[:-1, :-1], V[:-1, 1:], V[1:, :-1], V[1:, 1:]
    tris = np.concatenate([np.stack([a, c, b], axis=2), np.stack([b, c, d], axis=2)], axis=0).reshape(-1, 3)
    grass = np.tile(np.array([0.33, 0.45, 0.26], dtype=np.float32), (len(tris), 1))
    return {"pos": tris.ravel().tolist(), "col": grass.ravel().tolist()}


def _trees_soup(trees: list) -> dict:
    import trimesh

    ico = trimesh.creation.icosphere(subdivisions=1)
    cyl = trimesh.creation.cylinder(radius=0.22, height=1.0, sections=6)
    pos, col = [], []
    for x, y, z, h in trees:
        r = min(max(h * 0.3, 1.4), 7.5)
        cy = y + h - r * 0.9
        v = ico.vertices * np.array([r, r * 1.15, r]) + np.array([x, cy, z])
        pos.append(v[ico.faces].reshape(-1, 3)); col.append(np.tile([0.25, 0.34, 0.18], (len(ico.faces) * 3, 1)))
        th = max(cy - y, 1.0)
        # trimesh cylinders are along Z; turn to Y-up
        cv = cyl.vertices[:, [0, 2, 1]] * np.array([1 + h / 25, th, 1 + h / 25]) + np.array([x, y + th / 2, z])
        pos.append(cv[cyl.faces].reshape(-1, 3)); col.append(np.tile([0.29, 0.23, 0.17], (len(cyl.faces) * 3, 1)))
    if not pos:
        return {"pos": [], "col": []}
    return {"pos": np.concatenate(pos).ravel().tolist(), "col": np.concatenate(col).ravel().tolist()}


def export_zone(scene: dict, out_dir: Path) -> dict[str, int]:
    """Write terrain.glb, buildings.glb, roads.glb, trees.glb. Returns triangle counts."""
    out_dir.mkdir(parents=True, exist_ok=True)
    layers = {
        "terrain": _terrain_soup(scene["terrain"]),
        "buildings": {"pos": scene["walls"]["pos"] + scene["roofs"]["pos"] + scene.get("openings", {"pos": []})["pos"],
                      "col": scene["walls"]["col"] + scene["roofs"]["col"] + scene.get("openings", {"col": []})["col"]},
        "roads": {"pos": scene["roads"]["pos"] + scene["pavements"]["pos"], "col": scene["roads"]["col"] + scene["pavements"]["col"]},
        "trees": _trees_soup(scene["trees"]),
    }
    counts = {}
    for name, soup in layers.items():
        sc = _soup_to_scene(soup, name)
        if sc is None:
            continue
        sc.export(out_dir / f"{name}.glb")
        counts[name] = len(soup["pos"]) // 9
    return counts
