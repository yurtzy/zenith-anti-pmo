// Zenith Anti-PMO - Background Service Worker (Manifest V3)
// Handles content filtering, incognito monitoring, streak calculation, and redirection.

importScripts('utils/words.js');

// Milestone thresholds in days
const MILESTONES = [7, 14, 30, 60, 90, 180, 365];

// Initialize settings and stats on install
chrome.runtime.onInstalled.addListener(() => {
  chrome.storage.local.get([
    'streakStartDate',
    'urgesSurfed',
    'journalEntries',
    'customBlockedSites',
    'safeRedirectUrl',
    'urgeHistory',
    'streakHistory',
    'relapseHistory',
    'lastMilestoneNotified'
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
      updates.urgeHistory = {};
    }
    if (!result.streakHistory) {
      updates.streakHistory = [];
    }
    if (!result.relapseHistory) {
      updates.relapseHistory = [];
    }
    if (result.lastMilestoneNotified === undefined) {
      updates.lastMilestoneNotified = 0;
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

    if (host.includes('google.') || host.includes('bing.com') || host.includes('duckduckgo.com') || host.includes('yahoo.com') || host.includes('ecosia.org')) {
      const searchParams = new URLSearchParams(url.search);
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
        const queryWords = searchQuery.split(/\s+/);
        for (const keyword of dictionary.triggerKeywords) {
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

// Check and fire milestone notifications
function checkMilestones(streakDays) {
  chrome.storage.local.get(['lastMilestoneNotified'], (data) => {
    const lastNotified = data.lastMilestoneNotified || 0;

    // Find the highest milestone reached that hasn't been notified yet
    let nextMilestone = null;
    for (const m of MILESTONES) {
      if (streakDays >= m && m > lastNotified) {
        nextMilestone = m;
      }
    }

    if (nextMilestone !== null) {
      chrome.storage.local.set({ lastMilestoneNotified: nextMilestone });

      chrome.notifications.create({
        type: 'basic',
        iconUrl: 'icons/icon128.png',
        title: 'Zenith — Milestone Reached',
        message: `${nextMilestone} days clean. Discipline compounds. Keep going.`,
        priority: 2
      });
    }
  });
}

// Monitor navigation — using onCommitted for reliable redirect (fires after navigation commits)
chrome.webNavigation.onCommitted.addListener(async (details) => {
  // Only intercept main frame loads
  if (details.frameId !== 0) return;

  const url = details.url;

  const match = await checkUrlMatch(url);
  if (match.matched) {
    console.log(`Zenith Intercepted: ${url} due to trigger: ${match.trigger} (${match.type})`);

    // Log the urge surfed in stats
    chrome.storage.local.get(['urgesSurfed', 'urgeHistory', 'streakStartDate'], (data) => {
      const totalUrges = (data.urgesSurfed || 0) + 1;
      const history = data.urgeHistory || {};
      const todayStr = new Date().toISOString().split('T')[0];

      history[todayStr] = (history[todayStr] || 0) + 1;

      chrome.storage.local.set({
        urgesSurfed: totalUrges,
        urgeHistory: history
      });

      // Check milestones on every intercept (low cost)
      if (data.streakStartDate) {
        const start = new Date(data.streakStartDate);
        const now = new Date();
        const diffDays = Math.floor((now - start) / (1000 * 60 * 60 * 24));
        checkMilestones(diffDays);
      }
    });

    // Redirect the tab
    const interventionUrl = chrome.runtime.getURL(`intervention/intervention.html?trigger=${encodeURIComponent(match.trigger)}&original=${encodeURIComponent(url)}`);
    chrome.tabs.update(details.tabId, { url: interventionUrl });
  }
}, {
  // Only fire on http/https — skips chrome://, file://, chrome-extension:// at the platform level
  url: [{ schemes: ['http', 'https'] }]
});

// Handle DOM_TRIGGER messages from content_scanner.js
// The content script cannot do chrome.tabs.update directly, so it sends a message here.
chrome.runtime.onMessage.addListener((message, sender) => {
  if (message.type === 'DOM_TRIGGER' && sender.tab) {
    const tabId = sender.tab.id;
    const trigger = message.trigger || 'dom-content-scan';
    const url = message.url || sender.tab.url || '';

    console.log(`[Zenith] DOM scanner trigger on tab ${tabId}: ${trigger}`);

    // Log the event
    chrome.storage.local.get(['urgesSurfed', 'urgeHistory'], (data) => {
      const totalUrges = (data.urgesSurfed || 0) + 1;
      const history = data.urgeHistory || {};
      const todayStr = new Date().toISOString().split('T')[0];
      history[todayStr] = (history[todayStr] || 0) + 1;
      chrome.storage.local.set({ urgesSurfed: totalUrges, urgeHistory: history });
    });

    const interventionUrl = chrome.runtime.getURL(
      `intervention/intervention.html?trigger=${encodeURIComponent(trigger)}&original=${encodeURIComponent(url)}`
    );
    chrome.tabs.update(tabId, { url: interventionUrl });
  }

  // Handle 'report-current-tab' from popup — adds the current tab URL to customBlockedSites
  if (message.type === 'REPORT_TAB' && sender.tab === undefined) {
    // This comes from popup, so we need the tab info passed in the message
    const { reportUrl } = message;
    if (!reportUrl) return;

    try {
      const urlObj = new URL(reportUrl);
      const hostname = urlObj.hostname.toLowerCase().replace(/^www\./, '');

      chrome.storage.local.get(['customBlockedSites'], (data) => {
        const sites = data.customBlockedSites || [];
        if (!sites.includes(hostname)) {
          sites.push(hostname);
          chrome.storage.local.set({ customBlockedSites: sites });
        }
      });
    } catch (e) {
      // Invalid URL
    }
  }
});
