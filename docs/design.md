# Designbeslutning

## Stier og tabeller

```text
Query   live board      -> LiveBoardQuery          -> routes + trips (kun læsning)
Query   published plan  -> ReadSnapshotAsync       -> published_plans + published_plan_routes
Command cancel          -> CancelTripCommand       -> trips (rækkelås)
Command rename          -> RenameRouteCommand      -> routes
Command book            -> BookTripCommand         -> bookings (+ rækkelås på trips)
Command publish         -> PublishSnapshotCommand  -> læsning af live board + begge plantabeller
```

## Sammenligning

Jeg sammenlignede to designløsninger. Design A beregner live board ved hver læsning med én projektion, der bruger `COUNT` og `MIN` pr. rute på tværs af `routes` og `trips`. Design B vedligeholder en tabel med aktuelle oversigtsdata, som opdateres ved hver ændring. Jeg valgte design A.

En aflysning opdaterer én række i `trips`, og den næste læsning afspejler ændringen. I design B skulle oversigtstabellen også opdateres, og en omdøbning ville kræve ændringer to steder. En offentliggjort plan beskriver et bestemt tidsvindue. Hvis man vil understøtte et andet tidsvindue, kræver det enten en række pr. tidsvindue eller en ny beregning, hvilket fjerner fordelen ved design B.

En offentliggjort plan er sin egen kopi (`published_plans` og `published_plan_routes`) uden fremmednøgle til `routes`. Derfor kan senere aflysninger eller omdøbninger ikke ændre den offentliggjorte plan.

**Transaktionsgrænser:** Booking serialiseres pr. request-ID og låser rækken for den pågældende tur. Aflysning låser den samme række. Offentliggørelse læser live board og skriver både hovedposten og rækkerne i én transaktion, serialiseret pr. snapshot-ID, med et hook lige før commit.

Til live board valgte jeg en projektion frem for `Include`. `Include` ville også fjerne mønsteret med én forespørgsel pr. rute, men det ville stadig indlæse komplette `Route`- og `Trip`-objekter, som kun skulle bruges til at tælle posterne. Målingerne viser, at projektionen kræver én læsning og indlæser nul entiteter ved begge datastørrelser.
