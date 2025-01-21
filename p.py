#!/usr/bin/env python3
from dprojectstools.commands import command, CommandsManager
from dprojectstools.secrets import SecretsManager
from dprojectstools.git import GitManager
from dprojectstools.docker import DockerManager
import sys

# object
secrets = SecretsManager("cett")
gitManager = GitManager()
dockerManager = DockerManager("nas3")


# ***************
# **  Execute  **
# ***************
commandsManager = CommandsManager()
commandsManager.register(dockerManager)
commandsManager.register(gitManager)
commandsManager.execute(sys.argv)
