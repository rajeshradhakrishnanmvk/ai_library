document.addEventListener('DOMContentLoaded', function() {
    loadUserStories();
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('./js/serviceWorker.js').then(function() {
            console.log('Service Worker Registered');
        });
    }

    
    document.querySelectorAll('.story-list').forEach(list => {
        list.addEventListener('dragover', allowDrop);
        list.addEventListener('drop', drop);
    });

});

function loadUserStories() {
    const userStories = KanbanService.getUserStories();
    const backlog = document.querySelector('#backlog .story-list');
    const inProgress = document.querySelector('#in-progress .story-list');
    const done = document.querySelector('#done .story-list');

    backlog.innerHTML = '';
    inProgress.innerHTML = '';
    done.innerHTML = '';

    userStories.forEach(story => {
        const li = createStoryElement(story);
        if (story.status === 'backlog') {
            backlog.appendChild(li);
        } else if (story.status === 'in-progress') {
            inProgress.appendChild(li);
        } else if (story.status === 'done') {
            done.appendChild(li);
        }
    });
}

function createStoryElement(story) {
    const li = document.createElement('li');
    li.setAttribute('draggable', 'true');
    li.setAttribute('data-id', story.id);
    li.setAttribute('ondragstart', 'drag(event)');
    li.setAttribute('ondragover', 'allowDrop(event)');
    li.innerHTML = `<strong>${story.title}</strong>`;
    return li;
}

function addUserStory() {
    const title = document.getElementById('user-story-title').value;
    if (title) {
        const userStory = { id: Date.now(), title, status: 'backlog' };
        KanbanService.addUserStory(userStory);
        loadUserStories();
        document.getElementById('user-story-title').value = '';
    } else {
        alert('User story title is required');
    }
}

function allowDrop(event) {
    event.preventDefault();
    event.stopPropagation(); // Ensure event doesn't propagate further
}

function drag(event) {
    event.dataTransfer.setData('text', event.target.getAttribute('data-id'));
    console.log(event.target.getAttribute('data-id'));
}

/**
 * Handles the drop event when an element is dropped onto a target element.
 * @param {Event} event - The drop event object.
 */
function drop(event) {
    event.preventDefault();
    event.stopPropagation(); // Ensure event doesn't propagate further

    const id = event.dataTransfer.getData('text');
    const target = event.target;
    const status = target.getAttribute('data-status');
    
    if (status && target.classList.contains('story-list')) {
        const stories = KanbanService.getUserStories(); // Fetch the list of stories

        // Find the index of the updated story in the stories array
        const updatedStoryIndex = stories.findIndex(story => story.id === Number(id));
        
        // If the story is found in the array
        if (updatedStoryIndex !== -1) {
            // Update the status of the story
            stories[updatedStoryIndex].status = status;
            
            // Save the updated list of stories back to the database
            KanbanService.saveUserStories(stories);
            loadUserStories()
        }
    }
}

document.querySelectorAll('.story-list').forEach(list => {
    list.addEventListener('dragover', allowDrop);
    list.addEventListener('drop', drop);
});
