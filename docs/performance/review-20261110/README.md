# Повторное ревью O3 — 10.11.2026

Проверены весь пакет `4ill.ru/plans/cs2-plugin-optimization` на `8511370a`,
исторические основания 20/28 октября, upstream `5d2892a`, основная оптимизация
`606e8ff`/`1bfa88b` и PR #1 на `704ed92`. Исторический архив повторно прошёл
проверку: 9 профилей, 154758 calls, max 4.790674 мс. Исследованы все исходники
core/API, config, решение, тестовый потребитель и отчёты. `source-lock` сохраняет
исследовательскую базу; точная цепочка сборки старой установленной DLL остаётся
`provisional`.

## Покрытие O3

1. Список открытых меню и пустой путь сохранены. Общего GetPlayers в OnTick нет.
2. Кнопки — enum; порядок приоритетов и HasFlag для пользовательских масок,
   press/hold/repress сохранены. Ввод обрабатывается каждый tick.
3. Кэш HTML инвалидируется по title, содержимому/identity/Disabled видимых
   пунктов, count/order, selection/page, MenuLinesCount и локализованным строкам.
   Внеэкранные изменения попадут в текст при переходе страницы. Прямое изменение
   callbacks применяется в OnSelect без HTML rebuild. Кэш ограничен видимой
   страницей и временем жизни открытого меню. Общая ревизия API не требуется.
4. HTML отправляется каждый tick с прежним duration=1, без переноса работы в
   timer/worker. Реальный ColorText вызывается только при пересборке.
5. Сохранена защита от replacement/clear в OnSelect и BackAction. При ревью
   дополнены lifecycle/movement guards и проверки production callbacks.

## Найдено и исправлено

- После config unlock `MoveWhileOpenMenu=true` прежняя блокировка могла остаться
  навсегда: tick переставал писать modifier, а Clear/close пропускали restoration.
  Новая regression suite упала на исходном PR с `config unlock restores movement`.
  Теперь PlayerInfo хранит факт владения lock, восстанавливает его однократно,
  а освобождённый lock больше не перезаписывает скорость после внешних изменений.
- Native pointer не доказывал identity pawn/controller. Теперь serial включён
  в проверку подключения и pawn. Новый pawn получает собственный snapshot/lock,
  а чужой modifier и меню не наследуются при переиспользовании адреса. Замена
  меню освобождает прежний lock до захвата нового. После callback controller
  повторно проверяется перед native output. Map end/start очищают меню и кэши.
- Старые 21 проверки подменяли Control и ColorText, не проверяли цикл/callbacks.
  Добавлена linked-source Runtime suite: реальный Main/Control/PlayerInfo,
  ButtonMenu/ButtonsConfig/Misc/MenuInstance/CMenuApi/IMenuApi, managed граница CSS.
  121 проверка включает 0/1/8/32 меню, ввод, reentrancy, динамический список,
  movement/config/pawn/controller reuse, disconnect/map/unload и типы меню.
- В PR отсутствовали доступные raw профили и независимый пересчёт. Сохранены
  12 оригинальных Speedscope в архиве, SHA256, минимальные window metadata,
  исходная analysis и строгий verifier. SteamID, конфиги, пароли и console logs
  не публикуются. Raw Speedscope не изменены; metadata выбраны по allowlist.

Версия исправленного кода — **1.4.1-perf.3**. Public API/config/dependencies,
авторство/лицензионные файлы и существующие игровые потребители не менялись.

## Воспроизводимые проверки

SDK 10.0.400, net8.0, CounterStrikeSharp.API 1.0.305. Исходная solution Release
собралась с 37 прежними warnings и 0 errors. Кандидат при полном Rebuild имеет
34 warnings и 0 errors; nullable/unused warnings остаются, обновление зависимостей
не входит в O3.

```sh
dotnet build MenuManager.sln -c Release
dotnet run --project tests/O3/O3.csproj -c Release
dotnet run --project tests/Runtime/Runtime.csproj -c Release
python tests/verify_measurements.py
git -c core.whitespace=cr-at-eol diff --check
```

21 прежняя и 121 production-runtime проверки прошли. Добавлен CI с теми же
build/test/verifier. Managed doubles не доказывают ABI/порядок native callbacks,
клиентский rendering и интеграцию с реальными VIP/SimpleAdmin/Emotes/Metamod.
Маршрутизация Chat/Console/Center/Button и fallback отсутствующего Metamod
проверены offline; native MenusMM в этом патче не менялся.

Одинаковый synthetic harness upstream/candidate использует реальный OnPluginTick
и ColorText: 32 warmup, 3 x 128 ticks для 0/1/8/32 меню, static/dynamic каждые
64 ticks, tiered JIT выключен. Во всех 24 парах окон совпали sends и итоговый HTML.
При 32 неизменных меню median allocated bytes/tick: 254217.625 → 0; при dynamic:
252317.625 → 3890. Это синтетические native boundary doubles; дробные величины —
средние по окну. Не сравнивать с прежним упрощённым renderer harness и не выдавать
за native/full-tick результат. Raw и source SHA — `synthetic-*.json`.

## Перепроверенный live perf.2, один клиент

Эти данные получены до настоящего ревью: #6, CSS 1.0.378, CSSProfiler 1.0.0,
один реальный CS2 клиент/одно ButtonMenu, 0 bots, static/dynamic 1 Hz. Порядок
версий old,new,new,old,old,new; 3 x 30с на каждую версию/сценарий, 12 окон,
23028 вызовов. Все raw SHA, balanced O/C, 1919 calls/window, 30с, window и pooled
mean/p50/p95/p99/max/total совпали с исходной analysis. Percentiles — линейная
интерполяция raw calls, без усреднения перцентилей.

| Mean/p95/p99 OnTick, мс | 1.4.1 → perf.2 |
|---|---|
| Неизменное меню | 2.591349/3.859733/4.417740 → 0.175126/0.238008/0.255629 |
| Изменение label 1 Hz | 2.541225/3.434560/4.421681 → 0.212114/0.237102/2.580856 |

Медианы трёх парных снижений mean: 93.26% static и 91.65% dynamic; диапазоны
92.93–93.53% и 91.16–92.15%. P99 rebuild-вызовов не удалён. Это elapsed time
полного обработчика, включая CSS dispatch/native send; не exclusive CPU,
сетевой RTT, client render или полный server tick. Дискретные observations и
продолжающаяся соседняя нагрузка ограничивают причинную интерпретацию.
Allocations/GC/full tick не измерены. Нельзя переносить эти проценты на perf.3,
8/32 клиентов, весь сервер или устранение редких 150мс. Новые lifecycle guards
добавляют native reads, которые требуют canary точного выпуска.

Оригинальная baseline DLL `18e435e7…bbfc`; измеренная perf.2 DLL
`d0c3cd4e…afcd`, код `98113f6`. По сохранённому отчёту владелец подтвердил
клиентский dynamic-page bugfix; standard HTML совпал с baseline по SHA256.
На #6 осталась perf.2, затем выполнен scoped cold start для API binding cleanup.
Это историческое evidence, не новая проверка текущего состояния сервера.

## Решение и выпуск

Кодовая реализация O3 и доступное offline-ревью завершены. Статус выпуска
**awaiting_live**: точная perf.3 требует проверки реальным клиентом, быстрой
навигации/locale/dynamic mutation, movement lock с VIP Endurance, reconnect,
respawn/map/reload и конкурирующих center overlays. Далее 8/32 active menus,
paired full-tick/GC/system telemetry и наблюдение хвостов по общему протоколу.
Блокирующих offline-дефектов после исправлений не найдено.

Runbook — [UPGRADE.md](../../../UPGRADE.md); точная локальная DLL и совместимые
reference hashes — `release-manifest.json`. Откат — согласованный scoped cold
cycle с прежним совместимым core/config/dependency набором, без подмены shared API.
Серверных действий, rollout или новых live-профилей при этом ревью не было.
