# 5daysGameJam Team 2

Unity game jam project for Team 2.

## Environment

- Unity: `2022.3.62f3`
- Branch: `main`
- Git LFS: required

## First Setup

1. Install `Git`
2. Install `Git LFS`
3. Run `git lfs install`
4. Clone this repository
5. Open the project with Unity Hub using `2022.3.62f3`

## What To Commit

Commit these folders/files:

- `Assets/`
- `Packages/`
- `ProjectSettings/`

Do not commit these folders/files:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- generated solution/project files

## Team Workflow

1. Pull the latest `main`
2. Create a working branch
3. Commit in small units
4. Open a pull request or ask for review before merging to `main`

Recommended branch names:

- `feature/player-move`
- `feature/title-ui`
- `fix/jump-bug`
- `chore/project-setup`

## Unity Team Rules

- Keep `Visible Meta Files` enabled
- Keep asset serialization as `Force Text`
- Never delete `.meta` files by themselves
- Move and rename assets from inside Unity when possible
- If a scene is shared, avoid editing it at the same time as another person

## Large Files

This repository uses Git LFS for art/audio/binary assets.

If `git lfs pull` is not run correctly, some assets may appear as pointer text files instead of real data.
