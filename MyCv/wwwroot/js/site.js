const storage = window.localStorage;

var savedDarkTheme = storage.getItem("darkTheme");

function setTheme(darkMode) {
    var bodyElement = document.getElementsByTagName("body")[0];
    bodyElement.classList.remove("dark-theme", "light-theme");
    if (darkMode) {
        bodyElement.classList.add("dark-theme");
    } else {
        bodyElement.classList.add("light-theme");
    }
    storage.setItem("darkTheme", darkMode);
}

const prefersDarkTheme = savedDarkTheme ? savedDarkTheme === "true" : window.matchMedia('(prefers-color-scheme: dark)').matches;
setTheme(prefersDarkTheme);
document.getElementById("themeToggle").checked = prefersDarkTheme;