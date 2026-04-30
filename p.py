#!/usr/bin/env python3
from dprojectstools.commands import command, CommandsManager
from dprojectstools.git import GitManager
import sys

# object
gitManager = GitManager()


# ***************
# **  Execute  **
# ***************
commandsManager = CommandsManager()
commandsManager.register(gitManager)
commandsManager.execute(sys.argv)
