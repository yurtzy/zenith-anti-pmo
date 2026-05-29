// Zenith Anti-PMO - Mindful Intervention Page Controller
// Coordinates Navy SEAL Box Breathing cycles, friction timers, and redirects.

document.addEventListener('DOMContentLoaded', () => {
  // Parse URL Parameters
  const urlParams = new URLSearchParams(window.location.search);
  const trigger = urlParams.get('trigger') || 'Blocked Site';
  const originalUrl = urlParams.get('original') || '';
  const isManual = urlParams.get('manual') === 'true';

  // Customize message if opened manually
  if (isManual) {
    document.getElementById('pause-title').textContent = 'Urge Surfing Exercise';
    document.getElementById('pause-subtitle').textContent = 'You chose to step back and ride out an urge. Use this box-breathing cycle to ground yourself in the present moment.';
  } else {
    document.getElementById('pause-title').textContent = 'Pause. Breathe. Reflect.';
    document.getElementById('pause-subtitle').textContent = `Zenith intercepted a trigger (${escapeHTML(trigger)}). Your brain is seeking a dopamine loop. Break the circuit here.`;
  }

  // DOM Elements
  const breathingCircle = document.getElementById('breathing-circle');
  const breathingText = document.getElementById('breathing-text');
  const breathingInstruction = document.getElementById('breathing-instruction');
  const timerBox = document.getElementById('timer-box');
  const progressBar = document.getElementById('timer-progress');
  const timerLabel = document.getElementById('timer-label');
  const actionGrid = document.getElementById('action-grid');

  const safetyBtn = document.getElementById('safety-btn');
  const breatheAgainBtn = document.getElementById('breathe-again-btn');
  const proceedBtn = document.getElementById('proceed-btn');

  // Start breathing guide loop
  startBreathingCycle();

  // Start 10-second stability countdown
  startFrictionTimer(10, () => {
    timerBox.style.display = 'none';
    actionGrid.classList.remove('hide');
  });

  // Action Buttons Listeners
  safetyBtn.addEventListener('click', () => {
    chrome.storage.local.get(['safeRedirectUrl'], (data) => {
      const dest = data.safeRedirectUrl || 'https://www.google.com';
      window.location.href = dest;
    });
  });

  breatheAgainBtn.addEventListener('click', () => {
    actionGrid.classList.add('hide');
    timerBox.style.display = 'block';

    startFrictionTimer(10, () => {
      timerBox.style.display = 'none';
      actionGrid.classList.remove('hide');
    });
  });

  // High-Friction bypass check
  let bypassClickCount = 0;
  proceedBtn.addEventListener('click', () => {
    if (bypassClickCount === 0) {
      bypassClickCount = 1;
      proceedBtn.disabled = true;
      proceedBtn.style.opacity = '0.5';

      let secondsLeft = 5;
      proceedBtn.textContent = `Ponder for ${secondsLeft}s...`;

      const interval = setInterval(() => {
        secondsLeft--;
        if (secondsLeft > 0) {
          proceedBtn.textContent = `Ponder for ${secondsLeft}s...`;
        } else {
          clearInterval(interval);
          proceedBtn.disabled = false;
          proceedBtn.style.opacity = '1';
          proceedBtn.classList.remove('btn-danger-mute');
          proceedBtn.classList.add('btn-danger');
          proceedBtn.textContent = 'Confirm Bypass (Relapse)';
        }
      }, 1000);
    } else {
      // Log relapse: reset streak and archive previous streak
      chrome.storage.local.get(['streakStartDate', 'streakHistory', 'relapseHistory'], (data) => {
        const nowStr = new Date().toISOString();
        const prevStart = data.streakStartDate;
        const streakHistory = data.streakHistory || [];
        const relapseHistory = data.relapseHistory || [];

        // Archive the previous streak
        if (prevStart) {
          const start = new Date(prevStart);
          const end = new Date();
          const days = Math.floor((end - start) / (1000 * 60 * 60 * 24));
          streakHistory.push({ start: prevStart, end: nowStr, days });
        }

        // Log the relapse event
        relapseHistory.unshift({
          date: nowStr,
          trigger: trigger,
          source: originalUrl || 'manual'
        });

        chrome.storage.local.set({
          streakStartDate: nowStr,
          streakHistory,
          relapseHistory,
          lastMilestoneNotified: 0 // reset milestone tracking on new streak
        }, () => {
          if (originalUrl) {
            window.location.href = originalUrl;
          } else {
            window.location.href = 'https://www.google.com';
          }
        });
      });
    }
  });

  // Navy SEAL 4-4-4-4 Box Breathing Cycle Logic
  let breathingTimeout;
  function startBreathingCycle() {
    let phase = 0;

    function runPhase() {
      breathingCircle.classList.remove('inhale', 'hold', 'exhale', 'resting-hold');

      if (phase === 0) {
        breathingCircle.classList.add('inhale');
        breathingText.textContent = 'Inhale';
        breathingInstruction.textContent = 'Breathe in slowly through your nose...';
        phase = 1;
        breathingTimeout = setTimeout(runPhase, 4000);
      } else if (phase === 1) {
        breathingCircle.classList.add('hold');
        breathingText.textContent = 'Hold';
        breathingInstruction.textContent = 'Suspend your breath calmly...';
        phase = 2;
        breathingTimeout = setTimeout(runPhase, 4000);
      } else if (phase === 2) {
        breathingCircle.classList.add('exhale');
        breathingText.textContent = 'Exhale';
        breathingInstruction.textContent = 'Release your breath gently from your mouth...';
        phase = 3;
        breathingTimeout = setTimeout(runPhase, 4000);
      } else if (phase === 3) {
        breathingCircle.classList.add('resting-hold');
        breathingText.textContent = 'Hold';
        breathingInstruction.textContent = 'Keep your lungs comfortably empty...';
        phase = 0;
        breathingTimeout = setTimeout(runPhase, 4000);
      }
    }

    runPhase();
  }

  // 10s Friction Progress bar controller
  function startFrictionTimer(durationSeconds, onComplete) {
    let start = null;
    progressBar.style.width = '0%';

    function step(timestamp) {
      if (!start) start = timestamp;
      const elapsed = (timestamp - start) / 1000;
      const progress = Math.min(elapsed / durationSeconds, 1);

      progressBar.style.width = `${progress * 100}%`;

      const secondsRemaining = Math.max(0, Math.ceil(durationSeconds - elapsed));
      if (secondsRemaining > 0) {
        timerLabel.textContent = `Stabilizing attention: ${secondsRemaining}s`;
        requestAnimationFrame(step);
      } else {
        timerLabel.textContent = 'Attention Stabilized';
        progressBar.style.width = '100%';
        onComplete();
      }
    }

    requestAnimationFrame(step);
  }

  // Escape HTML output utility
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
});
