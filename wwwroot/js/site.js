(() => {
    "use strict";

    const dateEl = document.getElementById("liveDate");
    const timeEl = document.getElementById("liveTime");
    if (dateEl && timeEl) {
        const clockHost = document.querySelector("[data-live-clock]");
        const serverUnix = Number(clockHost?.dataset.serverTime || Date.now());
        const startPerf = performance.now();
        const updateClock = () => {
            const now = new Date(serverUnix + (performance.now() - startPerf));
            dateEl.textContent = new Intl.DateTimeFormat("en-PH", {
                timeZone: "Asia/Manila", year: "numeric", month: "long", day: "numeric"
            }).format(now);
            timeEl.textContent = new Intl.DateTimeFormat("en-PH", {
                timeZone: "Asia/Manila", hour: "numeric", minute: "2-digit", second: "2-digit", hour12: true
            }).format(now);
        };
        updateClock();
        window.setInterval(updateClock, 1000);
    }

    const search = document.getElementById("facultySearch");
    const facultyId = document.getElementById("facultyId");
    const facultyList = document.getElementById("facultyList");
    const selectedFaculty = document.getElementById("selectedFaculty");
    const selectedName = document.getElementById("selectedFacultyName");
    const clearFaculty = document.getElementById("clearFaculty");
    const timeInButton = document.getElementById("timeInButton");
    const noMatch = document.getElementById("noFacultyMatch");
    const visibleCountLabel = document.getElementById("facultyVisibleCount");
    const buttonHint = document.querySelector(".timein-button-hint");
    const form = document.getElementById("timeInForm");

    if (search && facultyId && facultyList && selectedFaculty && selectedName && timeInButton && form) {
        const options = Array.from(facultyList.querySelectorAll(".faculty-option"));
        const groups = Array.from(facultyList.querySelectorAll(".faculty-group"));
        const panes = Array.from(facultyList.querySelectorAll("[data-department-pane]"));
        const tabs = Array.from(document.querySelectorAll("[data-department-tab]"));
        const confirmationEl = document.getElementById("confirmTimeInModal");
        const confirmation = confirmationEl && window.bootstrap ? new bootstrap.Modal(confirmationEl) : null;
        const confirmButton = document.getElementById("confirmTimeInSubmit");
        const confirmName = document.getElementById("confirmTimeInName");
        let activeDepartment = "college";
        let confirmed = false;
        let submitted = false;

        const clearSelection = () => {
            facultyId.value = "";
            selectedName.textContent = "";
            selectedFaculty.classList.add("d-none");
            timeInButton.disabled = true;
            if (buttonHint) buttonHint.textContent = "Select your name above to enable time-in.";
            options.forEach(x => x.classList.remove("is-selected"));
        };

        const filterFaculty = () => {
            const term = search.value.trim().toLocaleLowerCase();
            let visibleCount = 0;
            options.forEach(option => {
                const pane = option.closest("[data-department-pane]");
                const inDepartment = pane?.dataset.departmentPane === activeDepartment;
                const matches = !term || (option.dataset.search || "").includes(term);
                option.classList.toggle("d-none", !matches);
                if (inDepartment && matches) visibleCount++;
            });
            groups.forEach(group => {
                const anyVisible = group.querySelector(".faculty-option:not(.d-none)") !== null;
                group.classList.toggle("d-none", !anyVisible);
            });
            if (noMatch) noMatch.classList.toggle("d-none", visibleCount !== 0);
            if (visibleCountLabel) {
                visibleCountLabel.textContent = visibleCount === 1
                    ? "1 person found"
                    : `${visibleCount} people found`;
            }
        };

        const showDepartment = (dept) => {
            activeDepartment = dept;
            panes.forEach(pane => { pane.hidden = pane.dataset.departmentPane !== dept; });
            tabs.forEach(tab => {
                const active = tab.dataset.departmentTab === dept;
                tab.classList.toggle("active", active);
                tab.setAttribute("aria-selected", active ? "true" : "false");
            });
            clearSelection();
            search.value = "";
            filterFaculty();
        };

        tabs.forEach(tab => tab.addEventListener("click", () => showDepartment(tab.dataset.departmentTab)));
        search.addEventListener("input", filterFaculty);

        options.forEach(option => {
            option.addEventListener("click", () => {
                if (option.disabled || submitted) return;
                clearSelection();
                option.classList.add("is-selected");
                facultyId.value = option.dataset.facultyId;
                selectedName.textContent = option.dataset.facultyName;
                selectedFaculty.classList.remove("d-none");
                timeInButton.disabled = false;
                if (buttonHint) buttonHint.textContent = "Ready! Check your name and press Confirm Time-In.";
                selectedFaculty.scrollIntoView({ behavior: "smooth", block: "nearest" });
            });
        });

        clearFaculty?.addEventListener("click", () => {
            clearSelection();
            search.focus();
        });

        form.addEventListener("submit", (event) => {
            if (!facultyId.value || submitted) {
                event.preventDefault();
                return;
            }
            if (!confirmed) {
                event.preventDefault();
                if (confirmName) confirmName.textContent = selectedName.textContent;
                if (confirmation) confirmation.show();
                else if (window.confirm(`Confirm time-in for ${selectedName.textContent}?`)) {
                    confirmed = true;
                    form.requestSubmit();
                }
                return;
            }
            submitted = true;
            timeInButton.disabled = true;
            if (confirmButton) confirmButton.disabled = true;
            const label = timeInButton.querySelector(".timein-button-label");
            if (label) label.textContent = "RECORDING...";
        });

        confirmButton?.addEventListener("click", () => {
            if (!facultyId.value || submitted) return;
            confirmed = true;
            if (confirmation) confirmation.hide();
            form.requestSubmit();
        });
        confirmationEl?.addEventListener("hidden.bs.modal", () => {
            if (!submitted) confirmed = false;
        });
        showDepartment("college");
    }

    document.getElementById("printReportButton")?.addEventListener("click", () => window.print());
})();
