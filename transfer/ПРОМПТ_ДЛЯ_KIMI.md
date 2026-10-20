# Промпт для Kimi (продолжение работы с ФОП на новой машине)

Вставь этот текст в новый чат Kimi Work на машине, куда переносится тема.

---

Мы продолжаем тему «ФОП — общие параметры Revit». Репозиторий уже склонирован локально, работай в нём.

## Контекст

- Репозиторий: github.com/prolojic/FOP, ветка `main` (единственный источник истины — файл `SharedParameters.txt`).
- ФОП = файлы общих параметров Revit, хранятся в **UTF-16**, вручную не править — только через Revit (Управление → Общие параметры → Изменить).
- Производные фирменные ФОПы в `fops/*.txt` не править руками — только генератором `FopTools build`.
- Полный регламент — в `README.md` репозитория.

## Утилита FopTools (C#, .NET 8)

Исходники в `FopTools/`, команды запускаются из корня репозитория:

```bash
dotnet run --project FopTools -- check          # валидация всех ФОПов в fops/
dotnet run --project FopTools -- check <файл>   # один файл
dotnet run --project FopTools -- build          # пересборка производных ФОПов
```

`check` проверяет: формат файла, дубли GUID и имён, ссылки на группы, соответствие производных ФОПов центральному (по GUID, имени, типу). Код выхода 0 — ошибок нет, 1 — есть ошибки.

Требуется .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
Если SDK нет, можно собрать один exe: `dotnet publish FopTools -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish`.

## Текущее состояние (на 20.10.2026)

- Центральный ФОП: `SharedParameters.txt` (слит из FOP_SharedParameters_latest + revit_SharedParameters + HT_FOP).
- Версии ФОПа Upside: `Upsite_v141026.txt` (базовая, 178 параметров) → `Upsite_v201026.txt` (заполнены описания всех 178 параметров; 18 назначений подтверждены инструкциями шаблона ОВ2: CreateSystem, AirExchange, SysName, PipeUtil, VentShem, OPN, HoleElevation).
- Каталог параметров с назначениями: `docs/Каталог_параметров_ФОП_Upside.pdf`.
- Производные ФОПы: `FOP_ADSK.txt` ✅ (префикс `ADSK_`, 4 параметра), `FOP_HospitalTechnic.txt` ✅ (standalone, ведётся отдельно); `FOP_Image.txt`, `FOP_StandartProekt.txt`, `FOP_Upside.txt` — заглушки по группе «01 Обязательные ОБЩИЕ», критерии отбора уточняются; `FOP_Expertiza.txt` — пусто, группы «!! Экспертиза*» в центральном ФОПе пока нет.

## Что делать дальше

1. Проверь окружение: `git status`, `dotnet --list-sdks`.
2. Запусти `dotnet run --project FopTools -- check` — покажи результат, ошибок быть не должно.
3. Дальше по регламенту: уточнение критериев отбора в `fop_rules.json` для Image / Стандарт Проект / Upside / Экспертиза (нужно решение BIM-координатора).
4. Любые правки параметров — только через Revit, затем `check` → `build` → коммит → push, с оповещением команды.

## Правила работы

- Перед работой `git pull`, после — `git push` и оповещение команды.
- GUID параметров после создания не меняются никогда.
- Локальные копии на сетевых дисках не использовать.
