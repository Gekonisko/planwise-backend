# Badanie estymacji kosztów: pojedyncze wywołanie wobec pętli weryfikacji

Metoda i wyniki. Liczby generuje `aggregate.py`; surowe treści leżą w `runs/raw/<znacznik>/`.

## Dlaczego inaczej niż przy alokacji

Dla przydziału zadań istnieje wzorzec optymalny — solver dowodzi optymalności, więc każdą metodę da
się wyrazić jako lukę nad znanym optimum. Dla kosztu takiego wzorca **nie ma**: rzeczywistych kosztów
zakończonych przedsięwzięć nie mamy, a pytanie i tak dotyczy wpływu pętli na własności odpowiedzi, a
nie trafności przewidywania wydatków.

Badane są więc wyłącznie wielkości rozstrzygalne bez danych referencyjnych: zgodność z przekazanym
cennikiem, spójność wewnętrzna kosztorysu i powtarzalność przy niezmienionym wejściu.

## Dwa ramiona

| Ramię | Budżet tur | Narzędzie `check_estimate` |
|---|---|---|
| `llm-single-shot` | 1 (klasa produkcyjna) | niedostępne |
| `llm-agent` | 4 | dostępne poza ostatnią turą |

Wspólne polecenie systemowe, wspólny opis kontekstu i wspólny schemat odpowiedzi — nie kopie, lecz
te same elementy kodu, żeby ramiona nie rozeszły się w czasie.

`check_estimate` przelicza każdą pozycję względem cennika i zwraca: role spoza cennika, zmienione
stawki, pozycje łamiące `koszt = godziny × stawka`, sumę składników i odległość każdego wariantu od
tej sumy. **Nie proponuje pracochłonności** — sprawdza tylko to, co rozstrzygalne.

Pominięto pamięć podręczną opartą na skrócie wejścia; dla identycznego wejścia zwracałaby odpowiedź
zapamiętaną i uniemożliwiała pomiar powtarzalności.

Kotwicą powtarzalności jest **suma wszystkich pozycji**, nie wartość wariantu zwanego realistycznym:
model swobodnie dobiera nazwy, więc miara oparta na nazwie mierzyłaby konwencję nazewniczą.

## Wyniki (46 przebiegów, claude-sonnet-5)

### Zgodność — bez zarzutu w obu ramionach

Zero ról spoza cennika, każda stawka dokładnie ta przekazana, pełne pokrycie ról. Pętla nie ma tu
czego naprawiać.

### Spójność wewnętrzna — pętla naprawia wszystko, co sprawdza

| Scenariusz | uzgodnione 1 tura | uzgodnione z pętlą | najmniejsze odchylenie 1 tura | z pętlą |
|---|---|---|---|---|
| CE-1 | 3/8 | 8/8 | 8,4% | 0,0% |
| CE-2 | 0/4 | 5/5 | 9,8% | 0,0% |
| CE-3 | 0/5 | 5/5 | 16,6% | 0,0% |
| CE-4 | 1/5 | 5/5 | 4,5% | 0,0% |
| **razem** | **4/22** | **23/23** | | |

Naruszenia `koszt = godziny × stawka` (3% pozycji w CE-1) zniknęły całkowicie. Rozpoznawalność nazw
wariantów — której narzędzie **nie** sprawdza — nie poprawiła się.

### Powtarzalność — wynik sprzeczny z prognozą

| Scenariusz | CV 1 tura | CV z pętlą | rozstęp 1 tura | z pętlą |
|---|---|---|---|---|
| CE-1 | 0,337 | 0,095 | 94% | 23% |
| CE-2 | 0,357 | 0,107 | 73% | 29% |
| CE-3 | 0,681 | 0,040 | 177% | 9% |
| CE-4 | 0,554 | 0,063 | 123% | 16% |

Prognoza zapisana w kodzie **przed** pomiarem brzmiała: pętla poprawi spójność i zostawi rozrzut mniej
więcej tam, gdzie był. Była błędna. W CE-3 pojedyncze wywołania dawały dla tego samego backlogu sumy
od 122 do 662 tysięcy; z pętlą — od 173 do 190 tysięcy.

Wyjaśnienie prawdopodobne, niezweryfikowane: zmuszony do uzgodnienia model wiąże wartość łączną z
sumą własnych pozycji, a ta zmienia się słabiej niż liczba dobierana swobodnie.

### Zastrzeżenie, bez którego wynik jest nadinterpretowany

Średnie w wariancie z pętlą są **systematycznie niższe** (CE-2: 395→228 tys., CE-3: 306→183 tys.,
CE-4: 313→211 tys.). Bez danych referencyjnych nie sposób orzec, który poziom jest bliższy prawdy.
Zmniejszenie rozrzutu jest udokumentowane; przesunięcie poziomu **nie jest** dowodem poprawy
trafności. Kosztorys stabilny i błędny pozostaje błędny.

## Dwa tryby awarii

1. **Jednostrzał, CE-2.** Pierwsze wywołanie urwane na `max_tokens`; w powtórzeniu model przekazał
   listę pozycji jako łańcuch znaków i dołączył drugie wywołanie z pustymi tablicami. Produkcyjny
   mechanizm ponowień wykonał obie próby i zgłosił błąd — zachowanie poprawne, widoczne **tylko**
   dzięki naprawionej kontroli pustych scenariuszy (wcześniej taka odpowiedź przechodziła po cichu).
2. **Agent, CE-3.** Model przekazał `labourLines` jako łańcuch znaków; `Reconcile` wywołało na tym
   `AsArray()` i wyjątek przerwał pętlę — czyli mechanizm, który powinien był to odstępstwo obsłużyć.
   Po utwardzeniu niezgodna struktura jest zgłaszana modelowi jako usterka do naprawienia; scenariusz
   powtórzono bez niepowodzeń. Odstępstwo wywracające pętlę niweczy sens pętli.

## Uruchomienie

```
dotnet run -- --cost-only --reps 5 --primary-reps 8 --cost-agent-turns 4
python aggregate.py runs/llm-evaluation-<a>.json runs/llm-evaluation-<b>.json
```

Scalanie zastępuje powtórzone `(scenariusz, ramię, próba)` — scenariusz powtórzony po poprawce nie
liczy się podwójnie.
