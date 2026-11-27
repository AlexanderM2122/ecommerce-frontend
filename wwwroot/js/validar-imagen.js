(() => {
    'use strict';

    const MAX_BYTES = 5 * 1024 * 1024;

    document.querySelectorAll('[data-validar-imagen]')
        .forEach(input => {

            const validar = () => {
                input.setCustomValidity('');

                const archivo = input.files[0];

                if (!archivo) {
                    return;
                }

                if (archivo.size > MAX_BYTES) {
                    input.setCustomValidity(
                        'La imagen no debe superar los 5 MB.'
                    );
                }
            };

            input.addEventListener('change', () => {
                validar();
                input.reportValidity();
            });

            input.form?.addEventListener('submit', event => {
                validar();

                if (!input.checkValidity()) {
                    event.preventDefault();
                    input.reportValidity();
                }
            });
        });
})();