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
