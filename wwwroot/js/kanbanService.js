const KanbanService = {
    getUserStories: function() {
        return JSON.parse(localStorage.getItem('userStories')) || [];
    },

    saveUserStories: function(userStories) {
        console.log(userStories);
        localStorage.setItem('userStories', JSON.stringify(userStories));
    },

    addUserStory: function(userStory) {
        const userStories = this.getUserStories();
        userStories.push(userStory);
        this.saveUserStories(userStories);
    },

    updateUserStoryStatus: function(id, status) {
        const userStories = this.getUserStories();
        const storyIndex = userStories.findIndex(story => story.id === id);
        if (storyIndex > -1) {
            userStories[storyIndex].status = status;
            this.saveUserStories(userStories);
        }
    }
};
