/**
 * Kalkimo Theme – Main JavaScript
 * Hamburger menu, header scroll shadow, mobile menu
 */
(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', init);

  function init() {
    initHamburgerMenu();
    initHeaderScroll();
  }

  /* ---------- Hamburger / Mobile Menu ---------- */
  function initHamburgerMenu() {
    var hamburger = document.querySelector('.km-hamburger');
    var mobileMenu = document.querySelector('.km-mobile-menu');
    var overlay = document.querySelector('.km-overlay');
    var closeBtn = document.querySelector('.km-mobile-menu-close');

    if (!hamburger || !mobileMenu || !overlay) return;

    function openMenu() {
      mobileMenu.classList.add('is-open');
      overlay.classList.add('is-open');
      document.body.classList.add('menu-open');
      hamburger.setAttribute('aria-expanded', 'true');
      // Focus the close button
      if (closeBtn) closeBtn.focus();
    }

    function closeMenu() {
      mobileMenu.classList.remove('is-open');
      overlay.classList.remove('is-open');
      document.body.classList.remove('menu-open');
      hamburger.setAttribute('aria-expanded', 'false');
      hamburger.focus();
    }

    hamburger.addEventListener('click', openMenu);

    if (closeBtn) {
      closeBtn.addEventListener('click', closeMenu);
    }

    overlay.addEventListener('click', closeMenu);

    // Close on ESC
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && mobileMenu.classList.contains('is-open')) {
        closeMenu();
      }
    });

    // Close on nav link click
    var mobileLinks = mobileMenu.querySelectorAll('.km-mobile-nav-link');
    mobileLinks.forEach(function (link) {
      link.addEventListener('click', closeMenu);
    });
  }

  /* ---------- Header Scroll Shadow ---------- */
  function initHeaderScroll() {
    var header = document.querySelector('.km-header');
    if (!header) return;

    var scrolled = false;

    function onScroll() {
      var shouldBeScrolled = window.scrollY > 10;
      if (shouldBeScrolled !== scrolled) {
        scrolled = shouldBeScrolled;
        header.classList.toggle('km-header--scrolled', scrolled);
      }
    }

    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll(); // Check initial state
  }
})();
