# Text Reader

WPF aplikace pro prohlížení velkých textových souborů z různých zdrojů.

## Funkce

**Načítání z různých zdrojů:**
- Lokální textové soubory
- Webové adresy
- Generování náhodného textu

**Práce s daty:**
- Vyhledávání s navigací mezi výsledky
- Uložení do souboru
- Výběr a kopírování textu
- Volitelné číslování řádků

**Výkon:**
- Okamžité otevření i u souborů o desítkách GB
- Virtualizované zobrazení - spotřeba paměti nezávisí na velikosti souboru
- Plynulé scrollování při milionech řádků

## Spuštění

Připravený build aplikace najdete v kořenové složce projektu - stačí spustit `TextReader.exe`.

Pro spuštění ze zdrojových kódů:

```bash
cd TextReader
dotnet run
```

## Ovládání

### Základní workflow
1. Klikněte na **Load** a vyberte zdroj dat (soubor/URL/generování)
2. Pro vyhledávání stiskněte **Ctrl+F**, zadejte text a potvrďte Enter
3. Navigujte mezi výsledky pomocí **F3** (další) / **Shift+F3** (předchozí)
4. Uložte obsah pomocí **Ctrl+S**

### Klávesové zkratky

| Zkratka | Funkce |
|---------|--------|
| `Ctrl+F` | Otevřít vyhledávání |
| `F3` | Další výsledek |
| `Shift+F3` | Předchozí výsledek |
| `Ctrl+S` | Uložit do souboru |
| `Ctrl+L` | Přepnout číslování řádků |
| `Home/End` | Skok na začátek/konec dokumentu |
| `PgUp/PgDn` | Stránkování |

### Ovládání myší

- **Double-click na levé straně readeru** - Přepnout číslování řádků

## Technické řešení

### Optimalizace pro velké soubory

**Buffering systém**
V paměti je vždy jen viditelná část dokumentu (~128 řádků). Zbytek se čte z disku on-demand. Soubor o 50 GB tak zabere v RAM pouze jednotky MB.

**Asynchronní výpočet offsetů**
Pozice řádků se počítají na pozadí. Aplikace je použitelná okamžitě, čeká se pouze na aktuálně zobrazované řádky.

**DrawingVisual rendering**
Vlastní implementace vykreslování pomocí low-level WPF API (DrawingVisual) namísto standardních TextBox kontrolek. Výrazně vyšší výkon při renderingu velkého množství textu.

**Cache vyhledávání**
První vyhledávání projde celý soubor a výsledky uloží do cache. Následné navigace mezi výsledky jsou okamžité. Inkrementální optimalizace - při hledání "hello" se využije již cachovaný výsledek pro "hell".

### Architektura

```
ITextInput interface
├── FileTextInput      - streaming ze souboru
├── UrlTextInput       - HTTP range requests s fallbackem na lokální download
└── RandomTextInput    - generování testovacích dat

LoadedText             - facade nad ITextInput + SearchFeature
TextReaderControl      - WPF control s virtualizací
RenderCanvas          - custom canvas s DrawingVisual API
```

**Hybrid strategie pro URL:** Aplikace začne s HTTP range requests (okamžitá dostupnost) a paralelně stahuje celý soubor na pozadí. Po dokončení stažení automaticky přepne na rychlejší file-based operace.

## Technologie

- .NET 8.0
- WPF (Windows Presentation Foundation)
- C# 12
- DrawingVisual API
- xUnit (testování)

