// Zenith Anti-PMO - Quick Popup Controller
// Reads storage state and coordinates quick launch actions.

document.addEventListener('DOMContentLoaded', () => {
  // Query storage for current metrics
  chrome.storage.local.get(['streakStartDate', 'urgesSurfed'], (data) => {
    const totalUrges = data.urgesSurfed || 0;
    const streakStart = data.streakStartDate;

    const urgesEl = document.getElementById('urges-surfed-val');
    if (urgesEl) urgesEl.textContent = totalUrges;

    const streakEl = document.getElementById('streak-days-val');
    if (streakEl) {
      if (streakStart) {
        const start = new Date(streakStart);
        const now = new Date();
        const diffTime = Math.abs(now - start);
        const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
        streakEl.textContent = diffDays;
      } else {
        streakEl.textContent = '0';
      }
    }
  });

  // Load current active tab URL for the Report button
  const reportLabel = document.getElementById('report-site-label');
  const reportBtn = document.getElementById('popup-report-btn');
  const reportConfirm = document.getElementById('report-confirm');
  let currentTabUrl = '';

  chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
    if (tabs && tabs[0] && tabs[0].url) {
      currentTabUrl = tabs[0].url;
      try {
        const urlObj = new URL(currentTabUrl);
        const hostname = urlObj.hostname.replace(/^www\./, '');
        if (reportLabel) reportLabel.textContent = hostname;

        // Disable report button if it's a chrome:// or extension page
        if (!currentTabUrl.startsWith('http')) {
          if (reportBtn) {
            reportBtn.disabled = true;
            reportBtn.style.opacity = '0.4';
          }
        }
      } catch (e) {
        if (reportLabel) reportLabel.textContent = 'Unknown';
      }
    }
  });

  // Report / block current site
  if (reportBtn) {
    reportBtn.addEventListener('click', () => {
      if (!currentTabUrl || !currentTabUrl.startsWith('http')) return;

      chrome.runtime.sendMessage({ type: 'REPORT_TAB', reportUrl: currentTabUrl }, () => {
        reportBtn.style.display = 'none';
        if (reportConfirm) {
          reportConfirm.style.display = 'block';
        }
      });
    });
  }

  // Action Buttons
  const emergencyBtn = document.getElementById('popup-emergency-btn');
  const dashboardBtn = document.getElementById('popup-dashboard-btn');

  if (emergencyBtn) {
    emergencyBtn.addEventListener('click', () => {
      const emergencyUrl = chrome.runtime.getURL('intervention/intervention.html?manual=true');
      chrome.tabs.create({ url: emergencyUrl });
      window.close();
    });
  }

  if (dashboardBtn) {
    dashboardBtn.addEventListener('click', () => {
      chrome.runtime.openOptionsPage();
      window.close();
    });
  }
});
