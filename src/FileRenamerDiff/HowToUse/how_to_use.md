% How To Use
% soi
% 2021/02/08

[View Online](https://github.com/soi013/FileRenamerDiff/blob/master/src/FileRenamerDiff/HowToUse/how_to_use.md)

# Quick 

1. Target file specification: You can drag and drop files into the file list or search for <kbd>Search Files</kbd> from a specific directory.
2. Set rename pattern: Set the Delete/Replace pattern from <kbd>Settings</kbd>.
3. Confirm rename: Execute <kbd>DRY RUN</kbd> to confirm the renaming.
4. Execute rename: Execute <kbd>SAVE</kbd> to rename the actual file.

# Specify the target file

You can specify the target file to be renamed in two ways: "File Search" or "Direct Specification".

To remove a file from the target file list, click the button on the left side of the line. You can also empty the file list by clicking the upper left button.

## File Search

Search for files in any folder and add them to the target file list.

There are three ways to search for files.

- Select from the folder icon in the upper left corner using the dialog.
- Dragging a folder to the folder path in the upper left corner.
- Directly enter the folder path in the upper left corner and press the <kbd>File Search</kbd> button.

You can specify the conditions for file search from <kbd>Settings</kbd> > <kbd>File Search</kbd>.

## Directly specify

Directly specify the files to be renamed.

There are two ways to specify directly.

- Use the <kbd>Add Files</kbd> dialog above to select the file.
- Drag the file to the file list.

# Rename settings

Specify the conditions for file renaming.

The execution order of the conditions is "Delete" → "Replace". If you want to delete the file after "Replace", leave <kbd>Replace Text</kbd> empty in <kbd>Replace Pattern</kbd>, which is the same as "Delete".

## Delete Texts

Specify the characters to be deleted.

 <kbd>ADD</kbd> adds a new line. You can delete a line by clicking the button on the right side of the line. To change the order of the lines, grab the left edge of the line and drag.

You can select the "Common Pattern" by clicking the bottom right button.

If the Regular Expression checkbox is checked, you can specify the characters to be deleted using regular expression.

### Regular Expression

The regular expressions(Regex) in this software use "Microsoft .NET" regular expressions. The following is a list of typical ones. For a more detailed explanation, please refer to the following links.

[Regex Quick Reference - Microsoft Docs ](https://docs.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference#character-escapes)

| Regex              | description                                                       | pattern | Input          | Output             |
| ------------------ | ---------------------------------------------------------- | ------- | -------------- | ------------------ |
| `[`ch_group`]`       | Matches any single character in *ch_group*. | [ae]    | Gr**a**y,S**ea**,Gr**ee**n | Gry,S,Grn  |
| `[`first`-`last`]` | Character range: Matches any single character in the range from *first* to *last*. | [A-Z]   | **R**ocky4 | ocky4           |
| `.`                | Wildcard: Matches any single character        | a.e     | w**ate**r      | wr                 |
| `\w`                 | Matches any word character      | \\w | **A**.**a** **あ**~**ä**- | . ~- |
| `\s`               | Matches any white-space character                  | \\s     | A B　C | ABC    |
| `\d`               | Matches any decimal digit                              | \\d     | Rocky**4** | Rocky |
| `^`              | Match must start at the beginning of the file name | ^r    | **r**ear rock | ear rock |
| `$`              | Match must occur at the end of the file name | r$      | rock rea**r** | rock rea |
| `*`            | Matches the previous element zero or more times | o*r | d**oor**,**or**,o,l**r** | d,,o,l |
| `+`              | Matches the previous element one or more time | o+r | d**oor**,**or**,o,lr | d,,o,lr |
| `?`              | Matches the previous element zero or one time | o?r  | do**or**,**or**,o,l**r** | do,,o,l |
| `{`n`}` | Matches the previous element exactly *n* times | [or]{2} | d**oo**r,**or**,o,lr | dr,,o,lr |
| `\`escape | Recognize escape characters such as `. ` and `*` as normal characters. | \\d\\.\\d | 1\_**2\.3**\_45           | 1__45    |

## Replace Pattern

Specifies the pattern before and after the replacement.

 <kbd>ADD</kbd> will add a new line. You can delete a row by clicking the right button in the row. To change the order of the lines, grab the left edge of the line and drag.

You can select the "Common Pattern" by clicking the bottom right button.

If <kbd>Regular Expression</kbd> is checked, you can specify the replacement pattern using regular expression.

### Regular Expression

The regular expression before replacement is the same as <kbd>Delete Texts</kbd>.

There are also regular expression that can be used after the replacement. Some of the most common are listed below. For a more detailed explanation, please refer to the following links

[Substitutions In Regular Expressions - Microsoft Docs ](https://docs.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions)

| Regex  | Description                                                  | Target Text | Replace Text | Input          | Output           |
| ------ | ------------------------------------------------------------ | ----------- | ------------ | -------------- | ---------------- |
| ` $0 ` | Include all matching strings in the replacement string | ABC         |  X$0X  | abc **ABC** AnBC | abc **XABCX** AnBC |
| ` $num ` | Includes the last substring matched by the capturing group that is identified by *num* | \\d\*\(\\d{3}\) | \$1       | A**0012** 34   | A**012** 34      |

### FileRenamerDiff Original Regex

This is a regular expression unique to this application that is not found in the "Microsoft .NET".

| Regex | Description                      | Input          | Output             |
| -------- | ------------------------------------ | -------------- | ------------------ |
| `\u`       | Convert all text to upper case | l**ow** UPP P**as** | L**OW** UPP P**AS** |
| `\l`       | Convert all text to lower case | low **UPP** **P**as | low **upp** **p**as |
| `\h`       | Convert all letters and numbers to Half-width characters | Ha14 **Ｆｕ１７** | Ha14 **Fu17**      |
| `\f`       | Convert all letters and numbers to Full-width characters | **Ha14** Ｆｕ１７ | **Ｈａ１４** Ｆｕ１７ |
| `\f`       | Convert all Half-width Katakana to full-width | **ｱﾝﾊﾟﾝ ﾊﾞｲｷﾝ** | **アンパン バイキン** |
| `\n`       | Convert umlauts to plain-ascii characters | s**üß** **Ö**L **Ä**ra | s**uess** **OE**L **Ae**ra |

# Rename confirmation

 <kbd>DRY RUN</kbd> allows you to see the files before and after they are renamed.

The description of each column in the file list is as follows.

| Column name         | Displayed contents                                           | Function                                                     |
| ------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Delete button       | Delete button                                                | Delete the line. The button in the header deletes all files. |
| OLD File Name       | File name before renaming. The changed part becomes a pink background. |                                                              |
| NEW File Name       | File name after renaming. The changed part becomes a green background. |                                                              |
| Mark with change    | A check mark is displayed if there are any changes.          | The button in the header makes the file list only with changes. |
| Mark with duplicate | If there are duplicates, a duplicate mark is displayed.      | The button in the header makes the file list only with duplicates. |
| Directory           | Directory to which the file belongs                          | Click to display the file in the explorer                    |
| Size                | Size of the file                                             |                                                              |
| Date modified       | Date and time of file modification                           |                                                              |
| Created Date        | Date and time of file creation                               |                                                              |



# Save rename

<kbd>SAVE</kbd> will rename the actual file. Cannot be executed if there are duplicates in the file list.

If the directory to which the file belongs is rewritten when renaming is executed, it will be removed from the file list.

