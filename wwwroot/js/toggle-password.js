document.addEventListener("DOMContentLoaded", () => {
    const botones = document.querySelectorAll("[data-toggle-password]");

    botones.forEach((boton) => {
        boton.addEventListener("click", () => {
            const targetId = boton.getAttribute("data-toggle-password");
            const input = document.getElementById(targetId);

            if (!input) return;

            const esOculta = input.type === "password";
            input.type = esOculta ? "text" : "password";
            boton.textContent = esOculta ? "Ocultar" : "Ver";
            boton.setAttribute("aria-pressed", String(esOculta));
        });
    });
});