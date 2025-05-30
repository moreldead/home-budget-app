//Sterowanie sidebarem

document.getElementById('sidebarToggle').addEventListener('click', function () {
    document.getElementById('sidebar').classList.toggle('collapsed');
});

document.addEventListener("DOMContentLoaded", function () {
    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath === "/home/docs") {
        const submenu = document.getElementById("submenu");
        const bsCollapse = new bootstrap.Collapse(submenu, { toggle: false });
        bsCollapse.show(); // rozwija z kontrolą JS
    }
});
