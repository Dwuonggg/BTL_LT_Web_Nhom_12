
document.addEventListener("DOMContentLoaded", function () {

    const form = document.getElementById("add-cart-form");
    const quantityInput = document.getElementById("food-qty");
    const minusButton = document.getElementById("qty-minus");
    const plusButton = document.getElementById("qty-plus");
    const totalElement = document.getElementById("food-total");

    if (!form || !quantityInput || !totalElement) {
        return;
    }

    const price = Number(form.dataset.price);

    const min = 1;
    const max = 20;

    function getQuantity() {
        const value = Number(quantityInput.value);

        if (!Number.isFinite(value)) {
            return min;
        }

        return Math.max(min, Math.min(max, Math.trunc(value)));
    }

    function updateTotal() {
        const quantity = getQuantity();
        const total = price * quantity;

        totalElement.textContent =
            new Intl.NumberFormat("vi-VN", {
                maximumFractionDigits: 0
            }).format(total) + "đ";

        minusButton.disabled = quantity <= min;
        plusButton.disabled = quantity >= max;
    }

    minusButton.addEventListener("click", function () {
        quantityInput.value = Math.max(min, getQuantity() - 1);
        updateTotal();
    });

    plusButton.addEventListener("click", function () {
        quantityInput.value = Math.min(max, getQuantity() + 1);
        updateTotal();
    });

    quantityInput.addEventListener("input", updateTotal);

    quantityInput.addEventListener("change", function () {
        quantityInput.value = getQuantity();
        updateTotal();
    });

    form.addEventListener("submit", function () {
        quantityInput.value = getQuantity();
    });

    updateTotal();
});
