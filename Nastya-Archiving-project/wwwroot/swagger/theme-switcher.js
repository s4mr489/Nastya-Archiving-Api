// theme-switcher.js
document.addEventListener('DOMContentLoaded', function () {
    const switchButton = document.createElement('button');
    switchButton.innerText = 'Toggle Theme';
    switchButton.style.position = 'fixed';
    switchButton.style.top = '10px';
    switchButton.style.right = '10px';
    switchButton.style.zIndex = '9999';
    document.body.appendChild(switchButton);

    const darkThemeLink = document.querySelector('link[href="/swagger/swagger-dark.css"]');

    switchButton.addEventListener('click', function () {
        if (darkThemeLink.disabled) {
            // Enable the dark theme
            darkThemeLink.disabled = false;
        } else {
            // Disable the dark theme (revert to default light theme)
            darkThemeLink.disabled = true;
        }
    });
});
window.addEventListener('load', () => {
    // Make the top-left logo/text link to your site
    const logoLink = document.querySelector('.topbar .link');
    if (logoLink) {
        logoLink.href = 'https://Samer-asp.verce.app';
        logoLink.target = '_blank';
        logoLink.rel = 'noopener';
    }

    // Change the API version text in the select label (if you didn’t hide it)
    const label = document.querySelector('.download-url-wrapper .select-label span');
    if (label) label.textContent = 'API Version';
});