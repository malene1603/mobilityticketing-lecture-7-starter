# Resultater

## Sammenligning af læsninger

| Datamængde | Læsning | Udførte kommandoer | Route-entiteter | Trip-entiteter | SQL-log |
| --- | --- | ---: | ---: | ---: | --- |
| 4 ruter / 9 ture | Baseline | 5 | 4 | 9 | [log](logs/20261029T1235386568658Z-baseline-0a6c681a.sql) |
| 4 ruter / 9 ture | Live board | 1 | 0 | 0 | [log](logs/20261029T1235399551903Z-live-f2519f05.sql) |
| 200 ruter / 100.000 ture | Baseline | 201 | 200 | 100.000 | [log](logs/20261029T1235582671716Z-baseline-f15549e6.sql) |
| 200 ruter / 100.000 ture | Live board | 1 | 0 | 0 | [log](logs/20261029T1235596422918Z-live-b3bd8c7b.sql) |

Baseline-løsningen med lazy loading udfører én forespørgsel efter ruterne og derefter én pr. rute. Derfor vokser antallet af læsninger og indlæste objekter med datamængden. Live board bruger én projektion direkte til `RouteOverviewDto`. Databasen tæller og finder den tidligste afgang pr. rute, så løsningen kræver fortsat kun én læsning og indlæser ingen entiteter, uanset datamængden.

## Scenarie: aflysning, omdøbning, afvist booking og uændret plan

Kør scenariet efter `reset`: offentliggør `morning`, aflys `T102`, og omdøb `R1` til "City Ring".

| Trin | Resultat |
| --- | --- |
| `publish morning` | Offentliggjort |
| `cancel T102` | Aflyst |
| `rename R1 "City Ring"` | Omdøbt |
| Live board, R1 | City Ring / 1 / 06:20 UTC ([log](logs/20261029T1232171113578Z-live-8395c061.sql)) |
| Plan `morning`, R1 | City Loop / 2 / 06:00 UTC, uændret ([log](logs/20261029T1232183744096Z-snapshot-019e0ea6.sql)) |
| `book T102 fresh-request` | `Cancelled`, ingen kvittering |
| `publish revised`, og læs derefter planen | City Ring / 1 / 06:20 UTC ([log](logs/20261029T1232225540830Z-snapshot-4e1ae895.sql)) |

Læsning af en offentliggjort plan kræver én kommando og indlæser ingen `Route`- eller `Trip`-entiteter. Der anvendes én `LEFT JOIN` mellem `published_plans` og `published_plan_routes`.

## Gentagne forsøg og konflikter

| Trin | Resultat |
| --- | --- |
| `book T201 campus-1` | Booket, kvittering `2026-10-29T12:33:42.186324Z` |
| `book T201 campus-1` igen | `AlreadyBooked`, identisk kvittering |
| `select * from lab07.bookings` | Én række |
| `publish morning` to gange | Først `Published`, derefter `AlreadyPublished` |
| `publish morning` for 07:00-08:00 | "Snapshot 'morning' belongs to another time window." |
| `snapshot morning` bagefter | Stadig 06:00-07:00 |

## Tests

- **Starter- og accepttests:** 47 bestået, 0 fejlet ([trx](tests/_52320d548dd4_2026-10-29_12_22_39.trx)).
- **Mine egne tests** (`StudentTests.cs`): 4 bestået, 0 fejlet ([trx](tests/_df7db17e72a0_2026-10-29_12_30_50.trx)).

Mine egne tests dækker:
- Trimning af mellemrum ved omdøbning samt afvisning af et tomt navn.
- Oprettelse af en ny plan efter ændringer, mens en ældre plan forbliver uændret.
- Samtidig offentliggørelse af to forskellige planer, hvor hver plan bevarer sit eget tidsvindue og sine egne rækker.
- Afvisning af et tidsvindue på under ét mikrosekund uden at gemme data.

Designskitsen og forklaringen findes i [`docs/design.md`](../../design.md).