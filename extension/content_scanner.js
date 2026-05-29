// Zenith Anti-PMO - DOM Content Scanner (Content Script)
// Runs on every HTTP/HTTPS page. Scans the rendered DOM for adult content indicators
// that wouldn't be caught by URL/domain blocking alone (e.g. mixed-content sites
// like Toongod, Toomics, Webtoons, Tapas that have both safe and 18+ sections).

(function () {
  'use strict';

  // Don't run on Zenith's own pages
  if (window.location.href.includes('chrome-extension://')) return;

  // --- Indicator patterns to scan for in the DOM ---

  // Text strings that commonly appear as 18+ badges, age gates, category labels
  const ADULT_TEXT_INDICATORS = [
    '18+', '18 +', 'r-18', 'r18', 'adult only', 'adults only',
    'mature content', 'mature readers', 'explicit content',
    'age-restricted', 'age restricted', 'age verification',
    'you must be 18', 'for adults only', 'adult content',
    'nsfw content', '19+', 'nc-17', 'rated x',
    // Manhwa-specific labels
    'adult manhwa', 'mature manhwa', 'hentai manhwa',
    // Indonesian
    'konten dewasa', 'khusus dewasa', 'untuk dewasa'
  ];

  // CSS class/id name fragments that adult sites use for badges
  const ADULT_CLASS_PATTERNS = [
    'adult', 'mature', 'nsfw', 'r18', 'erotica', 'explicit',
    'age-gate', 'age-verify', 'content-warning'
  ];

  // Meta tag content strings
  const ADULT_META_VALUES = [
    'adult', 'mature', 'RTA-5042-1996-1400-1577-RTA', // RTA label
    'rta', 'x-rated'
  ];

  let triggered = false;

  function sendInterventionSignal(reason) {
    if (triggered) return;
    triggered = true;
    console.log(`[Zenith DOM Scanner] Trigger detected: ${reason}`);
    // Send message to background to handle the redirect (avoids CSP issues)
    chrome.runtime.sendMessage({
      type: 'DOM_TRIGGER',
      trigger: reason,
      url: window.location.href
    });
  }

  // --- 1. Scan meta tags (fastest, runs first) ---
  function scanMetaTags() {
    const metas = document.querySelectorAll('meta');
    for (const meta of metas) {
      const name = (meta.getAttribute('name') || meta.getAttribute('property') || '').toLowerCase();
      const content = (meta.getAttribute('content') || '').toLowerCase();

      // RTA (Restricted To Adults) meta label — industry standard
      if (name === 'rating' || name === 'dc.audience') {
        for (const val of ADULT_META_VALUES) {
          if (content.includes(val)) {
            sendInterventionSignal(`meta[${name}]="${content}"`);
            return true;
          }
        }
      }
      // Check RTA label directly
      if (content.includes('rta-5042-1996-1400-1577-rta')) {
        sendInterventionSignal('RTA label detected');
        return true;
      }
    }
    return false;
  }

  // --- 2. Scan page title ---
  function scanPageTitle() {
    const title = document.title.toLowerCase();
    for (const indicator of ADULT_TEXT_INDICATORS) {
      if (title.includes(indicator)) {
        sendInterventionSignal(`title contains "${indicator}"`);
        return true;
      }
    }
    return false;
  }

  // --- 3. Scan DOM text nodes for adult badges/labels ---
  function scanDOMText() {
    // Only scan specific high-signal elements — not the entire body text
    // (to avoid false positives from articles discussing these topics)
    const selectors = [
      '.badge', '.tag', '.label', '.genre', '.category', '.rating',
      '[class*="badge"]', '[class*="tag"]', '[class*="genre"]',
      '[class*="category"]', '[class*="rating"]', '[class*="age"]',
      '[class*="mature"]', '[class*="adult"]', '[class*="nsfw"]',
      'h1', 'h2', '.title', '.content-type', '.content-label',
      'header', 'nav'
    ].join(', ');

    let elements;
    try {
      elements = document.querySelectorAll(selectors);
    } catch (e) {
      return false;
    }

    for (const el of elements) {
      const text = el.textContent.toLowerCase().trim();
      if (!text || text.length > 200) continue; // Skip large blocks

      for (const indicator of ADULT_TEXT_INDICATORS) {
        if (text.includes(indicator)) {
          sendInterventionSignal(`DOM element contains "${indicator}"`);
          return true;
        }
      }
    }
    return false;
  }

  // --- 4. Scan class and ID names of visible elements ---
  function scanClassNames() {
    const allElements = document.querySelectorAll('[class], [id]');
    for (const el of allElements) {
      const classes = (el.className || '').toString().toLowerCase();
      const id = (el.id || '').toLowerCase();
      const combined = classes + ' ' + id;

      for (const pattern of ADULT_CLASS_PATTERNS) {
        // Word-boundary check to avoid false positives like "manufacturer"
        const regex = new RegExp(`(^|[^a-z])${pattern}([^a-z]|$)`);
        if (regex.test(combined)) {
          sendInterventionSignal(`element class/id contains "${pattern}"`);
          return true;
        }
      }
    }
    return false;
  }

  // --- Run scanners in order of cost (cheapest first) ---
  function runScan() {
    if (scanMetaTags()) return;
    if (scanPageTitle()) return;
    if (scanDOMText()) return;
    // Class scan is most expensive — only run if others pass
    scanClassNames();
  }

  // Run immediately on document_idle (DOM ready)
  runScan();

  // Also observe DOM mutations for dynamically loaded content (SPA page navigation)
  const observer = new MutationObserver(() => {
    if (!triggered) runScan();
  });

  observer.observe(document.body || document.documentElement, {
    childList: true,
    subtree: true
  });

  // Stop observing if already triggered
  if (triggered) observer.disconnect();

})();
