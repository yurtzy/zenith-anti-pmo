// Zenith Anti-PMO - Dashboard Controller
// Dynamic UI handling, HTML5 Canvas chart drawing, storage management, and incognito status checking.

document.addEventListener('DOMContentLoaded', () => {
  // Navigation Tabs Logic
  const navBtns = document.querySelectorAll('.nav-btn');
  const tabContents = document.querySelectorAll('.tab-content');

  navBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const tabId = btn.getAttribute('data-tab');

      navBtns.forEach(b => b.classList.remove('active'));
      tabContents.forEach(c => c.classList.remove('active'));

      btn.classList.add('active');
      const targetTab = document.getElementById(tabId);
      if (targetTab) {
        targetTab.classList.add('active');
      }

      if (tabId === 'overview') {
        loadOverviewData();
      }
      if (tabId === 'shield') {
        loadSafeRedirectUrl();
      }
    });
  });

  // Journal Intensity Slider helper
  const slider = document.getElementById('journal-intensity');
  const sliderVal = document.getElementById('intensity-val');
  if (slider && sliderVal) {
    slider.addEventListener('input', () => {
      sliderVal.textContent = slider.value;
    });
  }

  // Relapse Modal Toggle
  const resetBtn = document.getElementById('reset-streak-btn');
  const modal = document.getElementById('relapse-modal');
  const cancelResetBtn = document.getElementById('cancel-reset-btn');
  const confirmResetBtn = document.getElementById('confirm-reset-btn');

  if (resetBtn && modal) {
    resetBtn.addEventListener('click', () => modal.classList.add('open'));
  }
  if (cancelResetBtn && modal) {
    cancelResetBtn.addEventListener('click', () => modal.classList.remove('open'));
  }

  if (modal) {
    modal.addEventListener('click', (e) => {
      if (e.target === modal) modal.classList.remove('open');
    });
  }

  if (confirmResetBtn) {
    confirmResetBtn.addEventListener('click', () => {
      const nowStr = new Date().toISOString();

      // Archive previous streak before resetting
      chrome.storage.local.get(['streakStartDate', 'streakHistory'], (data) => {
        const streakHistory = data.streakHistory || [];
        if (data.streakStartDate) {
          const start = new Date(data.streakStartDate);
          const end = new Date();
          const days = Math.floor((end - start) / (1000 * 60 * 60 * 24));
          streakHistory.push({ start: data.streakStartDate, end: nowStr, days });
        }

        chrome.storage.local.set({
          streakStartDate: nowStr,
          streakHistory,
          lastMilestoneNotified: 0
        }, () => {
          modal.classList.remove('open');
          loadOverviewData();
        });
      });
    });
  }

  // Emergency Urge button click
  const emergencyBtn = document.getElementById('emergency-btn');
  if (emergencyBtn) {
    emergencyBtn.addEventListener('click', () => {
      const emergencyUrl = chrome.runtime.getURL('intervention/intervention.html?manual=true');
      chrome.tabs.create({ url: emergencyUrl });
    });
  }

  // Load All Data
  loadOverviewData();
  loadJournalEntries();
  loadCustomBlockRules();
  checkIncognitoStatus();
  loadSafeRedirectUrl();

  // Handle Journal Submission
  const journalForm = document.getElementById('journal-form');
  if (journalForm) {
    journalForm.addEventListener('submit', (e) => {
      e.preventDefault();

      const trigger = document.getElementById('journal-trigger').value;
      const intensity = document.getElementById('journal-intensity').value;
      const notes = document.getElementById('journal-notes').value;
      const nowStr = new Date().toISOString();

      const newEntry = {
        date: nowStr,
        trigger: trigger,
        intensity: parseInt(intensity),
        notes: notes
      };

      chrome.storage.local.get(['journalEntries'], (data) => {
        const entries = data.journalEntries || [];
        entries.unshift(newEntry);
        chrome.storage.local.set({ journalEntries: entries }, () => {
          journalForm.reset();
          if (sliderVal) sliderVal.textContent = '5';
          loadJournalEntries();
        });
      });
    });
  }

  // Handle Custom Block Rule Submission
  const rulesForm = document.getElementById('rules-form');
  if (rulesForm) {
    rulesForm.addEventListener('submit', (e) => {
      e.preventDefault();
      const input = document.getElementById('custom-site-input');
      const rawSite = input.value.trim().toLowerCase();

      if (!rawSite) return;

      chrome.storage.local.get(['customBlockedSites'], (data) => {
        const sites = data.customBlockedSites || [];
        if (!sites.includes(rawSite)) {
          sites.push(rawSite);
          chrome.storage.local.set({ customBlockedSites: sites }, () => {
            input.value = '';
            loadCustomBlockRules();
          });
        }
      });
    });
  }

  // Handle Safe Redirect URL form
  const safeRedirectForm = document.getElementById('safe-redirect-form');
  if (safeRedirectForm) {
    safeRedirectForm.addEventListener('submit', (e) => {
      e.preventDefault();
      const input = document.getElementById('safe-redirect-input');
      const url = input.value.trim();
      if (!url) return;

      chrome.storage.local.set({ safeRedirectUrl: url }, () => {
        const savedMsg = document.getElementById('safe-redirect-saved');
        if (savedMsg) {
          savedMsg.style.display = 'block';
          setTimeout(() => { savedMsg.style.display = 'none'; }, 2000);
        }
      });
    });
  }
});

// Load current safe redirect URL into the input
function loadSafeRedirectUrl() {
  chrome.storage.local.get(['safeRedirectUrl'], (data) => {
    const input = document.getElementById('safe-redirect-input');
    if (input && data.safeRedirectUrl) {
      input.value = data.safeRedirectUrl;
    }
  });
}

// Calculate streak details and paint UI
function loadOverviewData() {
  chrome.storage.local.get(['streakStartDate', 'urgesSurfed', 'urgeHistory', 'streakHistory'], (data) => {
    const totalUrges = data.urgesSurfed || 0;
    document.getElementById('total-urges').textContent = totalUrges;

    // Calculate Streak days
    const startDate = data.streakStartDate ? new Date(data.streakStartDate) : new Date();
    const now = new Date();
    const diffTime = Math.abs(now - startDate);
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));

    document.getElementById('streak-days').textContent = diffDays;

    // Streak Circular Progress Ring
    // Radius = 70. Circumference = 2 * PI * 70 = 439.82
    const ring = document.getElementById('streak-ring');
    if (ring) {
      const circ = 439.82;
      ring.style.strokeDasharray = `${circ} ${circ}`;

      const progress = (diffDays % 30) / 30;
      const displayProgress = (diffDays > 0 && diffDays % 30 === 0) ? 1.0 : (progress === 0 ? 0.02 : progress);
      const offset = circ - (displayProgress * circ);
      ring.style.strokeDashoffset = offset;
    }

    // Urge History Analytics calculations
    const history = data.urgeHistory || {};
    const dates = Object.keys(history).sort();

    // Daily Average
    let dailyAvg = 0;
    if (dates.length > 0) {
      let sum = 0;
      dates.forEach(d => sum += history[d]);
      dailyAvg = (sum / dates.length).toFixed(1);
    }
    document.getElementById('daily-avg').textContent = dailyAvg;

    // Peak Urge Day
    let peakCount = 0;
    let peakDay = 'None';
    dates.forEach(d => {
      if (history[d] > peakCount) {
        peakCount = history[d];
        const dayObj = new Date(d);
        peakDay = dayObj.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' }) + ` (${peakCount})`;
      }
    });
    document.getElementById('peak-day').textContent = peakDay;

    // Draw chart
    drawUrgeChart(history);

    // Render past streaks table
    renderStreakHistory(data.streakHistory || []);
  });
}

// Render past streaks history table
function renderStreakHistory(history) {
  const tbody = document.getElementById('streak-history-list');
  if (!tbody) return;

  if (history.length === 0) {
    tbody.innerHTML = `<tr><td colspan="3" style="text-align:center; color:#55555c;">No completed streaks yet. Keep going.</td></tr>`;
    return;
  }

  tbody.innerHTML = '';
  // Most recent first
  [...history].reverse().forEach(entry => {
    const startStr = new Date(entry.start).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
    const endStr = new Date(entry.end).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
    const row = document.createElement('tr');
    row.innerHTML = `
      <td>${escapeHTML(startStr)}</td>
      <td>${escapeHTML(endStr)}</td>
      <td style="text-align:right; font-variant-numeric: tabular-nums;">${entry.days} day${entry.days !== 1 ? 's' : ''}</td>
    `;
    tbody.appendChild(row);
  });
}

// Draw custom canvas bar chart for urge tracking (Past 7 Days)
function drawUrgeChart(history) {
  const canvas = document.getElementById('urge-chart');
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  const daysData = [];
  const now = new Date();

  for (let i = 6; i >= 0; i--) {
    const tempDate = new Date();
    tempDate.setDate(now.getDate() - i);
    const dateStr = tempDate.toISOString().split('T')[0];
    const dayName = tempDate.toLocaleDateString(undefined, { weekday: 'short' });
    daysData.push({
      dateStr: dateStr,
      dayName: dayName,
      count: history[dateStr] || 0
    });
  }

  let maxCount = 4;
  daysData.forEach(d => {
    if (d.count > maxCount) maxCount = d.count;
  });

  const chartHeight = 160;
  const chartWidth = canvas.width - 60;
  const paddingLeft = 40;
  const paddingTop = 20;

  ctx.strokeStyle = '#222227';
  ctx.lineWidth = 1;
  ctx.fillStyle = '#8e8e93';
  ctx.font = '11px Inter, sans-serif';

  const gridLines = 4;
  for (let i = 0; i <= gridLines; i++) {
    const y = paddingTop + (chartHeight / gridLines) * i;
    const value = Math.round(maxCount - (maxCount / gridLines) * i);

    ctx.beginPath();
    ctx.moveTo(paddingLeft, y);
    ctx.lineTo(paddingLeft + chartWidth, y);
    ctx.stroke();

    ctx.fillText(value, 15, y + 4);
  }

  const barSpacing = chartWidth / 7;
  const barWidth = 32;

  daysData.forEach((day, index) => {
    const x = paddingLeft + (barSpacing * index) + (barSpacing - barWidth) / 2;
    const barHeight = (day.count / maxCount) * chartHeight;
    const y = paddingTop + chartHeight - barHeight;

    if (day.count > 0) {
      ctx.fillStyle = '#2563eb';
      ctx.fillRect(x, y, barWidth, barHeight);

      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 11px Inter, sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(day.count, x + barWidth / 2, y - 6);
    } else {
      ctx.fillStyle = '#141418';
      ctx.fillRect(x, paddingTop + chartHeight - 4, barWidth, 4);
    }

    ctx.fillStyle = '#8e8e93';
    ctx.font = '11px Inter, sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(day.dayName, x + barWidth / 2, paddingTop + chartHeight + 18);
  });
}

// Load mindful journal list
function loadJournalEntries() {
  chrome.storage.local.get(['journalEntries'], (data) => {
    const entries = data.journalEntries || [];
    const listElement = document.getElementById('journal-list');

    if (!listElement) return;

    if (entries.length === 0) {
      listElement.innerHTML = `
        <div class="empty-state">
          <p>No reflections recorded. Fill in the form to document your thoughts.</p>
        </div>
      `;
      return;
    }

    listElement.innerHTML = '';
    entries.forEach((entry, index) => {
      const dateObj = new Date(entry.date);
      const formattedDate = dateObj.toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });

      const entryCard = document.createElement('div');
      entryCard.className = 'journal-entry-card';

      entryCard.innerHTML = `
        <div class="journal-header">
          <div class="journal-meta">
            <span class="journal-tag">${escapeHTML(entry.trigger)}</span>
            <span class="journal-intensity-tag">Intensity: ${entry.intensity}/10</span>
          </div>
          <div style="display:flex; align-items:center; gap:12px;">
            <span class="journal-date">${formattedDate}</span>
            <button class="delete-rule-btn delete-journal-btn" data-index="${index}" title="Delete entry">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6"></polyline>
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
              </svg>
            </button>
          </div>
        </div>
        <div class="journal-body">${escapeHTML(entry.notes)}</div>
      `;

      entryCard.querySelector('.delete-journal-btn').addEventListener('click', () => {
        deleteJournalEntry(index);
      });

      listElement.appendChild(entryCard);
    });
  });
}

// Delete a journal entry by index
function deleteJournalEntry(index) {
  chrome.storage.local.get(['journalEntries'], (data) => {
    const entries = data.journalEntries || [];
    entries.splice(index, 1);
    chrome.storage.local.set({ journalEntries: entries }, loadJournalEntries);
  });
}

// Load custom block rules table
function loadCustomBlockRules() {
  chrome.storage.local.get(['customBlockedSites'], (data) => {
    const sites = data.customBlockedSites || [];
    const listElement = document.getElementById('custom-sites-list');

    if (!listElement) return;

    if (sites.length === 0) {
      listElement.innerHTML = `
        <tr>
          <td colspan="2" style="text-align: center; color: #55555c;">No custom sites added yet.</td>
        </tr>
      `;
      return;
    }

    listElement.innerHTML = '';
    sites.forEach((site, index) => {
      const row = document.createElement('tr');
      row.innerHTML = `
        <td>${escapeHTML(site)}</td>
        <td style="text-align: right;">
          <button class="delete-rule-btn" data-index="${index}">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"></polyline>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
            </svg>
          </button>
        </td>
      `;

      row.querySelector('.delete-rule-btn').addEventListener('click', () => {
        deleteBlockRule(index);
      });

      listElement.appendChild(row);
    });
  });
}

// Delete block rule logic
function deleteBlockRule(index) {
  chrome.storage.local.get(['customBlockedSites'], (data) => {
    const sites = data.customBlockedSites || [];
    sites.splice(index, 1);
    chrome.storage.local.set({ customBlockedSites: sites }, loadCustomBlockRules);
  });
}

// Verify chrome extension incognito access
function checkIncognitoStatus() {
  const statusBox = document.getElementById('incognito-status-box');
  const statusText = document.getElementById('incognito-status-text');

  if (!statusBox || !statusText) return;

  chrome.extension.isAllowedIncognitoAccess((isAllowed) => {
    statusBox.classList.remove('warning', 'success');
    if (isAllowed) {
      statusBox.classList.add('success');
      statusText.textContent = 'Active: Zenith is successfully monitoring and protecting Incognito tabs.';
    } else {
      statusBox.classList.add('warning');
      statusText.textContent = 'Inactive: Zenith is currently disabled in Incognito. Please follow the instructions above.';
    }
  });
}

// Escapes HTML inputs to prevent XSS issues inside storage
function escapeHTML(str) {
  if (!str) return '';
  return str.replace(/[&<>'"]/g,
    tag => ({
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      "'": '&#39;',
      '"': '&quot;'
    }[tag] || tag)
  );
}
