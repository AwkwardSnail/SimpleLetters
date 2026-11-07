# SimpleLetters

A plain word processor: one A4 page, and you write on it. The only things you
can change are the margins and the font size.

Made by [aWkward sn@ail](https://awkwardsnail.com.au): calm, simple tools that
stay the way you left them.

## Get it

- **Windows:** coming soon to the Microsoft Store.
- **Build it yourself (free):** see below.

## Using it

- **Margins:** the four arrows around the page mark the margins. The two above
  the page are the left and right margins; the two beside it are the top and
  bottom. Click an arrow to show or hide a solid line where that margin is, and
  drag it to move the margin.
- **Text size:** the − and + buttons in the toolbar set the size of what you
  type next, from where the cursor is. Text already on the page keeps its size.
  Moving the cursor into existing text picks up that text's size.
- **Open, Save, Print, Undo, Redo:** toolbar buttons, or Ctrl+O, Ctrl+S, Ctrl+P,
  Ctrl+Z and Ctrl+Y. Undo goes back a word at a time.

## The .sletter format

Letters are saved as `.sletter` files: plain XML holding the margins and the
text with its sizes, so they're easy to read without SimpleLetters.

```xml
<sletter version="1">
  <page size="A4" units="mm" top="25" bottom="25" left="25" right="25" />
  <text>
    <p size="12">
      <run size="12">Dear Sam,</run>
    </p>
    <p size="12" />
    <p size="12">
      <run size="12">Thank you for </run>
      <run size="16">everything</run>
      <br size="12" />
    </p>
  </text>
</sletter>
```

`p` is a paragraph, `run` is text in one size (in points), and `br` is a line
break within a paragraph (Shift+Enter). SimpleLetters can also open plain `.txt`
files and save them as letters.

## Running

The Windows version is a .NET 10 WPF app in `desktop/SimpleLetters`:

```
dotnet run --project desktop/SimpleLetters
```

There's also an earlier browser version (`index.html`, `app.js`, `styles.css`)
with just margins and one text size for the whole page, and no `.sletter`
support. Serve the folder and open it:

```
python -m http.server 8174
```

## Font

Text is set in [Atkinson Hyperlegible Next](https://github.com/googlefonts/atkinson-hyperlegible-next),
designed by the Braille Institute for easy reading. It's bundled in `fonts/`
under the SIL Open Font License 1.1 (see [fonts/OFL.txt](fonts/OFL.txt)), so
the app never needs to download it.

## Privacy

SimpleLetters collects no data and never contacts an Awkward Snail server. Your
writing stays on your device. See the
[privacy policy](https://awkwardsnail.com.au/privacy.html).

## Licence

The code is under the [MIT licence](LICENSE), so you're welcome to fork it. The
Awkward Snail name and icon are not covered by the licence: please don't use them
for your fork. The bundled font keeps its own licence (SIL OFL 1.1).
