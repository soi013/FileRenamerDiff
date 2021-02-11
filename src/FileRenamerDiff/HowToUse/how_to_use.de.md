% How To Use
% soi
% 2021/02/08

[View Online](https://github.com/soi013/FileRenamerDiff/blob/master/src/FileRenamerDiff/HowToUse/how_to_use.de.md)

# Schnell 

1. Spezifikation der Zieldatei: Sie können Dateien per Drag & Drop in die Dateiliste ziehen oder nach <kbd>Filesuche</kbd> aus einem bestimmten Verzeichnis suchen.

2. Muster für das Umbenennen einstellen: Stellen Sie das Muster "Löschen/Ersetzen" unter <kbd>Einstellungen</kbd> ein.

3. Umbenennen bestätigen: Führen Sie <kbd>Trockenlauf</kbd> aus, um das Umbenennen zu bestätigen.

4. Umbenennen ausführen: Führen Sie <kbd>Umbenennen Speichern</kbd> aus, um die aktuelle Datei umzubenennen.

# Geben Sie die Zieldatei an.

Sie können die Zieldatei, die umbenannt werden soll, auf zwei Arten angeben: " Dateien durchsuchen" oder "Direkte Angabe".

Um eine Datei aus der Zieldateiliste zu entfernen, klicken Sie auf die Schaltfläche links in der Zeile. Sie können die Dateiliste auch leeren, indem Sie auf die Schaltfläche oben links klicken.

## Dateien durchsuchen

Suchen Sie nach Dateien in einem beliebigen Ordner und fügen Sie sie der Liste der Zieldateien hinzu.

Es gibt drei Möglichkeiten, nach Dateien zu suchen.

- Auswahl über das Ordnersymbol in der oberen linken Ecke mit Hilfe des Dialogs.
- Ziehen Sie einen Ordner auf den Ordnerpfad in der oberen linken Ecke.
- Direkte Eingabe des Ordnerpfads in der oberen linken Ecke und Drücken der Schaltfläche <kbd>Dateien durchsuchen</kbd>.

Sie können die Bedingungen für das Durchsuchen von Dateien unter <kbd>Einstellungen</kbd> > <kbd>Filesuche</kbd> festlegen.

## Direkt angeben

Geben Sie die Dateien, die umbenannt werden sollen, direkt an.

Es gibt zwei Möglichkeiten, die Dateien direkt anzugeben.

- Verwenden Sie den obigen Dialog <kbd>Dateien hinzufügen</kbd>, um die Datei auszuwählen.
- Ziehen Sie die Datei in die Dateiliste.

# Einstellungen umbenennen

Legen Sie die Bedingungen für das Umbenennen von Dateien fest.

Die Ausführungsreihenfolge der Bedingungen ist "Löschen" → "Ersetzen". Wenn Sie die Datei nach "Ersetzen" löschen möchten, lassen Sie <kbd>Text ersetzen</kbd> in <kbd>Ersetzungsmuster</kbd> leer, was dem "Löschen" entspricht.

## Texte löschen

Geben Sie die zu löschenden Zeichen an.

 <kbd>Hinzufügen</kbd> fügt eine neue Zeile ein. Sie können eine Zeile löschen, indem Sie auf die Schaltfläche am rechten Rand der Zeile klicken. Um die Reihenfolge der Zeilen zu ändern, greifen Sie den linken Rand der Zeile und ziehen Sie.

Sie können das "Common Pattern" auswählen, indem Sie auf die Schaltfläche unten rechts klicken.

Wenn das Kontrollkästchen "Regulärer Ausdruck" aktiviert ist, können Sie die zu löschenden Zeichen mithilfe eines regulären Ausdrucks angeben.

### Regulären Ausdrücke

Die regulären Ausdrücke (Regex) in dieser Software verwenden "Microsoft .NET" reguläre Ausdrücke. Im Folgenden finden Sie eine Liste mit typischen Ausdrücken. Eine ausführlichere Erklärung finden Sie unter den folgenden Links.

[Regex Quick Reference - Microsoft Docs ](https://docs.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference#character-escapes)

| Regex              | Beschreibung                                           | Muster | Eingabe   | Ausgabe      |
| ------------------ | ---------------------------------------------------------- | ------- | -------------- | ------------------ |
| `[`ch_group`]`       | Passt auf jedes einzelne Zeichen in *ch_group*. | [ae]    | gr**a**y, L**A**N**E** | gry,LN             |
| `[`first`-`last`]` | Zeichenbereich: Passt auf jedes einzelne Zeichen im Bereich von *first* bis *last*. | [A-Z]   | **R**ocky4 | ocky4           |
| `.`                | Platzhalterzeichen: Passt auf jedes einzelne Zeichen | a.e     | w**ate**r      | wr                 |
| `\w`                 | Entspricht einem beliebigen Wortzeichen | \\w | **A**.**a** **あ**~**ä**- | . ~- |
| `\s`               | Entspricht einem beliebigen Zeichen mit Leerzeichen | \\s     | A B　C | ABC    |
| `\d`               | Entspricht einer beliebigen Dezimalziffer | \\d     | Rocky**4** | Rocky |
| `^`              | Die Suche muss am Anfang des Dateinamens beginnen | ^r    | **r**ear rock | ear rock |
| `$`              | Übereinstimmung muss am Ende des Dateinamens stehen | $r      | rock rea**r** | rock rea |
| `*`            | Entspricht dem vorherigen Element null oder mehr Mal | o*r | d**oor**,**or**,o,l**r** | d,,o,l |
| `+`              | Entspricht dem vorhergehenden Element ein oder mehrere Male | o+r | d**oor**,**or**,o,lr | d,,o,lr |
| `?`              | Stimmt mit dem vorherigen Element null oder ein Mal überein | o?r  | do**or**,**or**,o,l**r** | do,,o,l |
| `{`n`}` | Passt genau *n* Mal auf das vorhergehende Element | [or]{2} | d**oo**r,**or**,o,lr | dr,,o,lr |
| `\`escape | Erkennen von Escape-Zeichen wie z. B. `. ` und `*` als normale Zeichen. | \\d\\.\\d | 1\_**2\.3**\_45           | 1__45    |


## Ersetzungsmuster

Gibt das Muster vor und nach der Ersetzung an.

 <kbd>Hinzufügen</kbd> fügt eine neue Zeile ein. Sie können eine Zeile löschen, indem Sie mit der rechten Taste in die Zeile klicken. Um die Reihenfolge der Zeilen zu ändern, greifen Sie den linken Rand der Zeile und ziehen Sie.

Sie können das "Common Pattern" auswählen, indem Sie auf die Schaltfläche unten rechts klicken.

Wenn <kbd>Regulärer Ausdruck</kbd> aktiviert ist, können Sie das Ersetzungsmuster mit einem regulären Ausdruck angeben.

### Regulärer Ausdruck

Der reguläre Ausdruck vor der Ersetzung ist der gleiche wie <kbd>Zu löschen Texte</kbd>.

Es gibt auch reguläre Ausdrücke, die nach der Ersetzung verwendet werden können. Einige der gebräuchlichsten sind unten aufgeführt.Eine genauere Erläuterung finden Sie unter den folgenden Links.

[Substitutions In Regular Expressions - Microsoft Docs ](https://docs.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions)

| Regex  | Beschreibung                                      | Zieltext | Text ersetzen | Eingabe        | Ausgabe          |
| ------ | ------------------------------------------------------------ | ----------- | ------------ | -------------- | ---------------- |
| `$0` | Include all matching strings in the replacement string       | ABC         |  \[\$0\]  |  x**ABC**x_AxBC | x**\[ABC\]**x_AxBC |
| `$num` | Includes the last substring matched by the capturing group that is identified by *num* | \\d\*\(\\d{3}\) | \$1       | A**0012** 34   | A**012** 34      |

### FileRenamerDiff Original Regex

Dies ist ein regulärer Ausdruck, der nur in dieser Anwendung vorkommt und nicht in "Microsoft .NET".

| Regex | Beschreibung          | Eingabe                | Ausgabe                    |
| -------- | ------------------------------------ | -------------- | ------------------ |
| `\u`       | Allen Text in Großbuchstaben umwandeln | l**ow** UPP P**as** | L**OW** UPP P**AS** |
| `\l`       | Allen Text in Kleinbuchstaben umwandeln | low **UPP** **P**as | low **upp** **p**as |
| `\h`       | Alle Buchstaben und Zahlen in Zeichen halber Breite umwandeln | Ha14 **Ｆｕ１７** | Ha14 **Fu17**      |
| `\f`       | Alle Buchstaben und Zahlen in Zeichen normaler Breite umwandeln | **Ha14** Ｆｕ１７ | **Ｈａ１４** Ｆｕ１７ |
| `\f`       | Konvertiere alle Katakana halber Breite in volle Breite | **ｱﾝﾊﾟﾝ ﾊﾞｲｷﾝ** | **アンパン バイキン** |
| `\n`       | Umlaute in Klarschriftzeichen umwandeln | s**üß** **Ö**L **Ä**ra | s**uess** **OE**L **Ae**ra |

# Bestätigung des Umbenennens

<kbd>Trockenlauf</kbd> ermöglicht es Ihnen, die Dateien vor und nach dem Umbenennen zu sehen.

Die Beschreibung der einzelnen Spalten in der Dateiliste lautet wie folgt.

| Spaltenname            | Angezeigter Inhalt                                           | Funktion                                                     |
| ---------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Löschen-Schaltfläche   | Löschen-Schaltfläche                                         | Löschen der Zeile. Die Schaltfläche in der Kopfzeile löscht alle Dateien. |
| Alter Dateiname        | Dateiname vor dem Umbenennen. Der geänderte Teil wird rosa hinterlegt. |                                                              |
| Neuer Dateiname        | Dateiname nach dem Umbenennen. Der geänderte Teil wird grün hinterlegt. |                                                              |
| Häkchen                | Ein Häkchen wird angezeigt, wenn es Änderungen gibt.         | Die Schaltfläche in der Kopfzeile macht die Datei-Liste nur mit Änderungen. |
| Mit Duplikat markieren | Wenn es Duplikate gibt, wird eine Duplikatmarkierung angezeigt. | Die Schaltfläche in der Kopfzeile bewirkt, dass die Dateiliste nur mit Duplikaten angezeigt wird. |
| Verzeichnis            | Verzeichnis, zu dem die Datei gehört                         | Anklicken, um die Datei im Explorer anzuzeigen               |
| Größe                  | Größe der Datei                                              |                                                              |
| Änderungsdatum         | Datum und Uhrzeit der Änderung der Datei                     |                                                              |
| Erstellungsdatum       | Datum und Uhrzeit der Erstellung der Datei                   |                                                              |

# Speichern umbenennen

<kbd>Umbenennen Speichern</kbd> wird die aktuelle Datei umbenennen. Kann nicht ausgeführt werden, wenn es Duplikate in der Dateiliste gibt.

Wenn das Verzeichnis, zu dem die Datei gehört, beim Umbenennen neu geschrieben wird, wird sie aus der Dateiliste entfernt.