namespace BomLocalService.Models;

/// <summary>
/// JavaScript code templates used for page evaluation
/// </summary>
public class JavaScriptTemplatesConfig
{
    public string WaitForSearchResults { get; set; } = @"() => {
        const results = Array.from(document.querySelectorAll('li.bom-linklist__item[role=""listitem""]'));
        return results.length > 0 && results.some(r => r.offsetParent !== null);
    }";

    public string ExtractSearchResults { get; set; } = @"() => {
        const resultsList = document.querySelector('ul[aria-labelledby=""location-results-title""]');
        if (!resultsList) {
            console.log('Location results list not found');
            return [];
        }
        const results = Array.from(resultsList.querySelectorAll('li.bom-linklist__item[role=""listitem""]'));
        console.log('Found', results.length, 'location results');
        return results.map((r) => {
            const nameEl = r.querySelector('[data-testid=""location-name""]');
            const descEl = r.querySelector('.bom-linklist-item__desc');
            const name = nameEl ? (nameEl.textContent || nameEl.innerText || '').trim() : '';
            const desc = descEl ? (descEl.textContent || descEl.innerText || '').trim() : '';
            const fullText = (r.textContent || r.innerText || '').trim();
            console.log('Result:', { hasNameEl: !!nameEl, hasDescEl: !!descEl, name: name, desc: desc });
            return [name, desc, fullText];
        });
    }";

    public string ExtractSearchResultsFallback { get; set; } = @"() => { 
        const results = Array.from(document.querySelectorAll('li.bom-linklist__item[role=""listitem""]')); 
        return results.map(r => r.textContent || ''); 
    }";

    public string WaitForMapCanvas { get; set; } = @"() => {
        const canvas = document.querySelector('.esri-view-surface canvas');
        return canvas && canvas.width > 0 && canvas.height > 0 && canvas.offsetWidth > 0 && canvas.offsetHeight > 0;
    }";

    public string WaitForEsriView { get; set; } = @"() => {
        try {
            const elements = document.querySelectorAll('.esri-view');
            for (let el of elements) {
                if (el.__view && el.__view.ready) {
                    return true;
                }
            }
        } catch(e) {}
        return false;
    }";

    public string CheckActiveFrameSegment { get; set; } = @"() => {
        const segments = Array.from(document.querySelectorAll('[data-testid=""bom-scrub-segment""]'));
        const activeSegment = segments.find(s => {
            const style = window.getComputedStyle(s);
            return style.backgroundColor !== 'rgb(148, 148, 148)' && style.backgroundColor !== 'rgb(148, 148, 148)';
        });
        return activeSegment && activeSegment.getAttribute('data-id') === '0';
    }";

    public string WaitForMapContainer { get; set; } = @"() => {
        const container = document.querySelector('.esri-view-surface');
        return container && container.offsetWidth > 0 && container.offsetHeight > 0;
    }";

    public string ExtractFrameInfo { get; set; } = @"() => {
        const segments = Array.from(document.querySelectorAll('[data-testid=""bom-scrub-segment""]'));
        const now = new Date();
        return segments.map((seg, index) => {
            const ariaLabel = seg.getAttribute('aria-label') || '';
            let minutes = null;
            
            // Parse timestamp format: ""Wednesday 17 Dec, 11:05 pm"" or ""17 Dec, 11:05 pm""
            const timestampMatch = ariaLabel.match(/(?:[A-Za-z]+\s+)?(\d{1,2})\s+([A-Za-z]{3}),?\s+(\d{1,2}):(\d{2})\s+(am|pm)/i);
            if (timestampMatch) {
                try {
                    const day = parseInt(timestampMatch[1]);
                    const monthStr = timestampMatch[2];
                    const hour12 = parseInt(timestampMatch[3]);
                    const minute = parseInt(timestampMatch[4]);
                    const ampm = timestampMatch[5].toLowerCase();
                    
                    const monthNames = ['jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug', 'sep', 'oct', 'nov', 'dec'];
                    const month = monthNames.indexOf(monthStr.toLowerCase());
                    
                    if (month >= 0) {
                        let hour24 = hour12;
                        if (ampm === 'pm' && hour12 !== 12) hour24 += 12;
                        if (ampm === 'am' && hour12 === 12) hour24 = 0;
                        
                        const year = now.getFullYear();
                        const frameTime = new Date(year, month, day, hour24, minute);
                        
                        // If parsed time is in future, assume it's from last year
                        if (frameTime > now) {
                            frameTime.setFullYear(year - 1);
                        }
                        
                        const diffMs = now - frameTime;
                        minutes = Math.round(diffMs / (1000 * 60));
                        
                        // Validate reasonable range (0-2 hours)
                        if (minutes < 0 || minutes > 120) {
                            minutes = null;
                        }
                    }
                } catch(e) {
                    // Parse error, leave minutes as null
                }
            }
            
            return { index: index, minutesAgo: minutes };
        });
    }";

    public string CheckModalOverlay { get; set; } = @"() => {
        const bomOverlay = document.querySelector('.bom-modal-overlay--after-open');
        if (bomOverlay && bomOverlay.style.display !== 'none') {
            return true;
        }
        
        const recaptchaSelectors = ['.g-recaptcha', '#g-recaptcha', '.rc-anchor-container'];
        for (const selector of recaptchaSelectors) {
            const el = document.querySelector(selector);
            if (el) {
                const style = window.getComputedStyle(el);
                const rect = el.getBoundingClientRect();
                if (style.display !== 'none' && style.visibility !== 'hidden' && 
                    rect.width > 200 && rect.height > 200) {
                    return true;
                }
            }
        }
        
        const allForms = document.querySelectorAll('form');
        for (const form of allForms) {
            const action = form.getAttribute('action') || '';
            const id = form.getAttribute('id') || '';
            if (action.includes('feedback') || id.includes('feedback')) {
                const style = window.getComputedStyle(form);
                const rect = form.getBoundingClientRect();
                if (style.display !== 'none' && rect.width > 200 && rect.height > 200) {
                    const text = form.textContent || '';
                    if (text.includes('reCAPTCHA') || text.includes('recaptcha') || text.includes('Tell us why')) {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }";

    public string CheckModalStillVisible { get; set; } = @"() => {
        const bomOverlay = document.querySelector('.bom-modal-overlay--after-open');
        if (bomOverlay && bomOverlay.style.display !== 'none') return true;
        
        const allForms = document.querySelectorAll('form');
        for (const form of allForms) {
            const action = form.getAttribute('action') || '';
            const id = form.getAttribute('id') || '';
            if (action.includes('feedback') || id.includes('feedback')) {
                const style = window.getComputedStyle(form);
                const rect = form.getBoundingClientRect();
                if (style.display !== 'none' && rect.width > 200 && rect.height > 200) {
                    return true;
                }
            }
        }
        return false;
    }";

    public string GetViewportSize { get; set; } = @"() => JSON.stringify({ width: window.innerWidth, height: window.innerHeight })";

    public string ExtractWeatherMetadata { get; set; } = @"() => {
        const section = document.querySelector('section[data-testid=""weatherMetadata""]') || 
                       document.querySelector('section[aria-label=""Last updated""]');
        if (!section) return null;
        
        const divs = section.querySelectorAll('div');
        return Array.from(divs).map(div => div.textContent.trim()).filter(text => text).join(' ');
    }";
}

