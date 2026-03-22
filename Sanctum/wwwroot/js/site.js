document.addEventListener("DOMContentLoaded", function () {
    const bookingPage = document.getElementById("calendarDates");
    if (!bookingPage) return;

    const profileMenuBtn = document.getElementById("profileMenuBtn");
    const profileDropdown = document.getElementById("profileDropdown");

    const monthYearLabel = document.getElementById("calendarMonthYear");
    const calendarDates = document.getElementById("calendarDates");
    const prevMonthBtn = document.getElementById("prevMonthBtn");
    const nextMonthBtn = document.getElementById("nextMonthBtn");
    const buildingPins = document.querySelectorAll(".building-pin");
    const selectedBuildingBadge = document.getElementById("selectedBuildingBadge");
    // const buildingInfoText = document.getElementById("buildingInfoText");
    const timeSlotsContainer = document.getElementById("timeSlots");
    const roomListContainer = document.getElementById("roomList");
    const confirmBookingBtn = document.getElementById("confirmBookingBtn");

    const summaryBuilding = document.getElementById("summaryBuilding");
    const summaryRoom = document.getElementById("summaryRoom");
    const summaryDate = document.getElementById("summaryDate");
    const summaryTime = document.getElementById("summaryTime");

    let currentDate = new Date();
    let selectedBuilding = null;
    let selectedRoom = null;
    let selectedDate = null;
    let selectedTime = null;

    let rooms = {};

    async function loadRooms() {
        try {
            const response = await fetch('/Room/GetRooms');
            console.log('Response status:', response.status);
            const data = await response.json();
            console.log('Rooms data:', data);
            rooms = data;
        } catch (error) {
            console.error('Failed to load rooms:', error);
        }
    }

    const mockAvailability = {
        "COB-201": ["8:00 AM", "9:00 AM", "10:00 AM", "1:00 PM", "2:00 PM"],
        "COB-202": ["9:00 AM", "11:00 AM", "3:00 PM"],
        "COB-301": ["10:00 AM", "12:00 PM", "4:00 PM"],
        "COB-302": ["10:00 AM", "12:00 PM", "4:00 PM"],

        "HC-101": ["8:30 AM", "9:30 AM", "1:00 PM"],
        "HC-203": ["11:00 AM", "12:00 PM", "2:00 PM"],
        "HC-305": ["10:00 AM", "3:00 PM", "5:00 PM"],
        "HC-306": ["10:00 AM", "3:00 PM", "5:00 PM"],

        "SSSC-110": ["8:00 AM", "10:30 AM", "1:30 PM"],
        "SSSC-210": ["9:00 AM", "12:30 PM", "4:30 PM"],

        "VEC-17": ["7:30 AM", "9:30 AM", "12:30 PM"],
        "VEC-18": ["8:30 AM", "11:30 AM", "2:30 PM"],
        "VEC-19": ["10:00 AM", "1:00 PM", "4:00 PM"]
    };

    function renderRooms() {
        roomListContainer.innerHTML = "";

        if (!selectedBuilding) {
            roomListContainer.innerHTML = `<p class="placeholder-text">Choose a building first to view available rooms.</p>`;
            return;
        }

        const buildingRooms = rooms[selectedBuilding] || [];

        buildingRooms.forEach(room => {
            const btn = document.createElement("button");
            btn.classList.add("room-btn");
            btn.textContent = room;

            if (selectedRoom === room) {
                btn.classList.add("selected");
            }

            btn.addEventListener("click", function () {
                selectedRoom = room;
                selectedDate = null;
                selectedTime = null;
                renderRooms();
                renderCalendar();
                renderTimeSlots();
                updateSummary();
            });

            roomListContainer.appendChild(btn);
        });
    }

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

            if (!selectedBuilding || !selectedRoom || cellDate < today) {
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

        if (!selectedBuilding || !selectedRoom || !selectedDate) {
            timeSlotsContainer.innerHTML = `<p class="placeholder-text">Choose a building, room, and date to see available times.</p>`;
            confirmBookingBtn.disabled = true;
            return;
        }

        const slots = mockAvailability[selectedRoom] || [];

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
        summaryRoom.textContent = selectedRoom || "—";
        summaryDate.textContent = selectedDate ? selectedDate.toLocaleDateString() : "—";
        summaryTime.textContent = selectedTime || "—";

        confirmBookingBtn.disabled = !(selectedBuilding && selectedRoom && selectedDate && selectedTime);
    }

    buildingPins.forEach(pin => {
        pin.addEventListener("click", function () {
            buildingPins.forEach(p => p.classList.remove("active"));
            pin.classList.add("active");

            selectedBuilding = pin.dataset.building;
            selectedRoom = null;
            selectedDate = null;
            selectedTime = null;

            selectedBuildingBadge.textContent = selectedBuilding;
            // buildingInfoText.textContent = `${selectedBuilding} selected. Now choose a room.`;

            renderRooms();
            renderCalendar();
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
        if (selectedBuilding && selectedRoom && selectedDate && selectedTime) {
            alert(
                `Booking confirmed!\n\nBuilding: ${selectedBuilding}\nRoom: ${selectedRoom}\nDate: ${selectedDate.toLocaleDateString()}\nTime: ${selectedTime}`
            );
        }
    });

    if (profileMenuBtn && profileDropdown) {
    profileMenuBtn.addEventListener("click", function (e) {
        e.stopPropagation();
        profileDropdown.classList.toggle("show");
    });

    document.addEventListener("click", function (e) {
        if (!profileMenuBtn.contains(e.target) && !profileDropdown.contains(e.target)) {
            profileDropdown.classList.remove("show");
        }
    });
    }
    
    async function init() {
        console.log('init called');
        await loadRooms();
        renderRooms();
        renderCalendar();
        renderTimeSlots();
        updateSummary();
    }

    init();
});