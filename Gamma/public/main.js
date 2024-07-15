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
    const form = document.getElementById('updateCardForm');
    form.elements['updateTag'].value = tag;
    form.elements['updateFront'].value = front;
    form.elements['updateBack'].value = back;
    modal.dataset.id = id; // Store the card id in modal's dataset
    modal.style.display = 'block';
}

function closeModal() {
    const modal = document.getElementById('updateModal');
    modal.style.display = 'none';
}

function submitUpdate() {
    const modal = document.getElementById('updateModal');
    const id = modal.dataset.id;
    const form = document.getElementById('updateCardForm');
    const updatedTag = form.elements['updateTag'].value;
    const updatedFront = form.elements['updateFront'].value;
    const updatedBack = form.elements['updateBack'].value;

    fetch(`/update/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tag: updatedTag, front: updatedFront, back: updatedBack })
    })
        .then(response => {
            if (!response.ok) throw new Error('Failed to update card');
            closeModal();
            fetchCards(); // Refresh the card list
            showSnackbar('Card updated successfully');
        })
        .catch(error => {
            console.error('Error updating card:', error);
            showSnackbar('Failed to update card');
        });
}

function deleteCard(id) {
    const modal = document.getElementById('confirmationModal');
    modal.style.display = 'block';
    modal.dataset.id = id; // Store the card id in modal's dataset
}

function confirmDelete() {
    const modal = document.getElementById('confirmationModal');
    const id = modal.dataset.id;

    fetch(`/delete/${id}`, {
        method: 'DELETE'
    })
        .then(response => {
            if (!response.ok) throw new Error('Failed to delete card');
            closeConfirmation();
            fetchCards(); // Refresh the card list
            showSnackbar('Card deleted successfully');
        })
        .catch(error => {
            console.error('Error deleting card:', error);
            showSnackbar('Failed to delete card');
        });
}

function closeConfirmation() {
    const modal = document.getElementById('confirmationModal');
    modal.style.display = 'none';
}

function showSnackbar(message) {
    const snackbarContainer = document.getElementById('snackbarContainer');
    const snackbar = document.createElement('div');
    snackbar.className = 'snackbar';
    snackbar.textContent = message;
    snackbarContainer.appendChild(snackbar);

    setTimeout(() => {
        snackbar.classList.add('show');
        setTimeout(() => {
            snackbar.classList.remove('show');
            setTimeout(() => {
                snackbarContainer.removeChild(snackbar);
            }, 300);
        }, 3000);
    }, 100);
}

function openAddCardModal() {
    const modal = document.getElementById('addCardModal');
    modal.style.display = 'block';
}

function closeAddCardModal() {
    const modal = document.getElementById('addCardModal');
    modal.style.display = 'none';
}

function addNewCard() {
    const form = document.getElementById('addCardForm');
    const tag = form.elements['newTag'].value;
    const front = form.elements['newFront'].value;
    const back = form.elements['newBack'].value;
    addCardToDB(front, back, tag);
}

function addCardToDB(front, back, tag) {
    fetch('/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tag: tag, front: front, back: back })
    })
        .then(response => {
            if (!response.ok) throw new Error('Failed to add card');
            closeAddCardModal();
            fetchCards(); // Refresh the card list
            showSnackbar('Card added successfully');
        })
        .catch(error => {
            console.error('Error adding card:', error);
            showSnackbar('Failed to add card');
        });
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

