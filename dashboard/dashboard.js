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

      // Re-render chart if switching to overview
      if (tabId === 'overview') {
        loadOverviewData();
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
  
  // Close modal clicking outside the card
  if (modal) {
    modal.addEventListener('click', (e) => {
      if (e.target === modal) modal.classList.remove('open'));
    });
  }

  if (confirmResetBtn) {
    confirmResetBtn.addEventListener('click', () => {
      const nowStr = new Date().toISOString();
      chrome.storage.local.set({ streakStartDate: nowStr }, () => {
        modal.classList.remove('open');
        loadOverviewData();
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
        entries.unshift(newEntry); // Prepend new entry
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
});

// Calculate streak details and paint UI
function loadOverviewData() {
  chrome.storage.local.get(['streakStartDate', 'urgesSurfed', 'urgeHistory'], (data) => {
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
      
      // Calculate 30-day milestone progress
      const progress = (diffDays % 30) / 30;
      // If streak is exactly 0, show tiny indicator or empty. If non-zero but multiple of 30, show full circle.
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
        // Format day name nicely
        const dayObj = new Date(d);
        peakDay = dayObj.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' }) + ` (${peakCount} Urges)`;
      }
    });
    document.getElementById('peak-day').textContent = peakDay;

    // Draw the HTML5 custom charts
    drawUrgeChart(history);
  });
}

// Draw custom canvas bar chart for urge tracking (Past 7 Days)
function drawUrgeChart(history) {
  const canvas = document.getElementById('urge-chart');
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  
  // Clear canvas
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  // Generate list of past 7 days dates
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

  // Find max value in past 7 days to scale chart
  let maxCount = 4; // minimum scale ceiling
  daysData.forEach(d => {
    if (d.count > maxCount) maxCount = d.count;
  });
  
  // Design Layout Variables
  const chartHeight = 160;
  const chartWidth = canvas.width - 60;
  const paddingLeft = 40;
  const paddingTop = 20;
  
  // Draw horizontal grid lines and vertical labels
  ctx.strokeStyle = '#222227';
  ctx.lineWidth = 1;
  ctx.fillStyle = '#8e8e93';
  ctx.font = '11px Inter, sans-serif';
  
  const gridLines = 4;
  for (let i = 0; i <= gridLines; i++) {
    const y = paddingTop + (chartHeight / gridLines) * i;
    const value = Math.round(maxCount - (maxCount / gridLines) * i);
    
    // Grid Line
    ctx.beginPath();
    ctx.moveTo(paddingLeft, y);
    ctx.lineTo(paddingLeft + chartWidth, y);
    ctx.stroke();
    
    // Y-Axis labels
    ctx.fillText(value, 15, y + 4);
  }

  // Draw Bars
  const barSpacing = chartWidth / 7;
  const barWidth = 32;

  daysData.forEach((day, index) => {
    const x = paddingLeft + (barSpacing * index) + (barSpacing - barWidth) / 2;
    
    // Calculate height based on scale
    const barHeight = (day.count / maxCount) * chartHeight;
    const y = paddingTop + chartHeight - barHeight;

    // Draw Bar
    if (day.count > 0) {
      // Sleek solid blue bar
      ctx.fillStyle = '#2563eb';
      ctx.fillRect(x, y, barWidth, barHeight);
      
      // Draw count text on top of the bar
      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 11px Inter, sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(day.count, x + barWidth / 2, y - 6);
    } else {
      // Empty day indicator (very faint solid card outline)
      ctx.fillStyle = '#141418';
      ctx.fillRect(x, paddingTop + chartHeight - 4, barWidth, 4);
    }

    // Draw X-axis label
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
    entries.forEach(entry => {
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
            <span class="journal-intensity-tag">Urge Intensity: ${entry.intensity}/10</span>
          </div>
          <span class="journal-date">${formattedDate}</span>
        </div>
        <div class="journal-body">${escapeHTML(entry.notes)}</div>
      `;
      
      listElement.appendChild(entryCard);
    });
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
