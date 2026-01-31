/**
 * Kalkimo Planner – Embed Placeholder
 *
 * This is a placeholder until the actual Vue.js embed bundle is built.
 * Run `npm run build:embed` in apps/frontend/ to generate the real bundle.
 */
(function () {
  'use strict';

  // Find all km-planner elements
  var elements = document.querySelectorAll('km-planner');

  elements.forEach(function (el) {
    var lang = el.getAttribute('lang') || 'de';
    var placeholder = document.createElement('div');
    placeholder.className = 'km-embed-placeholder';
    placeholder.innerHTML =
      '<div class="km-embed-placeholder-inner">' +
        '<div class="km-embed-placeholder-icon">' +
          '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="48" height="48">' +
            '<rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>' +
            '<line x1="8" y1="21" x2="16" y2="21"></line>' +
            '<line x1="12" y1="17" x2="12" y2="21"></line>' +
          '</svg>' +
        '</div>' +
        '<h3>' + (lang === 'de' ? 'Kalkimo Planner' : 'Kalkimo Planner') + '</h3>' +
        '<p>' + (lang === 'de'
          ? 'Der Immobilien-Investitionsrechner wird geladen...'
          : 'The real estate investment calculator is loading...') +
        '</p>' +
        '<p class="km-embed-placeholder-hint">' +
          (lang === 'de'
            ? 'Embed-Bundle noch nicht gebaut. Führe <code>npm run build:embed</code> im Frontend-Projekt aus.'
            : 'Embed bundle not yet built. Run <code>npm run build:embed</code> in the frontend project.') +
        '</p>' +
      '</div>';

    el.appendChild(placeholder);
  });

  console.info('[Kalkimo] Embed placeholder loaded. Build the actual bundle with: npm run build:embed');
})();
