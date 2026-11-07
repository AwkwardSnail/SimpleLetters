# SimpleLetters

A plain word processor: one A4 page, and you write on it. The only things you
can change are the margins and the font size.

Made by [aWkward sn@ail](https://awkwardsnail.com.au): calm, simple tools that
stay the way you left them.

## Get it

- **Windows:** coming soon to the Microsoft Store.
- **Build it yourself (free):** see below.

## Running

The browser version is `index.html`, `app.js` and `styles.css`. Serve the folder
and open it:

```
python -m http.server 8174
```

Then open http://localhost:8174. Ctrl+P prints the page on A4.

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
