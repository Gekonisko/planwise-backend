# Badanie alokacji zasobów: cztery metody, te same instancje, jedna funkcja oceny

Dokument opisuje **metodę**. Wyniki liczbowe generuje `alloc_report.py` z plików w `runs/`; surowe
treści żądań i odpowiedzi leżą w `runs/raw/<znacznik>/`, więc każdą przytoczoną liczbę da się
prześledzić do odpowiedzi, z której pochodzi.

## Pytanie

Czy pętla agentowa — model językowy, który może ocenić własną propozycję i ją poprawić, zanim ją
zatwierdzi — poprawia jakość przydziału zadań na tyle, by uzasadnić swój koszt?

Pytanie ma sens tylko wtedy, gdy „jakość" da się zmierzyć bez danych historycznych. Tutaj da się,
i to jest powód, dla którego akurat alokacja nadaje się na przedmiot badania lepiej niż estymacja
kosztów: **istnieje wzorzec optymalny**. Solver CP-SAT dowodzi optymalności swojego rozwiązania
(`CpSolverStatus.Optimal`), więc każdą inną metodę można wyrazić jako procentową lukę nad znanym
optimum, zamiast porównywać heurystykę z heurystyką.

## Cztery ramiona

| Ramię | Co to jest | Koszt wywołań |
|---|---|---|
| `cpsat` | `CpSatScheduleOptimiser` — solver programowania z ograniczeniami, domyślny w produkcie | brak |
| `greedy` | `GreedyCapacityBalancer` — heurystyka zachłanna, wersja pierwsza i awaryjna | brak |
| `llm-single-shot` | `AgenticScheduleOptimiser` z budżetem **1 tury** | 1 na przebieg |
| `llm-agent` | ta sama klasa z budżetem **5 tur** | do 5 na przebieg |

Dwa ostatnie ramiona to **ta sama klasa, ten sam prompt, ten sam schemat narzędzia**. Przy budżecie
jednej tury narzędzie oceniające jest w ogóle nieoferowane, a `tool_choice` wymusza zatwierdzenie —
czyli dokładnie zachowanie jednostrzałowe, jakie mają pozostałe wywołania LLM w PlanWise. Jedyną
zmienną odróżniającą ramiona jest budżet tur, dzięki czemu różnicę między nimi można przypisać
pętli, a nie sformułowaniu polecenia.

### Dlaczego narzędzie oceniające znika w ostatniej turze

W pierwszym przebiegu tak nie było i to unieważniło pomiar. Zmuszony do `submit_assignment` w swojej
jedynej turze model wysłał **pustą** listę przydziałów, a zaraz po niej, w tej samej odpowiedzi,
pełną i sensowną alokację jako wywołanie `evaluate_assignment` — którego żaden odbiorca nie ma jak
uhonorować. Wynik ewaluacji zwrócony w ostatniej turze i tak nie może zostać wykorzystany, więc
oferowanie tego narzędzia jest pułapką. Od poprawki ostatnia tura dostaje wyłącznie narzędzie
zatwierdzające.

## Funkcja oceny

`ScheduleEvaluator.Evaluate` przyjmuje gotowy przydział i odpowiada na jedno pytanie, które znaczy to
samo dla każdej metody: **kiedy projekt się skończy**, przy zachowaniu kolejności zależności, zasady
jednego zadania naraz na osobę i czasów trwania skalowanych pojemnością etatu.

Model harmonogramu jest celowo identyczny z tym, którego używa `CpSatScheduleOptimiser` — jeden
punkt to jeden dzień, dzielony przez pojemność i zaokrąglany w górę, `NoOverlap` na osobę,
poprzedniki tylko nieukończone. Rozjazd w tym miejscu skrzywiłby po cichu porównanie, któremu ta
funkcja ma służyć.

Ta sama funkcja **napędza agenta**: `evaluate_assignment` uruchamia dokładnie ją. Agent nie ma
prywatnego brudnopisu ocenianego łagodniej niż pozostałe ramiona.

Weryfikują ją testy w `ScheduleEvaluatorTests` — przyrząd pomiarowy, którego nikt nie sprawdził, nie
jest dowodem.

### Luka nad optimum bywa niezdefiniowana

Zadanie bez właściciela **nie zajmuje niczyjego kalendarza**. Ramię, które nie przydzieli nic,
dostaje więc najkrótszy projekt ze wszystkich — i tak właśnie stało się w pierwszym przebiegu, gdzie
pusta odpowiedź wypadła lepiej od solvera.

Dlatego luka nad optimum jest liczona **wyłącznie dla kompletnych alokacji**, a kompletność jest
raportowana jako osobna kolumna. Niewykonana praca nie ma prawa wyglądać na efektywność. Zależność
jest utrwalona jako asercja w teście `Work_left_unallocated_is_reported_rather_than_rewarded`, żeby
nie mogła wrócić niepostrzeżenie.

## Instancje

Pięć syntetycznych projektów, zapisanych na stałe w `AllocationScenarios.cs`, więc każde ramię widzi
identyczne wejście i każdy przebieg da się odtworzyć bez bazy danych.

| Id | Kształt | Po co |
|---|---|---|
| AL-1 | 12 zadań, łańcuch 5 zadań, 4 osoby o różnej pojemności | instancja główna, wyższa liczba powtórzeń |
| AL-2 | 20 zadań niezależnych, 5 osób | czyste równoważenie obciążenia, bez zależności |
| AL-3 | 16 zadań w dwóch równoległych łańcuchach, 3 osoby | łańcuchy trzeba rozdzielić na różne osoby |
| AL-4 | 10 zadań, 4 osoby, w tym dwie na ćwierć etatu | czas trwania zależy od tego, kto weźmie zadanie |
| AL-5 | 40 zadań, las zależności, 6 osób | skala, przy której polecenie przestaje się mieścić w oku |

Instancje są tak zbudowane, żeby wybór osoby **kosztował dni**. Na backlogu niezależnych
jednopunktowych zadań rozdanych równym osobom każda alokacja kończy się tego samego dnia i
porównanie nie mierzy niczego. Każda instancja zawiera więc co najmniej jedno z trojga: łańcuch
zależności dość długi, by zdominować termin; osoby o pojemnościach różniących się na tyle, by zmienić
czas trwania zadania; albo więcej pracy równoległej niż ludzi.

**AL-1 okazał się przy tym bliski zdegenerowanemu** i trzeba to czytać z tą świadomością: jego
łańcuch krytyczny ma 17 punktów, a optimum wynosi 17 dni, więc o terminie decyduje wyłącznie to, czy
łańcuch trafi do osoby na pełnym etacie — reszta zadań może pójść gdziekolwiek. Nie jest to wada
pomiaru, lecz wynik sam w sobie: tam, gdzie o terminie rozstrzyga ścieżka krytyczna, pętla nie ma
czego poprawić.

## Uruchomienie

```
dotnet run -- --alloc-only --reps 5 --primary-reps 8 --agent-turns 5
python alloc_report.py
```

`--scenarios AL-3,AL-5` wznawia przerwaną serię; `alloc_report.py` scala wiele plików raportów, więc
przebiegi już opłacone nie są powtarzane.

## Czego to badanie nie mierzy

- **Rzeczywistych terminów projektów.** Długość projektu to wielkość modelowa, wyliczona z punktów i
  pojemności etatów, a nie zaobserwowana. Wnioski dotyczą jakości przydziału względem zadanego celu,
  nie trafności samego modelu czasu.
- **Jakości dopasowania kompetencji.** Dopasowanie umiejętności to heurystyka szukająca etykiety w
  tytule zadania; żadne zadanie nie niesie pola wymaganych kompetencji.
- **Kosztu w pieniądzu.** Raportowane są wywołania i tokeny. Przeliczenie na walutę zależy od cennika
  dostawcy i celowo nie jest utrwalane w kodzie.
- **Innych modeli i konfiguracji.** Wnioski ograniczają się do badanego modelu, budżetu tur i tych
  pięciu instancji.

## Wyniki (56 przebiegów LLM, claude-opus-5, budżet agenta 5 tur)

| Instancja | Struktura | Optimum | Zachłanna | LLM 1 tura | LLM agent |
|---|---|---|---|---|---|
| AL-1 | łańcuch krytyczny | 17 dni | +29,4% | +0,0% | +0,0% |
| AL-2 | brak zależności | 17 dni | +5,9% | +7,1% | +7,1% |
| AL-3 | dwa łańcuchy | 24 dni | +0,0% | +0,0% | +0,0% |
| AL-4 | zróżnicowana pojemność | 12 dni | +91,7% | +33,3% | **+8,3%** |
| AL-5 | 40 zadań | 26 dni | +46,2% | +18,5% | +17,7% |
| | **średnia** | | +34,6% | +10,5% | +5,9% |

Nakład: jednostrzał 1 wywołanie i ~710 tokenów wyjścia; agent ~4,3 wywołania i ~3524 tokeny
(na AL-5: 5 wywołań, 6607 tokenów, ~56 s). Metody deterministyczne nie wykonują wywołań zewnętrznych.

**Pętla poprawiła wynik na jednej instancji z pięciu.** Na AL-4 zdjęła 25 punktów procentowych; na
AL-1, AL-2 i AL-3 oba warianty wypadły identycznie, a na AL-5 różnica 0,8 punktu mieści się w
zaobserwowanym rozrzucie. Hipoteza wyjaśniająca, wymagająca sprawdzenia na szerszym zbiorze
instancji: pętla wnosi wartość tam, gdzie skutku decyzji nie da się przewidzieć bez policzenia go.
Na AL-1 i AL-3 o terminie rozstrzyga długość łańcucha, widoczna wprost w danych wejściowych. Na AL-4
czas trwania jest ilorazem punktów i pojemności etatu, a skutek przydziału ujawnia się dopiero po
rozplanowaniu.

Oba warianty LLM wypadły wyraźnie lepiej od heurystyki zachłannej obecnej w systemie i żaden nie
przekroczył wyniku solvera — co wynika z konstrukcji, bo solver dowodzi optymalności.

**Zgodność bez zarzutu:** w 56 przebiegach zero przydziałów do osoby spoza projektu, zero ponownych
przydziałów zadań zakończonych, zero odebranych przydziałów istniejących, wszystkie alokacje
kompletne.

**Powtarzalność:** zero rozrzutu na AL-1, AL-3 i AL-4 (na AL-4 agent stabilizuje się na 13 dniach,
jednostrzał na 16). Rozrzut pojawia się na AL-5: CV 0,072 jednostrzał, 0,049 agent.

### Dwa tryby awarii, oba pomiarowe

1. **Limit tokenów odpowiedzi.** Przy 8192 jeden z pięciu przebiegów agentowych na AL-5 urwał się w
   połowie (`stop_reason: max_tokens`), nie wróciło żadne użyteczne wywołanie i odpowiedziała
   heurystyka. Po podniesieniu do 16384 zjawisko nie wystąpiło; na mniejszych instancjach limit nigdy
   nie był wiążący. Ograniczenie konfiguracji, nie modelu — ale wyznacza granicę: odpowiedź
   odtwarzająca cały przydział rośnie z liczbą zadań.
2. **Odrzucenie wywołań po wyczerpaniu środków.** Przebiegi z tego okresu zwróciły wynik heurystyki,
   w samym obiekcie odpowiedzi nieodróżnialny od wyniku modelu. Stąd dwie zmiany: nazwa metody, która
   faktycznie odpowiedziała, jest zapisywana i wykluczana ze statystyk LLM, a seria przerywa się przy
   pierwszej odpowiedzi odrzuconej przez dostawcę.
