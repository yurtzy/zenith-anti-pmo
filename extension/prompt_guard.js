/**
 * Zenith Anti-PMO — AI Prompt Shield Content Script
 * Monitors local WebUI interfaces (Stable Diffusion, ComfyUI, SillyTavern)
 * to intercept and prevent suggestive prompt generation in real-time.
 */
(function() {
    'use strict';

    console.log("🛡️ [Zenith Shield] AI Prompt Guard active on:", window.location.host);

    const dictionary = window.ZenithDictionary;
    if (!dictionary) {
        console.error("❌ [Zenith Shield] Dictionary not found. AI Prompt Guard disabled.");
        return;
    }

    // Keep track of active triggers
    let activeTrigger = null;
    let warningBox = null;

    // Run input monitoring loop
    setInterval(scanInputs, 800);

    function scanInputs() {
        const textareas = document.querySelectorAll('textarea, input[type="text"]');
        let matchedWord = null;

        textareas.forEach(input => {
            const val = input.value.toLowerCase();
            if (!val) return;

            // Check trigger keywords
            for (const word of dictionary.triggerKeywords) {
                // To avoid false positives, only check word-by-word or complete phrase matches
                if (word.includes(' ')) {
                    if (val.includes(word)) {
                        matchedWord = word;
                        break;
                    }
                } else {
                    const wordsInInput = val.split(/\s+/);
                    if (wordsInInput.includes(word)) {
                        matchedWord = word;
                        break;
                    }
                }
            }
        });

        if (matchedWord) {
            if (activeTrigger !== matchedWord) {
                activeTrigger = matchedWord;
                applyShieldProtection(matchedWord);
            }
        } else {
            if (activeTrigger !== null) {
                activeTrigger = null;
                removeShieldProtection();
            }
        }
    }

    function applyShieldProtection(trigger) {
        console.warn(`🛡️ [Zenith Shield] Suggestive trigger detected in prompt: "${trigger}"`);

        // 1. Create or update the floating warning indicator
        if (!warningBox) {
            warningBox = document.createElement('div');
            warningBox.id = 'zenith-shield-indicator';
            
            // Sleek, distraction-free minimalist style matching Zenith premium dashboard
            Object.assign(warningBox.style, {
                position: 'fixed',
                bottom: '24px',
                right: '24px',
                background: '#0c0c0e',
                border: '1px solid #e11d48',
                borderRadius: '8px',
                padding: '12px 18px',
                color: '#ffffff',
                fontFamily: 'Inter, system-ui, sans-serif',
                fontSize: '13px',
                fontWeight: '500',
                boxShadow: '0 8px 30px rgba(0, 0, 0, 0.5), 0 0 15px rgba(225, 29, 72, 0.15)',
                zIndex: '999999',
                display: 'flex',
                alignItems: 'center',
                gap: '10px',
                transition: 'all 0.3s cubic-bezier(0.16, 1, 0.3, 1)',
                opacity: '0',
                transform: 'translateY(10px)'
            });

            warningBox.innerHTML = `
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#e11d48" stroke-width="2.5" style="flex-shrink: 0;">
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                </svg>
                <div style="display: flex; flex-direction: column;">
                    <span style="font-weight: 700; color: #e11d48; text-transform: uppercase; font-size: 11px; letter-spacing: 1px;">Zenith Shield Active</span>
                    <span id="zenith-shield-text" style="color: #a1a1aa; margin-top: 1px;">Suggestive trigger detected: "${trigger}"</span>
                </div>
            `;
            document.body.appendChild(warningBox);
            
            // Trigger animation
            setTimeout(() => {
                warningBox.style.opacity = '1';
                warningBox.style.transform = 'translateY(0)';
            }, 50);
        } else {
            const txt = document.getElementById('zenith-shield-text');
            if (txt) txt.textContent = `Suggestive trigger detected: "${trigger}"`;
        }

        // 2. Locate and disable generate/queue buttons to secure execution
        secureGenerationButtons(true);
    }

    function removeShieldProtection() {
        console.log("🛡️ [Zenith Shield] Prompt cleared. Shield restored to standby.");
        if (warningBox) {
            warningBox.style.opacity = '0';
            warningBox.style.transform = 'translateY(10px)';
            setTimeout(() => {
                if (warningBox && warningBox.parentNode) {
                    warningBox.parentNode.removeChild(warningBox);
                }
                warningBox = null;
            }, 300);
        }
        secureGenerationButtons(false);
    }

    function secureGenerationButtons(shouldSecure) {
        // SD WebUI generate button, ComfyUI queue prompt button, general submit/generate buttons
        const buttonSelectors = [
            '#txt2img_generate', 
            '#img2img_generate', 
            'button.gr-button-lg', 
            '#comfy-queue-btn',
            '#generate',
            'button:contains("Queue")',
            'button:contains("Generate")',
            '#send_button',
            'button.send-btn'
        ];

        buttonSelectors.forEach(selector => {
            let elList = [];
            if (selector.includes(':contains')) {
                // Find buttons by text fallback
                const textTarget = selector.match(/"([^"]+)"/)[1];
                document.querySelectorAll('button').forEach(btn => {
                    if (btn.textContent.includes(textTarget)) {
                        elList.push(btn);
                    }
                });
            } else {
                elList = document.querySelectorAll(selector);
            }

            elList.forEach(btn => {
                if (shouldSecure) {
                    btn.disabled = true;
                    btn.style.opacity = '0.3';
                    btn.style.cursor = 'not-allowed';
                    
                    // Intercept any clicks / triggers that bypass standard disabling
                    if (!btn.dataset.zenithIntercepted) {
                        btn.dataset.zenithIntercepted = 'true';
                        btn.addEventListener('click', handleBypassAttempt, true);
                        btn.addEventListener('mousedown', handleBypassAttempt, true);
                    }
                } else {
                    btn.disabled = false;
                    btn.style.opacity = '';
                    btn.style.cursor = '';
                    if (btn.dataset.zenithIntercepted) {
                        btn.dataset.zenithIntercepted = '';
                        btn.removeEventListener('click', handleBypassAttempt, true);
                        btn.removeEventListener('mousedown', handleBypassAttempt, true);
                    }
                }
            });
        });
    }

    function handleBypassAttempt(e) {
        if (activeTrigger) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            
            console.error("🛡️ [Zenith Shield] Generation attempt blocked due to trigger:", activeTrigger);
            
            // Divert user to Box Breathing intervention screen instantly!
            const extensionId = "bonebkgnmbaongbgjfalllepkbkahhda";
            const interventionUrl = `chrome-extension://${extensionId}/intervention/intervention.html?trigger=${encodeURIComponent("Local Prompt: " + activeTrigger)}&original=${encodeURIComponent(window.location.href)}`;
            
            window.location.href = interventionUrl;
        }
    }
})();
