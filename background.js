// Zenith Anti-PMO - Background Service Worker (Manifest V3)
// Handles content filtering, incognito monitoring, streak calculation, and redirection.

importScripts('utils/words.js');

// Initialize settings and stats on install
chrome.runtime.onInstalled.addListener(() => {
  chrome.storage.local.get([
    'streakStartDate',
    'urgesSurfed',
    'journalEntries',
    'customBlockedSites',
    'safeRedirectUrl',
    'urgeHistory'
  ], (result) => {
    const updates = {};
    const nowStr = new Date().toISOString();

    if (!result.streakStartDate) {
      updates.streakStartDate = nowStr;
    }
    if (result.urgesSurfed === undefined) {
      updates.urgesSurfed = 0;
    }
    if (!result.journalEntries) {
      updates.journalEntries = [];
    }
    if (!result.customBlockedSites) {
      updates.customBlockedSites = [];
    }
    if (!result.safeRedirectUrl) {
      updates.safeRedirectUrl = 'https://www.google.com';
    }
    if (!result.urgeHistory) {
      // Structure: { dateStr: count }
      updates.urgeHistory = {};
    }

    chrome.storage.local.set(updates, () => {
      console.log('Zenith Anti-PMO initialized successfully.');
    });
  });
});

// Parse query parameter from URL
function getSearchQuery(urlString) {
  try {
    const url = new URL(urlString);
    const host = url.hostname.toLowerCase();
    
    // Check if it's a known search engine
    if (host.includes('google.') || host.includes('bing.com') || host.includes('duckduckgo.com') || host.includes('yahoo.com') || host.includes('ecosia.org')) {
      const searchParams = new URLSearchParams(url.search);
      // 'q' is standard for Google, Bing, DuckDuckGo, Ecosia. 'p' is for Yahoo.
      const query = searchParams.get('q') || searchParams.get('p') || '';
      return query.toLowerCase().trim();
    }
  } catch (e) {
    // Invalid URL
  }
  return '';
}

// Check if a URL matches any blocked criteria
async function checkUrlMatch(urlString) {
  try {
    const url = new URL(urlString);
    const host = url.hostname.toLowerCase();
    const path = url.pathname.toLowerCase();
    
    // 1. Check custom blocked sites
    const storage = await new Promise((resolve) => {
      chrome.storage.local.get(['customBlockedSites'], resolve);
    });
    const customBlocked = storage.customBlockedSites || [];
    
    for (const site of customBlocked) {
      const cleanSite = site.toLowerCase().trim();
      if (cleanSite && (host === cleanSite || host.endsWith('.' + cleanSite) || urlString.toLowerCase().includes(cleanSite))) {
        return { matched: true, type: 'custom', trigger: site };
      }
    }

    // 2. Check built-in blocked domains
    const dictionary = self.ZenithDictionary;
    if (dictionary) {
      for (const domain of dictionary.blockedDomains) {
        if (host === domain || host.endsWith('.' + domain)) {
          return { matched: true, type: 'domain', trigger: domain };
        }
      }

      // 3. Check blocked paths
      for (const blockedPath of dictionary.blockedPaths) {
        if (path.includes(blockedPath)) {
          return { matched: true, type: 'path', trigger: blockedPath };
        }
      }

      // 4. Check search query intentions
      const searchQuery = getSearchQuery(urlString);
      if (searchQuery) {
        // Split query into words to prevent partial match issues (e.g. "expensive" matching "sex")
        const queryWords = searchQuery.split(/\s+/);
        for (const keyword of dictionary.triggerKeywords) {
          // If keyword has spaces, do a direct substring check, otherwise check word-by-word
          if (keyword.includes(' ')) {
            if (searchQuery.includes(keyword)) {
              return { matched: true, type: 'search', trigger: keyword };
            }
          } else {
            if (queryWords.includes(keyword)) {
              return { matched: true, type: 'search', trigger: keyword };
            }
          }
        }
      }
    }
  } catch (e) {
    // Invalid URL
  }
  return { matched: false };
}

// Monitor navigation in real-time
chrome.webNavigation.onBeforeNavigate.addListener(async (details) => {
  // We only intercept main frame loads (tab changes, direct navigation)
  if (details.frameId !== 0) return;

  const url = details.url;
  // Ignore chrome:// pages, extension files, or local resources
  if (url.startsWith('chrome://') || url.startsWith('chrome-extension://') || url.startsWith('about:')) {
    return;
  }

  const match = await checkUrlMatch(url);
  if (match.matched) {
    console.log(`Zenith Intercepted: ${url} due to trigger: ${match.trigger} (${match.type})`);
    
    // Log the urge surfed in stats
    chrome.storage.local.get(['urgesSurfed', 'urgeHistory'], (data) => {
      const totalUrges = (data.urgesSurfed || 0) + 1;
      const history = data.urgeHistory || {};
      const todayStr = new Date().toISOString().split('T')[0];
      
      history[todayStr] = (history[todayStr] || 0) + 1;
      
      chrome.storage.local.set({
        urgesSurfed: totalUrges,
        urgeHistory: history
      });
    });

    // Determine redirection destination
    const interventionUrl = chrome.runtime.getURL(`intervention/intervention.html?trigger=${encodeURIComponent(match.trigger)}&original=${encodeURIComponent(url)}`);
    
    // Redirect the tab immediately
    chrome.tabs.update(details.tabId, { url: interventionUrl });
  }
});
