// Port Boss icon set v1: 24-unit grid, 1.75 stroke, round caps. Hand-drawn for this plan (no icon packs).
const P = {
  rod: '<path d="M4 20 L18 4"/><path d="M18 4 L20 3"/><path d="M18 4 C 21 9, 21 14, 19 17"/><circle cx="19" cy="18.5" r="1.5"/><circle cx="7.5" cy="16.5" r="2"/>',
  fish: '<path d="M2.5 12 C 5.5 7.2, 12 6.6, 16 10.8 L 20.8 7.4 C 20.1 10.2, 20.1 13.8, 20.8 16.6 L 16 13.2 C 12 17.4, 5.5 16.8, 2.5 12 Z"/><circle cx="6.6" cy="11.2" r=".9" fill="currentColor"/><path d="M9.4 9.2 C 10.4 10.8, 10.4 13.2, 9.4 14.8"/>',
  flare: '<path d="M3 8 H16.5 V12.5 H11.5 L10.3 18 H6.2 L7.2 12.5 H3 Z"/><path d="M16.5 9.2 H19"/><path d="M9.3 12.5 C 9.3 14, 10.6 14.6, 11.5 14"/><path d="M20.6 7.4 L22 6M21 10.3 H22.6"/>',
  shell: '<path d="M12 20 C 5 20, 3 13, 6 8 C 8 5, 16 5, 18 8 C 21 13, 19 20, 12 20 Z"/><path d="M12 20 V7"/><path d="M12 20 L8 8.5"/><path d="M12 20 L16 8.5"/><path d="M10 20 H14 V22 H10 Z"/>',
  sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2.5v2.5M12 19v2.5M2.5 12H5M19 12h2.5M5.3 5.3l1.8 1.8M16.9 16.9l1.8 1.8M5.3 18.7l1.8-1.8M16.9 7.1l1.8-1.8"/>',
  cloudsun: '<path d="M9 5.5 V4M4.5 9H3M5.6 5.6l1 1M13.4 5.6l-1 1"/><path d="M6.2 11.5 A3.5 3.5 0 0 1 12.6 8.6"/><path d="M7 19 H17.5 A3.5 3.5 0 0 0 17 12 A5 5 0 0 0 7.6 13.4 A2.8 2.8 0 0 0 7 19 Z"/>',
  cloud: '<path d="M6.5 18.5 H17.5 A4 4 0 0 0 17 10.6 A5.5 5.5 0 0 0 6.6 12 A3.3 3.3 0 0 0 6.5 18.5 Z"/>',
  rain: '<path d="M6.5 14.5 H17.5 A4 4 0 0 0 17 6.6 A5.5 5.5 0 0 0 6.6 8 A3.3 3.3 0 0 0 6.5 14.5 Z"/><path d="M8 17.5 L7 20M12 17.5 L11 20M16 17.5 L15 20"/>',
  fog: '<path d="M4 9 H20M3 13 H17M6 17 H21"/>',
  wind: '<path d="M3 9 H14 A2.5 2.5 0 1 0 11.5 6.5"/><path d="M3 13 H18 A2.5 2.5 0 1 1 15.5 15.5"/><path d="M3 17 H9"/>',
  storm: '<path d="M6.5 13.5 H17.5 A4 4 0 0 0 17 5.6 A5.5 5.5 0 0 0 6.6 7 A3.3 3.3 0 0 0 6.5 13.5 Z"/><path d="M12.5 13.5 L10 17.5 H13.5 L11.5 21.5"/>',
  msg: '<path d="M4 5.5 H20 V16 H10 L6 19.5 V16 H4 Z"/><path d="M8 9.5 H16M8 12.5 H13"/>',
  contract: '<path d="M7 4.5 H17 V20.5 H7 Z"/><path d="M9.5 3.5 H14.5 V6 H9.5 Z"/><path d="M9.5 10 H14.5M9.5 13 H14.5M9.5 16 H12.5"/>',
  crew: '<circle cx="9" cy="8.5" r="3"/><path d="M3.5 19.5 C 4 15.5, 14 15.5, 14.5 19.5"/><circle cx="16.5" cy="9.5" r="2.4"/><path d="M15.5 14.6 C 18.5 14.4, 20.4 16, 20.6 18.6"/>',
  map: '<path d="M3.5 6.5 L9 4.5 L15 6.5 L20.5 4.5 V17.5 L15 19.5 L9 17.5 L3.5 19.5 Z"/><path d="M9 4.5 V17.5M15 6.5 V19.5"/>',
  bank: '<path d="M3.5 9 L12 4.5 L20.5 9"/><path d="M5 9.5 V17M9.7 9.5 V17M14.3 9.5 V17M19 9.5 V17"/><path d="M3.5 19.5 H20.5"/>',
  gear: '<circle cx="12" cy="12" r="3"/><path d="M12 3.5 V6M12 18 V20.5M3.5 12 H6M18 12 H20.5M6 6 L7.8 7.8M16.2 16.2 L18 18M6 18 L7.8 16.2M16.2 7.8 L18 6"/>',
  book: '<path d="M4 5.5 C 7 4.5, 10 4.5, 12 6 C 14 4.5, 17 4.5, 20 5.5 V18.5 C 17 17.5, 14 17.5, 12 19 C 10 17.5, 7 17.5, 4 18.5 Z"/><path d="M12 6 V19"/>',
  anchor: '<circle cx="12" cy="5" r="2"/><path d="M12 7 V20"/><path d="M8 10.5 H16"/><path d="M4.5 13.5 C 5 17.5, 8 20, 12 20 C 16 20, 19 17.5, 19.5 13.5"/><path d="M3 15 L4.5 13 L6.5 14.5M21 15 L19.5 13 L17.5 14.5"/>',
  hook: '<path d="M12 3 V14 A4 4 0 1 1 6 11"/><path d="M6 11 L7.8 13.2"/><circle cx="12" cy="3" r=".1"/>',
  coin: '<circle cx="12" cy="12" r="8"/><path d="M14.5 9 C 13.8 8, 9.5 7.8, 9.5 10 C 9.5 12.5, 14.6 11.4, 14.6 14 C 14.6 16.2, 10.2 16, 9.4 15"/><path d="M12 6.5 V8M12 16 V17.5"/>',
  wave: '<path d="M2.5 14 C 5 10.5, 7.5 10.5, 10 14 C 12.5 17.5, 15 17.5, 17.5 14 C 19 12, 20 11.5, 21.5 11.5"/><path d="M2.5 19 C 5 16.5, 7.5 16.5, 10 19"/>',
  warn: '<path d="M12 4 L21 19.5 H3 Z"/><path d="M12 10 V14"/><circle cx="12" cy="16.8" r=".5" fill="currentColor"/>',
  check: '<path d="M5 12.5 L10 17 L19 7.5"/>',
  chev: '<path d="M9 5.5 L15.5 12 L9 18.5"/>',
  back: '<path d="M15 5.5 L8.5 12 L15 18.5"/>',
  lock: '<path d="M6.5 11 H17.5 V20 H6.5 Z"/><path d="M8.5 11 V8 A3.5 3.5 0 0 1 15.5 8 V11"/>',
  bed: '<path d="M3 18 V7M3 13 H21 V18"/><path d="M3 10.5 H8 A2 2 0 0 1 10 12.5 V13"/><path d="M10 13 V10.5 H18 A3 3 0 0 1 21 13.5"/>',
  boat: '<path d="M2.5 14 H21.5 L18.5 19 H6 Z"/><path d="M7 14 V10 H13 L15 14"/><path d="M9 10 V7"/><path d="M2 21.5 C 4 20.5, 6 20.5, 8 21.5 C 10 22.5, 12 22.5, 14 21.5 C 16 20.5, 18 20.5, 20 21.5"/>',
  pin: '<path d="M12 21 C 12 21, 5 14.5, 5 9.5 A7 7 0 0 1 19 9.5 C 19 14.5, 12 21, 12 21 Z"/><circle cx="12" cy="9.5" r="2.4"/>',
  clock: '<circle cx="12" cy="12" r="8.5"/><path d="M12 7.5 V12 L15 14"/>',
  plus: '<path d="M12 5 V19M5 12 H19"/>',
  minus: '<path d="M5 12 H19"/>',
  bait: '<path d="M7 5 C 11 4, 13 8, 10 10 C 7 12, 9 16, 13 15 C 16 14.3, 17 17, 15 19"/><circle cx="7" cy="5" r="1.3" fill="currentColor"/>',
  tackle: '<path d="M3.5 9 H20.5 V19 H3.5 Z"/><path d="M8.5 9 V6.5 H15.5 V9"/><path d="M3.5 13 H20.5"/><path d="M11 13 V15 H13 V13"/>',
  star: '<path d="M12 3.5 L14.6 8.9 L20.5 9.6 L16.1 13.6 L17.3 19.5 L12 16.5 L6.7 19.5 L7.9 13.6 L3.5 9.6 L9.4 8.9 Z"/>',
  sparkle: '<path d="M12 3 C 12.5 8.5, 15.5 11.5, 21 12 C 15.5 12.5, 12.5 15.5, 12 21 C 11.5 15.5, 8.5 12.5, 3 12 C 8.5 11.5, 11.5 8.5, 12 3 Z"/>',
  ice: '<path d="M12 3 V21M4.2 7.5 L19.8 16.5M4.2 16.5 L19.8 7.5"/><path d="M9.5 4.5 L12 6.5 L14.5 4.5M9.5 19.5 L12 17.5 L14.5 19.5"/>',
  signal: '<path d="M5 18 V15M9 18 V12M13 18 V9M17 18 V6"/>',
  battery: '<path d="M3.5 8 H18.5 V16 H3.5 Z"/><path d="M20.5 10.5 V13.5"/><path d="M5.5 10 H14 V14 H5.5 Z" fill="currentColor" stroke="none"/>',
  dollar: '<path d="M15.5 7.5 C 14.5 6, 8.5 5.8, 8.5 9 C 8.5 12.5, 15.8 11, 15.8 15 C 15.8 18.2, 9.5 18, 8.2 16.2"/><path d="M12 4 V6M12 18 V20"/>',
  squid: '<path d="M12 3 C 8 6, 7.5 10, 8.5 13 H15.5 C 16.5 10, 16 6, 12 3 Z"/><path d="M9 13 C 8.5 16, 7 18, 6 20M11 13 C 11 16, 10.5 18.5, 10 21M13 13 C 13 16, 13.5 18.5, 14 21M15 13 C 15.5 16, 17 18, 18 20"/><circle cx="10.5" cy="10" r=".7" fill="currentColor"/><circle cx="13.5" cy="10" r=".7" fill="currentColor"/>',
  people: '<circle cx="12" cy="8" r="3.5"/><path d="M5 20 C 5.5 14.5, 18.5 14.5, 19 20"/>',
};
(function () {
  const s = Object.entries(P).map(([k, v]) => `<symbol id="i-${k}" viewBox="0 0 24 24">${v}</symbol>`).join('');
  const d = document.createElement('div');
  d.style.display = 'none';
  d.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg">${s}</svg>`;
  document.body.prepend(d);
  document.querySelectorAll('[data-i]').forEach(el => {
    el.outerHTML = `<svg class="i ${el.className || ''}" style="${el.getAttribute('style') || ''}"><use href="#i-${el.dataset.i}"/></svg>`;
  });
})();
