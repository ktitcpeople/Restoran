(() => {
    'use strict';
    let nextId = 0;
    const fields = 'input:not([type=hidden]):not([type=checkbox]):not([type=radio]),textarea,select';
    function errorFor(input) {
        if (input.disabled || input.readOnly) return '';
        if (input.dataset.choice && (!input.value || input.value === '0')) return 'Выберите значение из списка.';
        if (input.required && input.type !== 'number' && !input.value.trim()) return 'Заполните это поле.';
        if (input.dataset.kind === 'name' && input.value && !/^[\p{L}][\p{L}\p{M} .'-]*$/u.test(input.value.trim())) return 'Укажите имя буквами; допустимы пробел, дефис, точка и апостроф.';
        if (input.dataset.kind === 'login' && input.value && !/^[\p{L}\p{N}_.@-]{3,64}$/u.test(input.value)) return 'Логин: 3–64 символа; буквы, цифры, точка, @, дефис или подчёркивание.';
        if (input.dataset.kind === 'address' && input.value && (!/\p{L}/u.test(input.value) || !/[0-9]/.test(input.value))) return 'Укажите улицу и номер дома.';
        if (input.minLength > 0 && input.value && input.value.trim().length < input.minLength) return `Не менее ${input.minLength} символов.`;
        if (input.dataset.phone && !/^[78][0-9]{10}$/.test(input.value)) return 'Введите 11 цифр, начиная с 7 или 8.';
        const validity = input.validity;
        if (validity.badInput) return 'Введите число в допустимом формате.';
        if (validity.valueMissing) return 'Заполните это поле.';
        if (validity.rangeUnderflow) return `Минимальное значение: ${input.min}.`;
        if (validity.rangeOverflow) return `Максимальное значение: ${input.max}.`;
        if (validity.stepMismatch) return `Допустимый шаг: ${input.step || 1}.`;
        if (validity.tooLong) return `Не более ${input.maxLength} символов.`;
        if (validity.tooShort) return `Не менее ${input.minLength} символов.`;
        if (validity.typeMismatch) return 'Проверьте формат значения.';
        if (validity.patternMismatch) return 'Проверьте формат значения.';
        return '';
    }
    function showError(input) {
        const message = errorFor(input);
        let hint = input.parentElement?.querySelector(`[data-error-for="${input.dataset.validationId}"]`);
        if (message && !hint) {
            input.dataset.validationId ||= `field-${++nextId}`;
            hint = document.createElement('small');
            hint.className = 'field-error';
            hint.dataset.errorFor = input.dataset.validationId;
            hint.id = `${input.dataset.validationId}-error`;
            hint.setAttribute('aria-live', 'polite');
            input.insertAdjacentElement('afterend', hint);
        }
        if (hint) {
            hint.textContent = message;
            hint.hidden = !message;
            if (message) input.setAttribute('aria-describedby', hint.id);
            else input.removeAttribute('aria-describedby');
        }
        input.setAttribute('aria-invalid', message ? 'true' : 'false');
        return !message;
    }
    // Capture before Blazor reads input: keyboard, pasted text and drag/drop all share the same rule.
    document.addEventListener('paste', event => {
        const input = event.target;
        if (!input.matches?.('input[data-phone]')) return;
        event.preventDefault();
        const digits = (event.clipboardData?.getData('text') || '').replace(/[^0-9]/g, '');
        const start = input.selectionStart ?? input.value.length;
        const end = input.selectionEnd ?? start;
        input.value = (input.value.slice(0, start) + digits + input.value.slice(end)).slice(0, 11);
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }, true);
    document.addEventListener('input', event => {
        const input = event.target;
        if (!input.matches?.(fields)) return;
        if (input.dataset.phone) input.value = input.value.replace(/[^0-9]/g, '').slice(0, 11);
        if (input.getAttribute('aria-invalid') === 'true') showError(input);
    }, true);
    document.addEventListener('focusout', event => {
        if (event.target.matches?.(fields)) showError(event.target);
    });
    document.addEventListener('click', event => {
        const button = event.target.closest?.('[data-validate]');
        if (!button) return;
        const scope = button.closest('.modal-content') || button.closest('[data-form-zone]') || button.closest('.card') || button.parentElement;
        let firstInvalid;
        for (const input of scope.querySelectorAll(fields)) {
            if (!showError(input)) firstInvalid ||= input;
        }
        if (firstInvalid) {
            event.preventDefault();
            event.stopImmediatePropagation();
            firstInvalid.focus();
        }
    }, true);
})();
