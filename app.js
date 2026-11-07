// SimpleLetters: one A4 page. The only settings are the margins and the font size.
// Everything is kept in this browser's local storage; nothing leaves the device.

(function () {
  'use strict';

  const STORE_KEY = 'simpleletters.v1';

  const DEFAULTS = {
    text: '',
    margins: { top: 25, bottom: 25, left: 25, right: 25 }, // mm
    fontSize: 12 // pt
  };

  const page = document.getElementById('page');
  const text = document.getElementById('text');
  const overflowNote = document.getElementById('overflow');
  const inputs = {
    top: document.getElementById('m-top'),
    bottom: document.getElementById('m-bottom'),
    left: document.getElementById('m-left'),
    right: document.getElementById('m-right'),
    fontSize: document.getElementById('font-size')
  };

  function load() {
    try {
      const saved = JSON.parse(localStorage.getItem(STORE_KEY));
      if (saved) {
        return {
          text: typeof saved.text === 'string' ? saved.text : DEFAULTS.text,
          margins: Object.assign({}, DEFAULTS.margins, saved.margins),
          fontSize: saved.fontSize || DEFAULTS.fontSize
        };
      }
    } catch (e) { /* storage unavailable or corrupt: start fresh */ }
    return JSON.parse(JSON.stringify(DEFAULTS));
  }

  const state = load();

  let saveTimer = null;
  function save() {
    clearTimeout(saveTimer);
    saveTimer = setTimeout(() => {
      try { localStorage.setItem(STORE_KEY, JSON.stringify(state)); } catch (e) { /* ignore */ }
    }, 300);
  }

  function clamp(n, min, max) {
    return Math.min(max, Math.max(min, n));
  }

  // Read a number input, keeping the previous value if the field is empty or invalid.
  function readNumber(input, fallback) {
    const n = parseFloat(input.value);
    if (!isFinite(n)) return fallback;
    return clamp(n, parseFloat(input.min), parseFloat(input.max));
  }

  function apply() {
    const s = page.style;
    s.setProperty('--m-top', state.margins.top + 'mm');
    s.setProperty('--m-bottom', state.margins.bottom + 'mm');
    s.setProperty('--m-left', state.margins.left + 'mm');
    s.setProperty('--m-right', state.margins.right + 'mm');
    s.setProperty('--font-size', state.fontSize + 'pt');
    checkOverflow();
  }

  function checkOverflow() {
    overflowNote.hidden = text.scrollHeight <= text.clientHeight + 1;
  }

  // Fill the controls from the saved state.
  inputs.top.value = state.margins.top;
  inputs.bottom.value = state.margins.bottom;
  inputs.left.value = state.margins.left;
  inputs.right.value = state.margins.right;
  inputs.fontSize.value = state.fontSize;
  text.value = state.text;

  ['top', 'bottom', 'left', 'right'].forEach((side) => {
    const input = inputs[side];
    input.addEventListener('input', () => {
      state.margins[side] = readNumber(input, state.margins[side]);
      apply();
      save();
    });
    input.addEventListener('change', () => { input.value = state.margins[side]; });
  });

  inputs.fontSize.addEventListener('input', () => {
    state.fontSize = readNumber(inputs.fontSize, state.fontSize);
    apply();
    save();
  });
  inputs.fontSize.addEventListener('change', () => { inputs.fontSize.value = state.fontSize; });

  text.addEventListener('input', () => {
    state.text = text.value;
    checkOverflow();
    save();
  });

  // Recheck once the bundled font has loaded, since it changes line lengths.
  if (document.fonts && document.fonts.ready) document.fonts.ready.then(checkOverflow);

  apply();
  text.focus();
})();
