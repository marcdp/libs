#!/usr/bin/env dish

#build



# git
function menuitem_git_pull --index 90
    git pull 
end 
function menuitem_git_status
    git status --short
end 
function menuitem_git_add
    git add . --all
end
function menuitem_git_commit
    read --prompt "Enter changes: " GIT_COMMIT_MESSAGE
    git commit -a -m "$GIT_COMMIT_MESSAGE"
end
 
function menuitem_git_push 
    git push
end


# menu
menu 