const input = document.getElementById("chatInput");   // autocomplete input
const suggestionsDiv = document.getElementById("suggestions");
const chatLog = document.getElementById("chatLog");   // chat log div
let lastMessageCount = 0;
let debounceTimer;
let activeIndex = -1;
let currentSuggestions = [];
const cache = {};

const API_PRED = "/api/prediction/next"; // autocomplete API
const API_CHAT = "/api/chat";             // chat API

// Sačuvaj clientId u localStorage
let clientId = localStorage.getItem("clientId");
if (!clientId) {
    clientId = Math.random().toString(36).substring(2, 9);
    localStorage.setItem("clientId", clientId);
}

// ------------------ Debounce ------------------
function debounce(fn, delay) {
    return (...args) => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => fn(...args), delay);
    };
}

// ------------------ Fetch predictions with cache ------------------
async function fetchPredictions(key) {
    if (cache[key]) return cache[key];

    const response = await fetch(`${API_PRED}?prev=${encodeURIComponent(key)}`);
    const data = await response.json();
    cache[key] = data;
    return data;
}

// ------------------ Highlight predikcije ------------------
function highlight(word, typed) {
    if (!typed) return word;
    const index = word.toLowerCase().indexOf(typed.toLowerCase());
    if (index === -1) return word;
    return word.substring(0, index) +
           `<span class="highlight">` +
           word.substring(index, index + typed.length) +
           `</span>` +
           word.substring(index + typed.length);
}

// ------------------ Render autocomplete suggestions ------------------
function renderSuggestions(list, lastWord) {
    suggestionsDiv.innerHTML = "";
    currentSuggestions = [];
    activeIndex = -1;

    if (!list || list.length === 0) return;

    currentSuggestions = list.slice(0, 5);

    currentSuggestions.forEach((word, index) => {
        const div = document.createElement("div");
        div.className = "suggestion";
        div.innerHTML = highlight(word, lastWord);
        div.onclick = () => selectSuggestion(index);
        suggestionsDiv.appendChild(div);
    });
}

// ------------------ Select suggestion ------------------
function selectSuggestion(index) {
    if (index < 0 || index >= currentSuggestions.length) return;

    const suggestion = currentSuggestions[index];
    const text = input.value;
    const endsWithSpace = text.endsWith(" ");

    if (endsWithSpace) {
        input.value = text + suggestion + " ";
    } else {
        const words = text.trim().split(/\s+/);
        words[words.length - 1] = suggestion;
        input.value = words.join(" ") + " ";
    }

    sessionStorage.setItem("draftText", input.value);

    suggestionsDiv.innerHTML = "";
    input.focus();
    handleInput(); // ponovo učitaj predikcije
}

// ------------------ Chat log ------------------
function renderChat(messages) {
    chatLog.innerHTML = "";
    messages.forEach(msg => {
        const sender = msg.clientId === clientId ? "user" : "other";
        addMessage(msg.text, sender, false);
    });
    chatLog.scrollTop = chatLog.scrollHeight;
}

function addMessage(message, sender = "user", save = true) {
    if (!chatLog) return;
    const div = document.createElement("div");
    div.className = `chat-message ${sender}`;
    div.textContent = message;
    chatLog.appendChild(div);
    chatLog.scrollTop = chatLog.scrollHeight;

    if (save) {
        fetch(`${API_CHAT}/send`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ clientId, text: message })
        }).catch(console.error);
    }
}

// ------------------ Load chat history ------------------
async function loadChatHistory() {
    try {
        const res = await fetch(`${API_CHAT}/history`);
        const data = await res.json();

        chatLog.innerHTML = "";
        data.forEach(msg => {
            const sender = msg.clientId === clientId ? "user" : "other";
            addMessage(msg.text, sender, false);
        });

        lastMessageCount = data.length;
        chatLog.scrollTop = chatLog.scrollHeight;

    } catch (err) {
        console.error("Greška pri učitavanju chat history:", err);
    }
}

// ------------------ Input handler (autocomplete) ------------------
const handleInput = debounce(async () => {
    const text = input.value;
    const endsWithSpace = text.endsWith(" ");
    const words = text.trim().split(/\s+/).filter(w => w.length > 0);

    if (words.length < 2) {
        suggestionsDiv.innerHTML = "";
        return;
    }

    let key;
    let lastWord = "";

    if (endsWithSpace) {
        key = words.slice(-2).join(" ");
        lastWord = "";
    } else {
        key = words.slice(-2).join(" ");
        lastWord = words[words.length - 1];
    }

    suggestionsDiv.innerHTML = `<div class="loading">Učitavanje...</div>`;

    try {
        const raw = await fetchPredictions(key);
        let predictions = raw.predictions || [];
        renderSuggestions(predictions, lastWord);
    } catch {
        suggestionsDiv.innerHTML = "";
    }
}, 250);

input.addEventListener("input", handleInput);

// ------------------ Keyboard navigation i Enter logika ------------------
input.addEventListener("keydown", async (e) => {
    const items = document.querySelectorAll(".suggestion");

    if (e.key === "ArrowDown") {
        e.preventDefault();
        if (items.length) {
            activeIndex = (activeIndex + 1) % items.length;
            updateActive(items);
        }
    }

    if (e.key === "ArrowUp") {
        e.preventDefault();
        if (items.length) {
            activeIndex = (activeIndex - 1 + items.length) % items.length;
            updateActive(items);
        }
    }

    if (e.key === "Enter") {
        e.preventDefault();

        if (activeIndex >= 0 && currentSuggestions.length > 0){
            selectSuggestion(activeIndex);
        } else {
            const msg = input.value.trim();
            if (!msg) return;

            // samo pošalji backendu
            fetch(`${API_CHAT}/send`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ clientId, text: msg })
            }).catch(console.error);

            input.value = "";
            sessionStorage.removeItem("draftText");
            suggestionsDiv.innerHTML = "";
        }
    }

    if (e.key === "Escape") {
        suggestionsDiv.innerHTML = "";
        activeIndex = -1;
        currentSuggestions = [];
    }
});

function updateActive(items) {
    items.forEach(i => i.classList.remove("active"));
    if (items[activeIndex])
        items[activeIndex].classList.add("active");
}



// Sačuvaj trenutni draft tekst
input.value = sessionStorage.getItem("draftText") || "";

input.addEventListener("input", () => {
    sessionStorage.setItem("draftText", input.value);
});

async function refreshChat() {
    try {
        const res = await fetch(`${API_CHAT}/history`);
        const data = await res.json();

        if (data.length > lastMessageCount) {

            const newMessages = data.slice(lastMessageCount);

            newMessages.forEach(msg => {
                const sender = msg.clientId === clientId ? "user" : "other";
                addMessage(msg.text, sender, false);
            });

            lastMessageCount = data.length;
            chatLog.scrollTop = chatLog.scrollHeight;
        }

    } catch (err) {
        console.error("Greška pri osvežavanju chat-a:", err);
    }
}

// ------------------ Init ------------------
loadChatHistory().then(async () => {
    const draft = sessionStorage.getItem("draftText") || "";
    input.value = draft;

    input.focus();
    input.setSelectionRange(input.value.length, input.value.length);

    
    if (draft.trim().split(/\s+/).length >= 2) {
        await handleInput();
    }
});

setInterval(refreshChat, 2000);