# Ocena LLM w priorytetyzacji i estymacji kosztów

> Jakie możliwości i ograniczenia ujawnia zastosowanie dużych modeli językowych do priorytetyzacji
> zadań i estymacji kosztów w systemie PlanWise, oceniane pod względem **zgodności z danymi
> projektu**, **spójności** i **powtarzalności** wyników?

Stanowisko mierzy dwa mechanizmy PlanWise oparte na modelach językowych — porządkowanie backlogu
(`IBacklogPrioritisationModel`) i estymację kosztów (`ICostEstimationModel`) — wzdłuż trzech osi
pytania badawczego.

## Dlaczego nie potrzeba rzeczywistych kosztów projektów

Pytanie nie dotyczy trafności przewidywania faktycznych wydatków, lecz trzech własności, które daje
się ustalić bez danych o wykonaniu:

| oś | co jest punktem odniesienia |
|---|---|
| zgodność | **dane wejściowe przekazane modelowi** — czy użyte role występują w cenniku, czy stawki są dokładnie te podane, czy klucze zadań istnieją w backlogu |
| spójność | **sama odpowiedź** — czy `cost = hours × hourlyRate`, czy suma scenariusza równa się sumie jego składników, czy warianty są uporządkowane |
| powtarzalność | **inny przebieg na identycznym wejściu** — rozrzut kosztu, przesunięcia pozycji w rankingu |

Żadna z tych miar nie wymaga wiedzy o tym, ile projekt faktycznie kosztował.

## Konstrukcja stanowiska

Harness wywołuje **produkcyjne implementacje**, a nie własną kopię zapytań. Dzięki temu mierzone są
rzeczywiste instrukcje systemowe, definicje narzędzi i obsługa błędów, a nie ich przybliżenie.

Trzy decyzje konstrukcyjne warte odnotowania:

**Pominięcie procedury zadania asynchronicznego.** Modele są wołane bezpośrednio, nie przez
`CostEstimationJobHandler`. Handler liczy skrót SHA-256 danych wejściowych i przy niezmienionym
wejściu zwraca zapisany wcześniej wynik — czyli dokładnie to, co unieważniłoby pomiar
powtarzalności. Wielokrotne odczytanie tego samego rekordu nie jest badaniem stabilności modelu.

**Przechwytywanie warstwy transportowej.** `RecordingHandler` zapisuje surowe treści żądań i
odpowiedzi. Jest to konieczne, ponieważ obie implementacje **naprawiają** odpowiedź modelu w drodze
do aplikacji: prioritizer po cichu odrzuca nieznane i powtórzone klucze zadań oraz dopisuje pominięte
pozycje na końcu. Mierząc wyłącznie zwrócony obiekt, opisalibyśmy zachowanie *systemu*, a nie
*modelu*, i otrzymalibyśmy idealną zgodność niezależnie od tego, co model zrobił. Raport podaje
obie warstwy osobno.

**Dostęp przez refleksję.** Obie klasy są `internal` w swoich zestawach infrastruktury. Harness
tworzy je przez refleksję, zamiast dodawać `InternalsVisibleTo` do kodu produkcyjnego — narzędzie
pomiarowe nie powinno wymuszać zmian w mierzonym systemie.

Klucze API czytane są z tego samego magazynu user secrets co projekt API (`UserSecretsId` wskazuje
na ten sam identyfikator), więc nie są nigdzie kopiowane.

## Scenariusze

Wszystkie dane są syntetyczne i **deterministyczne** — identyczne bajty w każdym powtórzeniu.
Jakakolwiek zmienność wejścia byłaby nieodróżnialna od zmienności modelu.

Projekt bazowy: przepisanie portalu klienta, 24 zadania z opisami niosącymi realny sygnał
(integracja płatnicza, migracja danych, zgodność z RODO), zespół pięciu osób w czterech rolach
o różnych stawkach i wymiarach etatu.

### Estymacja kosztów

| id | scenariusz | co sprawdza |
|---|---|---|
| CE-1 | 8 zadań, pełny cennik | przypadek podstawowy; zarazem scenariusz powtarzalności (10 powtórzeń) |
| CE-2 | 24 zadania, pełny cennik | wpływ długości backlogu |
| CE-3 | 24 zadania, cennik zastępczy | czy model ujawnia zastępczy charakter danych, gdy nikt w zespole nie ma stawki |
| CE-4 | 24 zadania, połowa bez oszacowań | zachowanie przy niekompletnych danych wejściowych |

### Priorytetyzacja

| id | scenariusz | co sprawdza |
|---|---|---|
| BP-1 | 10 zadań, pełne dane | przypadek podstawowy; scenariusz powtarzalności (10 powtórzeń) |
| BP-2 | 24 zadania, pełne dane | wpływ długości backlogu |
| BP-3 | 24 zadania, brak prognozy ryzyka | czy brak jednego z kryteriów destabilizuje wynik |
| BP-4 | 20 zadań, 11 krawędzi zależności | czy kolejność respektuje relacje poprzedzania |

## Miary

### Zgodność z danymi projektu

- **role spoza cennika** — pozycje kosztów pracy z rolą, której nie ma w przekazanym cenniku
- **stawki** — udział pozycji, w których stawka godzinowa jest dokładnie tą podaną
- **pokrycie cennika** — udział ról z cennika wykorzystanych w kosztorysie
- **klucze obce / powtórzone / pominięte** — zadania wymyślone, zdublowane lub opuszczone przez
  model, liczone w warstwie surowej
- **pozycje nieuszeregowane** — ile pozycji warstwa aplikacji musiała dopisać samodzielnie

### Spójność

- **arytmetyka pozycji** — udział pozycji spełniających `cost = hours × hourlyRate`
- **zgodność sumy** — czy suma wariantu równa się sumie kosztów pracy i kosztów pozapłacowych
  (równanie kosztu całkowitego przyjęte w pracy)
- **kompletność i uporządkowanie wariantów** — trzy scenariusze, rozpoznawalne nazwy, niemalejące
  sumy i percentyle
- **respektowanie zależności** — udział par (poprzednik, następnik) uszeregowanych poprawnie
- **korelacja własnej oceny z pozycją** — czy wystawione przez model oceny wartości tłumaczą
  nadaną przez niego kolejność, czy są ozdobne

### Powtarzalność

- **współczynnik zmienności** kosztu wariantu środkowego i łącznej liczby godzin
- **rozstęp względny** kosztu
- **podobieństwo Jaccarda** zbioru użytych ról
- **średnie przesunięcie pozycji** między przebiegami
- **korelacja rangowa Spearmana** i **podobieństwo pierwszej piątki**

## Uruchomienie

```bash
dotnet build
dotnet run -- --smoke                          # po jednym wywołaniu na scenariusz
dotnet run -- --reps 5 --primary-reps 10       # pełna seria (50 wywołań)
python aggregate.py                            # tabele zbiorcze
python inspect.py                              # podgląd pojedynczych przebiegów
```

Wyniki trafiają do `runs/llm-evaluation-<znacznik>.json`, a surowe treści żądań i odpowiedzi do
`runs/raw/<znacznik>/`. Raport jest zapisywany po każdym przebiegu, więc przerwana seria nie traci
dotychczasowych danych.

## Wyniki

Seria z 19 września 2026. Modele: `claude-sonnet-5` (koszty), `claude-opus-5` (priorytetyzacja).
51 zarejestrowanych przebiegów, 53 wywołania HTTP, 129 tys. tokenów wejścia i 119 tys. wyjścia,
20,7 minuty czasu wywołań.

### Zgodność z danymi projektu

| | estymacja kosztów (n=26) | priorytetyzacja (n=25) |
|---|---|---|
| role spoza cennika | **0** | — |
| stawki dokładnie z cennika | **1,00** | — |
| pokrycie cennika | **1,00** | — |
| wymyślone klucze zadań | — | **0** |
| powtórzone klucze | — | **0** |
| pominięte zadania | — | **0** |
| oceny poza przedziałem [0,1] | — | **0** |
| pozycje dopisane przez warstwę aplikacji | — | **0** |

Zgodność z przekazanymi danymi okazała się bez zarzutu w obu mechanizmach. Ograniczenie modelu do
ról i stawek z cennika oraz do kluczy zadań z backlogu zadziałało w każdym przebiegu. Warstwa
obronna prioritizera — odrzucanie nieznanych kluczy i dopisywanie pominiętych — **ani razu nie
musiała zadziałać**; gdyby mierzyć wyłącznie zwrócony obiekt, nie dałoby się tego odróżnić od
sytuacji, w której stale ratuje ona wadliwe odpowiedzi.

### Spójność

| scenariusz | bez wariantów | 3 warianty | rozpozn. nazwy | monotoniczne | arytmetyka pozycji | uzgodnienie sum |
|---|---|---|---|---|---|---|
| CE-1 | 1/10 | 9/10 | 5/10 | 10/10 | 0,95 | 5/9 |
| CE-2 | 2/6 | 4/6 | 2/6 | 6/6 | 0,96 | 0/4 |
| CE-3 | 1/5 | 4/5 | 1/5 | 5/5 | 1,00 | 0/4 |
| CE-4 | 0/5 | 5/5 | 2/5 | 5/5 | 0,95 | 2/5 |

Spójność jest słabym punktem estymacji kosztów i zawodzi na kilku poziomach jednocześnie:

- **W 4 z 26 przebiegów odpowiedź nie zawierała żadnego wariantu kosztowego**, mimo że schemat
  narzędzia oznacza `scenarios` jako pole wymagane. Klient produkcyjny przyjął taki wynik bez
  zastrzeżeń — użytkownik zobaczyłby kosztorys bez kosztu.
- **Nazwy wariantów są swobodne.** Tylko 10 z 26 przebiegów użyło rozpoznawalnej trójki
  optymistyczny–realistyczny–pesymistyczny. Spotykane etykiety to „P50 — Base estimate",
  „P70 — With 12% contingency", „Low (optimistic)", „Worst case". Kod dobierający wariant po nazwie
  jest wobec tego zawodny.
- **Wariant środkowy nie ma stałego znaczenia.** Bywa estymatą bazową, bywa bazą powiększoną
  o rezerwę. Z tego powodu powtarzalność liczono na sumie pozycji, a nie na sumie wariantu.
- **Uzgodnienie sum zawodzi w 15 z 22 przebiegów.** Tylko w 7 przypadkach jakikolwiek wariant
  uzgadnia się z sumą własnych pozycji modelu (tolerancja 0,5%). Najmniejsze odchylenie wynosi
  średnio od 1,7% (CE-1) do 5,9% (CE-3). Skrajny przypadek ze wstępnej próby: suma scenariusza
  równa dokładnie kosztom pracy, z pominięciem własnych pozycji pozapłacowych na 17 257 USD.
- **Arytmetyka pojedynczej pozycji zawodzi w 4 z 89 przypadków (4,5%).** Wszystkie cztery dotyczą
  jedynej roli o niezaokrąglonej stawce mieszanej (98,33 USD/h), a błąd jest zawsze dodatni
  i drobny (od +8 do +80 USD, czyli 0,03–0,04% pozycji).

Priorytetyzacja wypada tu odmiennie: uszeregowanie respektuje **wszystkie** 11 krawędzi zależności
w każdym z 5 przebiegów BP-4, a uzasadnienia są w 100% różne między pozycjami. Korelacja rangowa
między wystawioną przez model oceną wartości a nadaną przez niego pozycją wynosi 0,62–0,80 —
oceny tłumaczą kolejność tylko częściowo, są więc raczej komentarzem niż mechanizmem decyzyjnym.

### Powtarzalność

Estymacja kosztów, kotwica: suma pozycji kosztowych.

| scenariusz | n | średnia | CV | min | max | rozstęp |
|---|---|---|---|---|---|---|
| CE-1 (8 zadań) | 10 | 135 249 | **0,341** | 58 349 | 210 944 | **113%** |
| CE-2 (24 zadania) | 6 | 307 802 | **0,560** | 129 240 | 568 224 | **143%** |
| CE-3 (cennik zastępczy) | 5 | 161 494 | 0,245 | 132 120 | 231 000 | 61% |
| CE-4 (braki oszacowań) | 5 | 333 890 | 0,410 | 224 363 | 559 808 | 100% |

Priorytetyzacja, identyczne wejście:

| scenariusz | n | par | D_rank | ρ Spearmana | top-5 Jaccard | pary identyczne |
|---|---|---|---|---|---|---|
| BP-1 (10 zadań) | 10 | 45 | 0,24 | **0,974** | 0,88 | 16/45 |
| BP-2 (24 zadania) | 5 | 10 | 0,37 | **0,995** | 0,80 | 0/10 |
| BP-3 (bez ryzyka) | 5 | 10 | 0,41 | **0,994** | 0,80 | 0/10 |
| BP-4 (zależności) | 5 | 10 | **0,06** | **0,999** | 1,00 | 4/10 |

To najostrzejszy kontrast w całym badaniu. Przy identycznym wejściu, identycznej architekturze
i wymuszonym wywołaniu narzędzia w obu przypadkach:

- koszt tego samego ośmiozadaniowego backlogu waha się od 58 tys. do 211 tys. USD — **rozstęp
  przekracza średnią**;
- kolejność tego samego backlogu jest niemal identyczna, ze średnim przesunięciem pozycji poniżej
  pół miejsca i korelacją rangową powyżej 0,97.

Możliwe wyjaśnienie, którego w tej pracy nie weryfikowano: priorytetyzacja jest **uporządkowaniem
względnym zbioru domkniętego** — przestrzeń odpowiedzi jest ograniczona przez wejście, a model musi
jedynie ułożyć elementy, które dostał. Estymacja kosztów jest **otwartym generowaniem wielkości
liczbowej** — nic w danych wejściowych nie kotwiczy rzędu wielkości, a liczba godzin dla roli jest
swobodnym wyborem modelu. Zależności dodatkowo stabilizują ranking: BP-4 ma najwyższą powtarzalność
ze wszystkich scenariuszy, bo część kolejności jest wymuszona przez strukturę.

Co istotne, **zbiór użytych ról jest w pełni stabilny** (Jaccard 1,00 we wszystkich scenariuszach
kosztowych) — zmienia się rozkład godzin, nie skład zespołu. Zmienność nie jest więc chaotyczna:
dotyczy skali, nie struktury.

### Zaobserwowane tryby awarii

Trzy odrębne, poza zestawem zliczonym wyżej:

1. **`ArgumentNullException` z kodu produkcyjnego.** Gdy model pominie tablicę `reductions`,
   `AnthropicCostEstimationModel.ParseResult` wywołuje `.Select` na wartości `null`. Pętla ponowień
   łapie wyłącznie `JsonException`, więc wyjątek opuszcza `EstimateAsync` nieobsłużony. Wystąpiło
   raz i przerwało całą serię — stąd podział badania na dwa uruchomienia.
2. **Odchylenie od schematu nieusunięte przez ponowienie.** Model wyemitował parametry narzędzia
   jako tekst ze znacznikami `<parameter name=...>`, a przy ponowieniu rozsypał tablicę `scenarios`
   na klucze najwyższego poziomu `"1"` i `"2"`. Oba podejścia zawiodły strukturalnie, po czym
   klient zgłosił niepowodzenie. Jedno dopuszczone ponowienie okazało się niewystarczające.
3. **Odpowiedź bez wariantów przyjęta jako poprawna** — opisana wyżej, 4 z 26 przebiegów.

Pierwszy i trzeci tryb są defektami implementacji ujawnionymi przez eksperyment, nie właściwościami
modelu: schemat deklaruje oba pola jako wymagane, lecz klient nie weryfikuje ich obecności po
odczytaniu odpowiedzi.

## Ograniczenia wniosków

Wyniki dotyczą **wskazanych modeli, w tej konfiguracji i na tych scenariuszach**. Konkretnie:

- dwa różne modele — estymacja kosztów i priorytetyzacja korzystają z odrębnej konfiguracji i nie
  muszą zachowywać się tak samo;
- temperatura nie jest ustawiana, obowiązuje wartość domyślna dostawcy; pełna treść wysłanych żądań
  jest zapisana w `runs/raw/`, więc konfiguracja jest odtwarzalna;
- dane są syntetyczne, a projekt jeden — wyniki opisują zachowanie na tym materiale, nie
  w dowolnym projekcie;
- liczba powtórzeń (5, a dla scenariuszy podstawowych 10) pozwala wykryć rząd wielkości rozrzutu,
  nie zaś precyzyjnie go oszacować;
- dostawca może zmienić zachowanie modelu bez zmiany jego nazwy, wobec czego wyniki są związane
  z datą wykonania, zapisaną w raporcie.
