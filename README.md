# SimpleLetters

A plain word processor: one A4 page, and you write on it. You can change the
margins, the text size, plain or serif letters, and bold, italic and underline.
That's all.

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
- **Text size:** the − and + buttons in the toolbar. With text selected, they
  resize the selection; with nothing selected, they set the size of what you
  type next and the text already on the page keeps its size.
- **Sans or Serif:** one button shows the font in use, Sans (Atkinson
  Hyperlegible Next) or Serif (Literata); press it to switch. New letters start
  in the last one you used.
- **Bold, italic, underline:** the B, I and U buttons, or Ctrl+B, Ctrl+I and
  Ctrl+U.
- With text selected, all of these change the selection (one undo step each);
  with nothing selected, they apply to what you type next. Moving the cursor
  into existing text picks up its look, and the buttons show it.
- **Zoom:** makes the page bigger or smaller on screen (or hold Ctrl and turn
  the mouse wheel). It doesn't change the letter or how it prints.
- **Open, Save, Print, Undo, Redo:** toolbar buttons, or Ctrl+O, Ctrl+S, Ctrl+P,
  Ctrl+Z and Ctrl+Y. Undo goes back a word at a time.

## The .sletter format

Letters are saved as `.sletter` files: plain XML holding the margins and the
text with its sizes and styles, so they're easy to read without SimpleLetters.

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
      <run size="16" font="serif" bold="true">everything</run>
      <br size="12" />
    </p>
  </text>
</sletter>
```

`p` is a paragraph, `run` is text in one size (in points), and `br` is a line
break within a paragraph (Shift+Enter). A `run` can also have `font="serif"`,
`bold="true"`, `italic="true"` and `underline="true"`; without them it is plain
Sans. Older `.sletter` files open unchanged. SimpleLetters can also open plain `.txt`
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

Sans text is set in [Atkinson Hyperlegible Next](https://github.com/googlefonts/atkinson-hyperlegible-next),
designed by the Braille Institute for easy reading. Serif text is set in
[Literata](https://github.com/googlefonts/literata), designed for long reading
on screen and in print. Both are under the SIL Open Font License 1.1 and are
bundled with the app (see `desktop/SimpleLetters/Fonts/OFL.txt` and
`OFL-Literata.txt`), so it never needs to download them.

The browser version is the earlier, simpler one: one font and text size for the
whole page, without zoom, styles or `.sletter` files.

## Privacy

SimpleLetters collects no data and never contacts an Awkward Snail server. Your
writing stays on your device. See the
[privacy policy](https://awkwardsnail.com.au/privacy.html).

## Licence

The code is under the [MIT licence](LICENSE), so you're welcome to fork it. The
Awkward Snail name and icon are not covered by the licence: please don't use them
for your fork. The bundled font keeps its own licence (SIL OFL 1.1).
