document.addEventListener('DOMContentLoaded', fetchCards);

function fetchCards() {
    fetch('/data')
        .then(response => response.json())
        .then(data => {
            const cardsTable = document.getElementById('cardsTable');
            const fragment = document.createDocumentFragment(); // Use document fragment for efficiency
            data.forEach(card => {
                const row = createCardRow(card);
                fragment.appendChild(row);
            });
            cardsTable.innerHTML = ''; // Clear existing rows
            cardsTable.appendChild(fragment); // Append all rows at once
        })
        .catch(error => console.error('Error fetching cards:', error));
}

function createCardRow(card) {
    const row = document.createElement('tr');
    row.innerHTML = `
        <td>${card.id}</td>
        <td>${card.tag}</td>
        <td>${card.front}</td>
        <td>${card.back}</td>
        <td>
            <button type="button" class="update-button">Update</button>
            <button type="button" class="delete-button">Delete</button>
        </td>
    `;
    row.querySelector('.update-button').addEventListener('click', () => openModal(card.id, card.front, card.back, card.tag));
    row.querySelector('.delete-button').addEventListener('click', () => deleteCard(card.id));
    return row;
}

function filterCards() {
    const filterTagValue = document.getElementById('filterTag').value.toLowerCase();
    const filterContentValue = document.getElementById('filterContent').value.toLowerCase();
    const rows = document.querySelectorAll('#cardsTable tr');

    rows.forEach(row => {
        const tag = row.getElementsByTagName('td')[1];
        const front = row.getElementsByTagName('td')[2];
        const back = row.getElementsByTagName('td')[3];

        if (tag && front && back) {
            const tagText = tag.textContent.toLowerCase();
            const frontText = front.textContent.toLowerCase();
            const backText = back.textContent.toLowerCase();

            if (tagText.includes(filterTagValue) &&
                (frontText.includes(filterContentValue) || backText.includes(filterContentValue))) {
                row.style.display = ''; // Show matching rows
            } else {
                row.style.display = 'none'; // Hide non-matching rows
            }
        }
    });
}

function openModal(id, front, back, tag) {
    const modal = document.getElementById('updateModal');
    modal.style.display = 'block';

    // Populate fields with current card data
    document.getElementById('updateFront').value = front;
    document.getElementById('updateBack').value = back;
    document.getElementById('updateTag').value = tag;

    // Store the ID in a hidden field or variable
    modal.dataset.cardId = id;
}

function closeModal() {
    const modal = document.getElementById('updateModal');
    modal.style.display = 'none';
}

function submitUpdate() {
    const id = document.getElementById('updateModal').dataset.cardId;
    const front = document.getElementById('updateFront').value;
    const back = document.getElementById('updateBack').value;
    const tag = document.getElementById('updateTag').value;
    const data = `id=${id}&front=${encodeURIComponent(front)}&back=${encodeURIComponent(back)}&tag=${encodeURIComponent(tag)}`;

    fetch('/data', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: data
    })
    .then(response => {
        if (response.status === 200) {
            showSnackbar('Card updated successfully!');
            closeModal();
            fetchCards();
        } else {
            showSnackbar('Error updating card', '#e06c75');
        }
    })
    .catch(error => console.error('Error updating card:', error));
}

function deleteCard(id) {
    const confirmationModal = document.getElementById('confirmationModal');
    confirmationModal.style.display = 'block';

    // Store the ID in a hidden field or variable
    confirmationModal.dataset.cardId = id;
}

function confirmDelete() {
    const id = document.getElementById('confirmationModal').dataset.cardId;
    const data = `id=${id}`;

    fetch('/data', {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: data
    })
    .then(response => {
        if (response.status === 200) {
            showSnackbar('Card deleted successfully!');
            fetchCards();
        } else {
            showSnackbar('Error deleting card', '#e06c75');
        }
    })
    .catch(error => console.error('Error deleting card:', error));

    closeConfirmation(); // Close the confirmation modal after deleting
}

function closeConfirmation() {
    const confirmationModal = document.getElementById('confirmationModal');
    confirmationModal.style.display = 'none';
}

function showSnackbar(message, color = '#61afef') {
    const snackbarContainer = document.getElementById('snackbarContainer');
    
    // Create snackbar element
    const snackbar = document.createElement('div');
    snackbar.className = 'snackbar';
    snackbar.style.backgroundColor = color;
    snackbar.textContent = message;

    // Append snackbar to container
    snackbarContainer.appendChild(snackbar);

    // Trigger slide-in animation
    setTimeout(() => {
        snackbar.style.visibility = 'visible';
        snackbar.style.animation = 'slideIn 0.5s ease-in-out, fadeOut 0.5s ease-in-out 2.5s'; // Reset animation
    }, 100); // Delay to ensure visibility change is applied after appending

    // Automatically remove snackbar after animation completes
    setTimeout(() => {
        snackbar.remove();
    }, 3000);
}

// Function to open the add card modal
function openAddCardModal() {
    const addCardModal = document.getElementById('addCardModal');
    addCardModal.style.display = 'block';
}

// Function to close the add card modal
function closeAddCardModal() {
    const addCardModal = document.getElementById('addCardModal');
    addCardModal.style.display = 'none';
}

function addNewCard() {
    const form = document.getElementById('addCardForm');
    const tag = form.elements['newTag'].value;
    const front = form.elements['newFront'].value;
    const back = form.elements['newBack'].value;
    addCardToDB(front, back, tag);
}

function addCardToDB(front, back, tag) {
    fetch('/data', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `front=${encodeURIComponent(front)}&back=${encodeURIComponent(back)}&tag=${encodeURIComponent(tag)}`
    })
    .then(response => {
        if (response.status === 200) {
            showSnackbar('Card added successfully!');
            closeAddCardModal(); // Close modal after adding card
            fetchCards(); // Refresh cards table
        } else {
            showSnackbar('Error adding card', '#e06c75');
        }
    })
    .catch(error => console.error('Error adding card:', error));
}

function addAllGeneratedCards() {
    const generatedCardsTable = document.getElementById('generatedCardsTable');
    const rows = generatedCardsTable.querySelectorAll('tr');

    rows.forEach(row => {
        const front = row.cells[0].textContent;
        const back = row.cells[1].textContent;
        const tag = document.getElementById('generateTag').value; // Get the tag from the input field

        addCardToDB(front, back, tag);
    });

    closeGenerateModal();
}


function openGenerateModal() {
    const modal = document.getElementById('generateModal');
    modal.style.display = 'block';
}

function closeGenerateModal() {
    const modal = document.getElementById('generateModal');
    modal.style.display = 'none';
}

function esc(str) {
    return (str + '').replace(/[\\"']/g, '\\$&').replace(/\u0000/g, '\\0');
}

function generateCards(event) {
    event.preventDefault();
    const form = document.getElementById('generateCardForm');
    const prompt = form.elements['generatePrompt'].value;
    const amount = form.elements['generateAmount'].value;
    const tag = form.elements['generateTag'].value;

    fetch(`/generate?prompt="${esc(prompt)}"&count=${amount}&tag="${tag}"`, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
        .then(response => response.json())
        .then(data => {
            const tableContainer = document.getElementById('generatedCardsTableContainer');
            const generatedCardsTable = document.getElementById('generatedCardsTable');
            generatedCardsTable.innerHTML = ''; // Clear existing rows

            data.forEach((card, index) => {
                const row = document.createElement('tr');
                const cardId = card.id || index; // Use card.id if available, otherwise use index
                row.dataset.id = cardId; // Set data-id attribute
                row.dataset.prompt = prompt; // Set data-prompt attribute
                row.dataset.amount = amount; // Set data-amount attribute
                row.dataset.tag = tag; // Set data-tag attribute
                row.innerHTML = `
                    <td>${card.front}</td>
                    <td>${card.back}</td>
                    <td><button onclick="regenerateCard(${cardId}, '${prompt}', ${amount}, '${tag}')">Regenerate</button></td>
                `;
                generatedCardsTable.appendChild(row);
            });

            tableContainer.style.display = 'block';
        })
        .catch(error => {
            console.error('Error generating cards:', error);
            showSnackbar('Failed to generate cards');
        });
}


function regenerateCard(cardId, prompt, amount, tag) {
    fetch(`/generate?prompt="${esc(prompt)}"&count=1&tag="${tag}"`, {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        const generatedCardsTable = document.getElementById('generatedCardsTable');
        const row = generatedCardsTable.querySelector(`tr[data-id='${cardId}']`);
        if (row) {
            row.innerHTML = `
                <td>${data[0].front}</td>
                <td>${data[0].back}</td>
                <td><button onclick="regenerateCard(${cardId}, '${prompt}', ${amount}, '${tag}')">Regenerate</button></td>
            `;
        } else {
            console.error(`Row with data-id '${cardId}' not found.`);
        }
    })
    .catch(error => {
        console.error('Error regenerating card:', error);
        showSnackbar('Failed to regenerate card');
    });
}

function regenerateAll() {
    const rows = document.querySelectorAll('#generatedCardsTable tr');
    rows.forEach(row => {
        const cardId = row.dataset.id;
        const prompt = row.dataset.prompt;
        const amount = row.dataset.amount;
        const tag = row.dataset.tag;
        regenerateCard(cardId, prompt, amount, tag); // Call regenerateCard with current row data
    });
}

