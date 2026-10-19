
document.addEventListener("DOMContentLoaded", () => {
    const search = document.getElementById("menu-search");
    const category = document.getElementById("menu-category");
    const sort = document.getElementById("menu-sort");
    const minPrice = document.getElementById("price-min");
    const maxPrice = document.getElementById("price-max");
    const clear = document.getElementById("clear-filters");
    const resultCount = document.getElementById("result-count");
    const empty = document.getElementById("menu-empty");
    const pagination = document.getElementById("menu-pagination");

    const items = Array.from(
        document.querySelectorAll(".menu-item")
    );

    const pageSize = 9;
    let currentPage = 1;

    function updateMenu() {
        const keyword = (search.value || "").trim().toLowerCase();
        const selectedCategory = category.value;
        const minimum = Number(minPrice.value || 0);
        const maximum = maxPrice.value
            ? Number(maxPrice.value)
            : Infinity;

        let visible = items.filter(item => {
            const name = item.dataset.name || "";
            const itemCategory = item.dataset.category;
            const price = Number(item.dataset.price);

            return name.includes(keyword)
                && (!selectedCategory ||
                    itemCategory === selectedCategory)
                && price >= minimum
                && price <= maximum;
        });

        if (sort.value === "asc") {
            visible.sort((a, b) =>
                Number(a.dataset.price) - Number(b.dataset.price));
        } else if (sort.value === "desc") {
            visible.sort((a, b) =>
                Number(b.dataset.price) - Number(a.dataset.price));
        } else {
            visible.sort((a, b) =>
                Number(b.dataset.featured === "true") -
                Number(a.dataset.featured === "true"));
        }

        const totalPages = Math.ceil(visible.length / pageSize);
        currentPage = Math.min(currentPage, Math.max(1, totalPages));

        items.forEach(item => item.hidden = true);

        // Sắp xếp lại các món theo bộ lọc
        const grid = document.getElementById("menu-grid");
        visible.forEach(item => grid.appendChild(item));

        const start = (currentPage - 1) * pageSize;
        visible.slice(start, start + pageSize)
            .forEach(item => item.hidden = false);

        resultCount.textContent = `${visible.length} món ăn`;
        empty.hidden = visible.length > 0;

        pagination.innerHTML = "";

        for (let page = 1; page <= totalPages; page++) {
            const li = document.createElement("li");
            li.className = "page-item" +
                (page === currentPage ? " active" : "");

            const button = document.createElement("button");
            button.type = "button";
            button.className = "page-link";
            button.textContent = page;

            button.addEventListener("click", () => {
                currentPage = page;
                updateMenu();
            });

            li.appendChild(button);
            pagination.appendChild(li);
        }
    }

    [search, category, sort, minPrice, maxPrice].forEach(control => {
        control.addEventListener("input", () => {
            currentPage = 1;
            updateMenu();
        });
    });

    clear.addEventListener("click", () => {
        search.value = "";
        category.value = "";
        sort.value = "featured";
        minPrice.value = "";
        maxPrice.value = "";
        currentPage = 1;
        updateMenu();
    });

    updateMenu();
});
