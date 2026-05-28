// Zenith Landing Page Interactive Script

document.addEventListener('DOMContentLoaded', () => {
  // Mockup Interactive Navigation
  const mockupNavItems = document.querySelectorAll('.mockup-nav-item');
  const mockupContentArea = document.querySelector('.mockup-content');
  const mockupTitle = mockupContentArea.querySelector('h3');

  const tabContents = {
    overview: `
      <div class="mockup-grid">
        <div class="mockup-card main-streak">
          <h4>Clean Streak</h4>
          <div class="mockup-streak-circle">
            <div class="mockup-streak-number">7</div>
            <div class="mockup-streak-label">Days</div>
          </div>
        </div>
        <div class="mockup-card quick-stats">
          <h4>Urges Surfed</h4>
          <div class="mockup-stat-number">24</div>
          <p>Chemical impulses successfully ridden out with box breathing.</p>
        </div>
      </div>
    `,
    journal: `
      <div style="display: flex; flex-direction: column; gap: 10px; font-size: 0.85rem; height: 100%; overflow-y: auto;">
        <div style="background: var(--bg-card); border: 1px solid var(--border-color); padding: 12px; border-radius: 6px;">
          <div style="display: flex; justify-content: space-between; margin-bottom: 5px; color: var(--text-secondary); font-size: 0.75rem;">
            <span>Stress / Fatigue</span>
            <span>Today, 2:45 PM</span>
          </div>
          <p style="color: white; font-weight: 500;">Rode out a strong biological trigger by sitting back and running through 4 box breathing cycles. Feeling clear now.</p>
        </div>
        <div style="background: var(--bg-card); border: 1px solid var(--border-color); padding: 12px; border-radius: 6px;">
          <div style="display: flex; justify-content: space-between; margin-bottom: 5px; color: var(--text-secondary); font-size: 0.75rem;">
            <span>Boredom</span>
            <span>Yesterday, 8:12 PM</span>
          </div>
          <p style="color: white; font-weight: 500;">Was tempted to open Edge and browse incognito. Zenith Shield closed the window and redirected me. Streak saved.</p>
        </div>
      </div>
    `,
    rules: `
      <div style="width: 100%; border-collapse: collapse; font-size: 0.85rem;">
        <table style="width: 100%; text-align: left; border-spacing: 0;">
          <thead>
            <tr style="color: var(--text-secondary); border-bottom: 1px solid var(--border-color);">
              <th style="padding: 8px 4px; font-weight: 600;">Trigger Target</th>
              <th style="padding: 8px 4px; font-weight: 600; text-align: right;">Mode</th>
            </tr>
          </thead>
          <tbody>
            <tr style="border-bottom: 1px solid var(--border-color);">
              <td style="padding: 8px 4px; color: white;">*adultsite*</td>
              <td style="padding: 8px 4px; color: var(--color-success); text-align: right; font-weight: 600;">Blocked</td>
            </tr>
            <tr style="border-bottom: 1px solid var(--border-color);">
              <td style="padding: 8px 4px; color: white;">civitai.com</td>
              <td style="padding: 8px 4px; color: var(--color-success); text-align: right; font-weight: 600;">Blocked</td>
            </tr>
            <tr>
              <td style="padding: 8px 4px; color: white;">local WebUI (7860)</td>
              <td style="padding: 8px 4px; color: var(--color-success); text-align: right; font-weight: 600;">Blocked</td>
            </tr>
          </tbody>
        </table>
      </div>
    `,
    shield: `
      <div style="display: flex; flex-direction: column; gap: 15px; align-items: center; justify-content: center; height: 100%; text-align: center;">
        <div style="width: 48px; height: 48px; border-radius: 50%; border: 3px solid var(--color-primary); color: var(--color-primary); display: flex; align-items: center; justify-content: center; margin-bottom: 5px;">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
          </svg>
        </div>
        <div>
          <h4 style="color: white; font-size: 1rem; margin-bottom: 4px;">Watchdog Shield: Running</h4>
          <p style="color: var(--text-secondary); font-size: 0.8rem; max-width: 320px;">System-wide process guard is monitoring. Mutual keep-alive resurrection mode active.</p>
        </div>
      </div>
    `
  };

  mockupNavItems.forEach(item => {
    item.addEventListener('click', () => {
      // Clear active class
      mockupNavItems.forEach(nav => nav.classList.remove('active'));
      // Add active class
      item.classList.add('active');

      const tabName = item.textContent.trim().toLowerCase().split(' ')[1] || item.textContent.trim().toLowerCase();
      
      // Update Title
      mockupTitle.textContent = item.textContent.trim();
      
      // Update Content
      const currentGrid = mockupContentArea.querySelector('.mockup-grid') || mockupContentArea.querySelector('div:not(.mockup-header)');
      if (currentGrid) {
        currentGrid.remove();
      }
      
      const newContentHtml = tabContents[tabName] || '';
      mockupContentArea.insertAdjacentHTML('beforeend', newContentHtml);
    });
  });
});
