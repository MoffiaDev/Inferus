#!/usr/bin/env python3
# Inferus - prepares inputs for map_diff.py on any OS (lists changed maps, extracts base copies, renders both sides).
"""Usage, from the repo root:

  python Tools/_Inferus/map_diff_prep.py [--base origin/master] [--work ../mapdiff] [--no-render]

Then:

  python Tools/_Inferus/map_diff.py --repo . --changes ../mapdiff/changes.tsv --base-src ../mapdiff/src/base
      --base-renders ../mapdiff/render/base --head-renders ../mapdiff/render/head --out ../mapdiff/out

Compares the working tree's committed HEAD against the merge base with --base. Commit (or stash) your map
changes first, or pass --uncommitted to diff the working tree instead of HEAD.
"""
import argparse
import os
import subprocess
import sys


def git(*args, binary=False):
    out = subprocess.run(["git", *args], check=True, capture_output=True)
    return out.stdout if binary else out.stdout.decode("utf-8").strip()


def render(files, out_dir, label):
    if not files:
        return
    os.makedirs(out_dir, exist_ok=True)
    env = dict(os.environ, DOTNET_PROCESSOR_COUNT="1")
    cmd = ["dotnet", "run", "--project", "Content.MapRenderer", "--configuration", "Release", "--",
           "--format", "png", "-o", out_dir, "-f", *files]
    print(f"Rendering {label}: {len(files)} map(s)")
    if subprocess.run(cmd, env=env).returncode != 0:
        print(f"warning: the map renderer reported an error for the {label} maps", file=sys.stderr)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="origin/master", help="branch or commit to compare against")
    ap.add_argument("--work", default=os.path.join("..", "mapdiff"))
    ap.add_argument("--uncommitted", action="store_true", help="diff the working tree instead of HEAD")
    ap.add_argument("--no-render", action="store_true")
    args = ap.parse_args()

    work = os.path.abspath(args.work)
    base_src = os.path.join(work, "src", "base")
    os.makedirs(base_src, exist_ok=True)
    mb = git("merge-base", args.base, "HEAD")
    target = [] if args.uncommitted else ["HEAD"]
    raw = git("diff", "--name-status", "--no-renames", mb, *target, "--", "Resources/Maps/**.yml")
    rows = [line.split("\t", 1) for line in raw.splitlines() if "\t" in line]
    if not rows:
        print(f"No map changes between {args.base} (merge base {mb[:8]}) and "
              f"{'the working tree' if args.uncommitted else 'HEAD'}.")
        return 1
    with open(os.path.join(work, "changes.tsv"), "w", encoding="utf-8", newline="\n") as f:
        for status, path in rows:
            f.write(f"{status[0]}\t{path}\n")

    base_files, head_files = [], []
    for status, path in rows:
        print(f"{status[0]}  {path}")
        if status[0] != "A":
            dest = os.path.join(base_src, *path.split("/"))
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with open(dest, "wb") as f:
                f.write(git("show", f"{mb}:{path}", binary=True))
            base_files.append(dest)
        if status[0] != "D":
            head_files.append(os.path.abspath(path))

    if not args.no_render:
        render(base_files, os.path.join(work, "render", "base"), "base")
        render(head_files, os.path.join(work, "render", "head"), "head")
    print(f"\nInputs ready in {work}. Now run map_diff.py (see the docstring).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
