#!/usr/bin/env python3
from dprojectstools.commands import command, CommandsManager
from dprojectstools.git import GitManager
import sys
import subprocess

# object
gitManager = GitManager()

# commands
@command()
def Nuget_publish():
    # execute the publish_nuget.py script
    print("Executing publish_nuget.py script...")
    subprocess.run(["python", "scripts/publish_nuget.py", "--publish"], check=True)

@command()
def Nuget_incremental_publish_dryrun():
    # execute
    print("Executing publish_nuget_autoincrement_version.py ...")
    subprocess.run(["python", "scripts/publish_nuget_autoincrement_version.py"], check=True)

@command()
def Nuget_incremental_publish():
    # execute the publish_nuget_autoincrement_version.py 
    print("Executing publish_nuget_autoincrement_version.py ...")
    subprocess.run(["python", "scripts/publish_nuget_autoincrement_version.py", "--publish"], check=True)

# ***************
# **  Execute  **
# ***************
commandsManager = CommandsManager()
commandsManager.register()
commandsManager.register(gitManager)
commandsManager.execute(sys.argv)
