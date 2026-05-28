// Zenith Anti-PMO - Quick Popup Controller
// Reads storage state and coordinates quick launch actions.

document.addEventListener('DOMContentLoaded', () => {
  // Query storage for current metrics
  chrome.storage.local.get(['streakStartDate', 'urgesSurfed'], (data) => {
    const totalUrges = data.urgesSurfed || 0;
    const streakStart = data.streakStartDate;
    
    // Display total urges surfed
    const urgesEl = document.getElementById('urges-surfed-val');
    if (urgesEl) urgesEl.textContent = totalUrges;

    // Display clean streak days
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
