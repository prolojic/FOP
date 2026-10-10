# -*- coding: utf-8 -*-
"""Валидация ФОП-файлов.

Проверяет:
  1. Корректность формата (META/GROUP/PARAM, обязательные поля).
  2. Уникальность GUID и имён внутри файла.
  3. Ссылки параметров на существующие группы.
  4. Соответствие производных ФОПов центральному (параметр производного
     должен существовать в центральном с тем же GUID, типом и именем).

Запуск:
  python check_fops.py                      — проверить всё
  python check_fops.py путь\к\файлу.txt     — проверить один файл
"""
import sys, io, os, glob

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
BASE = os.path.dirname(os.path.abspath(__file__))

FIELDS = ["GUID", "NAME", "DATATYPE", "DATACATEGORY", "GROUP", "VISIBLE",
          "DESCRIPTION", "USERMODIFIABLE", "HIDEWHENNOVALUE"]

# Файлы, которые ведутся отдельно от центрального ФОПа —
# сверка с центральным для них не выполняется.
STANDALONE = {"FOP_HospitalTechnic.txt"}


def parse(path):
    errors = []
    with open(path, "r", encoding="utf-16") as f:
        lines = f.read().splitlines()
    meta = None
    groups = {}
    params = []
    for i, line in enumerate(lines, 1):
        if not line.strip() or line.startswith("#") or line.startswith("*"):
            continue
        parts = line.split("\t")
        if parts[0] == "META":
            meta = parts[1:3]
        elif parts[0] == "GROUP":
            if len(parts) < 3:
                errors.append(f"строка {i}: GROUP без ID/имени")
            else:
                groups[int(parts[1])] = parts[2]
        elif parts[0] == "PARAM":
            if len(parts) < 10:
                errors.append(f"строка {i}: PARAM имеет {len(parts)-1} полей вместо 9")
                continue
            params.append(dict(zip(FIELDS, parts[1:]), _line=i))
        else:
            errors.append(f"строка {i}: неизвестная запись {parts[0]!r}")
    return meta, groups, params, errors


def validate(path, central_index=None, strict_duplicates=True):
    """strict_duplicates=False — дубли имён считаются предупреждениями
    (для файлов, ведущихся отдельно от центрального)."""
    meta, groups, params, errors = parse(path)
    warnings = []
    if meta is None:
        errors.append("нет секции META")
    seen_guid, seen_name = {}, {}
    for p in params:
        if p["GUID"] in seen_guid:
            errors.append(f"строка {p['_line']}: дубль GUID {p['GUID']} "
                          f"(уже на строке {seen_guid[p['GUID']]})")
        seen_guid[p["GUID"]] = p["_line"]
        if p["NAME"] in seen_name:
            msg = (f"строка {p['_line']}: дубль имени {p['NAME']!r} "
                   f"(уже на строке {seen_name[p['NAME']]}, другой GUID — "
                   f"в Revit это два разных параметра!)")
            (errors if strict_duplicates else warnings).append(msg)
        seen_name[p["NAME"]] = p["_line"]
        try:
            gid = int(p["GROUP"])
            if gid not in groups:
                errors.append(f"строка {p['_line']}: параметр ссылается на "
                              f"несуществующую группу {gid}")
        except ValueError:
            errors.append(f"строка {p['_line']}: GROUP не число: {p['GROUP']!r}")
        if central_index is not None:
            ref = central_index.get(p["GUID"])
            if ref is None:
                errors.append(f"строка {p['_line']}: {p['NAME']!r} нет в центральном ФОПе")
            elif (ref["NAME"], ref["DATATYPE"]) != (p["NAME"], p["DATATYPE"]):
                errors.append(f"строка {p['_line']}: {p['NAME']!r} расходится с "
                              f"центральным ФОПом по имени/типу")
    for e in errors:
        print(f"  ❌ {os.path.basename(path)}: {e}")
    for w in warnings:
        print(f"  ⚠️  {os.path.basename(path)}: {w}")
    if not errors:
        suffix = f" — ошибок нет" + (f", предупреждений: {len(warnings)}" if warnings else "")
        print(f"  ✅ {os.path.basename(path)}: {len(params)} параметров, "
              f"{len(groups)} групп{suffix}")
    return not errors


def main():
    central = os.path.join(BASE, "SharedParameters.txt")
    c_meta, c_groups, c_params, c_err = parse(central)
    central_index = {p["GUID"]: p for p in c_params}
    all_ok = validate(central)
    for e in c_err:
        all_ok = False
        print(f"  ❌ центральный: {e}")
    targets = sys.argv[1:] or sorted(glob.glob(os.path.join(BASE, "fops", "*.txt")))
    for t in targets:
        if os.path.abspath(t) == os.path.abspath(central):
            continue
        idx = None if os.path.basename(t) in STANDALONE else central_index
        strict = os.path.basename(t) not in STANDALONE
        if not validate(t, idx, strict_duplicates=strict):
            all_ok = False
    sys.exit(0 if all_ok else 1)


if __name__ == "__main__":
    main()
