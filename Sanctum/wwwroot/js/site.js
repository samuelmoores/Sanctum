// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    const bookingPage = document.getElementById("calendarDates");
    if (!bookingPage) return;

    const monthYearLabel = document.getElementById("calendarMonthYear");
    const calendarDates = document.getElementById("calendarDates");
    const prevMonthBtn = document.getElementById("prevMonthBtn");
    const nextMonthBtn = document.getElementById("nextMonthBtn");
    const buildingPins = document.querySelectorAll(".building-pin");
    const selectedBuildingBadge = document.getElementById("selectedBuildingBadge");
    const buildingInfoText = document.getElementById("buildingInfoText");
    const timeSlotsContainer = document.getElementById("timeSlots");
    const confirmBookingBtn = document.getElementById("confirmBookingBtn");

    const summaryBuilding = document.getElementById("summaryBuilding");
    const summaryDate = document.getElementById("summaryDate");
    const summaryTime = document.getElementById("summaryTime");

    let currentDate = new Date();
    let selectedBuilding = null;
    let selectedDate = null;
    let selectedTime = null;

    const mockAvailability = {
        ECS: ["8:00 AM", "9:00 AM", "10:00 AM", "1:00 PM", "2:00 PM", "4:00 PM"],
        LIB: ["9:00 AM", "11:00 AM", "12:00 PM", "3:00 PM", "5:00 PM"],
        USU: ["8:30 AM", "10:30 AM", "1:30 PM", "2:30 PM"],
        VEC: ["7:30 AM", "9:30 AM", "12:30 PM", "4:30 PM"]
    };

    function renderCalendar() {
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const monthName = currentDate.toLocaleString("default", { month: "long" });
    monthYearLabel.textContent = `${monthName} ${year}`;

    calendarDates.innerHTML = "";

    for (let i = 0; i < firstDay; i++) {
        const emptyCell = document.createElement("div");
        emptyCell.classList.add("calendar-cell", "empty");
        calendarDates.appendChild(emptyCell);
    }

    for (let day = 1; day <= daysInMonth; day++) {
        const cell = document.createElement("div");
        cell.classList.add("calendar-cell");
        cell.textContent = day;

        const cellDate = new Date(year, month, day);
        const today = new Date();

        today.setHours(0, 0, 0, 0);
        cellDate.setHours(0, 0, 0, 0);

        if (
            cellDate.getDate() === today.getDate() &&
            cellDate.getMonth() === today.getMonth() &&
            cellDate.getFullYear() === today.getFullYear()
        ) {
            cell.classList.add("today");
        }

        if (
            selectedDate &&
            cellDate.getDate() === selectedDate.getDate() &&
            cellDate.getMonth() === selectedDate.getMonth() &&
            cellDate.getFullYear() === selectedDate.getFullYear()
        ) {
            cell.classList.add("selected");
        }

        if (cellDate < today) {
            cell.classList.add("disabled");
        } else {
            cell.addEventListener("click", function () {
                selectedDate = cellDate;
                selectedTime = null;
                renderCalendar();
                renderTimeSlots();
                updateSummary();
            });
        }

        calendarDates.appendChild(cell);
    }
}

    function renderTimeSlots() {
        timeSlotsContainer.innerHTML = "";

        if (!selectedBuilding || !selectedDate) {
            timeSlotsContainer.innerHTML = `<p class="placeholder-text">Choose a building and date to see available times.</p>`;
            confirmBookingBtn.disabled = true;
            return;
        }

        const slots = mockAvailability[selectedBuilding] || [];

        slots.forEach(time => {
            const btn = document.createElement("button");
            btn.classList.add("time-slot-btn");
            btn.textContent = time;

            if (selectedTime === time) {
                btn.classList.add("selected");
            }

            btn.addEventListener("click", function () {
                selectedTime = time;
                renderTimeSlots();
                updateSummary();
            });

            timeSlotsContainer.appendChild(btn);
        });

        confirmBookingBtn.disabled = !selectedTime;
    }

    function updateSummary() {
        summaryBuilding.textContent = selectedBuilding || "—";
        summaryDate.textContent = selectedDate
            ? selectedDate.toLocaleDateString()
            : "—";
        summaryTime.textContent = selectedTime || "—";

        confirmBookingBtn.disabled = !(selectedBuilding && selectedDate && selectedTime);
    }

    buildingPins.forEach(pin => {
        pin.addEventListener("click", function () {
            buildingPins.forEach(p => p.classList.remove("active"));
            pin.classList.add("active");

            selectedBuilding = pin.dataset.building;
            selectedTime = null;

            selectedBuildingBadge.textContent = selectedBuilding;
            buildingInfoText.textContent = `${selectedBuilding} selected. Now choose a date to view available times.`;

            renderTimeSlots();
            updateSummary();
        });
    });

    prevMonthBtn.addEventListener("click", function () {
        currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    });

    nextMonthBtn.addEventListener("click", function () {
        currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    });

    confirmBookingBtn.addEventListener("click", function () {
        if (selectedBuilding && selectedDate && selectedTime) {
            alert(
                `Booking confirmed!\n\nBuilding: ${selectedBuilding}\nDate: ${selectedDate.toLocaleDateString()}\nTime: ${selectedTime}`
            );
        }
    });

    renderCalendar();
    renderTimeSlots();
    updateSummary();
});