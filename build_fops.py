# -*- coding: utf-8 -*-
"""Генератор фирменных ФОПов из центрального SharedParameters.txt.

Центральный файл — единственный источник истины. Производные файлы в fops/
пересобираются этим скриптом по правилам fop_rules.yaml. Руками их не править.
"""
import sys, io, os, re

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE = os.path.dirname(os.path.abspath(__file__))

# --- Правила распределения (пока без внешнего yaml-файла, чтобы не тянуть зависимости) ---
# Каждое правило: список условий. Параметр попадает в ФОП, если выполнено ХОТЯ БЫ одно условие.
# Условие: {"group_prefix": "..."} — имя группы начинается с префикса,
#          {"group_exact": "..."}  — точное имя группы,
#          {"name_prefix": "..."}  — имя параметра начинается с префикса.
RULES = {
    "FOP_ADSK.txt": [
        {"name_prefix": "ADSK_"},
    ],
    # Image, StandartProekt, Upside: критерии пока не определены — оставлены как заготовки.
    # Правило "центральный минус чужие префиксы" активируем, когда подтвердите состав.
    "FOP_Image.txt": [
        {"group_exact": "01 Обязательные ОБЩИЕ"},
    ],
    "FOP_StandartProekt.txt": [
        {"group_exact": "01 Обязательные ОБЩИЕ"},
    ],
    "FOP_Upside.txt": [
        {"group_exact": "01 Обязательные ОБЩИЕ"},
    ],
    # В центральном ФОПе группы "!! Экспертиза СПб" нет — файл будет собран пустой,
    # параметры экспертизы пока живут в HT_FOP.txt.
    "FOP_Expertiza.txt": [
        {"group_prefix": "!! Экспертиза"},
    ],
}

HEADER = (
    "# This is a Revit shared parameter file.\r\n"
    "# Do not edit manually.\r\n"
)


def parse_central(path):
    with open(path, "r", encoding="utf-16") as f:
        lines = f.read().splitlines()
    meta = None
    groups = {}   # id -> name
    params = []   # список dict (порядок важен)
    for line in lines:
        if not line.strip() or line.startswith("#"):
            continue
        parts = line.split("\t")
        if parts[0] == "META":
            meta = (parts[1], parts[2])
        elif parts[0] == "GROUP":
            groups[int(parts[1])] = parts[2]
        elif parts[0] == "PARAM":
            params.append({
                "guid": parts[1],
                "name": parts[2],
                "datatype": parts[3],
                "datacategory": parts[4] if len(parts) > 4 else "",
                "group": int(parts[5]) if len(parts) > 5 and parts[5] else 0,
                "visible": parts[6] if len(parts) > 6 else "",
                "description": parts[7] if len(parts) > 7 else "",
                "usermodifiable": parts[8] if len(parts) > 8 else "",
                "hidewhennovalue": parts[9] if len(parts) > 9 else "",
                "raw_fields": parts[1:],  # чтобы не потерять поля при пересборке
            })
    return meta, groups, params


def match(p, groups, cond):
    gname = groups.get(p["group"], "")
    if "group_prefix" in cond:
        return gname.startswith(cond["group_prefix"])
    if "group_exact" in cond:
        return gname == cond["group_exact"]
    if "name_prefix" in cond:
        return p["name"].startswith(cond["name_prefix"])
    return False


def build(meta, groups, params, conds):
    selected = [p for p in params if any(match(p, groups, c) for c in conds)]
    # перенумеруем группы под выборку, сохранив порядок следования
    used_ids, new_groups, mapping = [], [], {}
    for p in selected:
        if p["group"] not in mapping:
            mapping[p["group"]] = len(used_ids) + 1
            used_ids.append(p["group"])
    lines = [HEADER.rstrip("\r\n"), "*META\tVERSION\tMINVERSION",
             f"META\t{meta[0]}\t{meta[1]}", "*GROUP\tID\tNAME"]
    for old_id in used_ids:
        lines.append(f"GROUP\t{mapping[old_id]}\t{groups[old_id]}")
    lines.append("*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\tHIDEWHENNOVALUE")
    for p in selected:
        fields = list(p["raw_fields"])
        fields[4] = str(mapping[p["group"]])  # GROUP
        lines.append("PARAM\t" + "\t".join(fields))
    return "\r\n".join(lines) + "\r\n", len(selected)


def main():
    central = os.path.join(BASE, "SharedParameters.txt")
    outdir = os.path.join(BASE, "fops")
    os.makedirs(outdir, exist_ok=True)
    meta, groups, params = parse_central(central)
    print(f"Центральный ФОП: {len(params)} параметров, {len(groups)} групп\n")
    for fname, conds in RULES.items():
        text, n = build(meta, groups, params, conds)
        with open(os.path.join(outdir, fname), "w", encoding="utf-16") as f:
            f.write(text)
        print(f"  fops/{fname}: {n} параметров")


if __name__ == "__main__":
    main()
