# Team Workflow

## Daily Flow

1. `git switch main`
2. `git pull origin main`
3. `git switch -c feature/your-task`
4. Work in Unity
5. `git status`
6. `git add .`
7. `git commit -m "feat: short summary"`
8. `git push -u origin feature/your-task`

## Merge Rules

- Do not push direct gameplay changes to `main`
- Merge to `main` after review or team confirmation
- If a conflict happens in a scene or prefab, resolve it together with the person who touched it

## Commit Message Examples

- `feat: add player movement`
- `fix: correct jump landing logic`
- `chore: update input settings`
- `docs: add team workflow`

## Unity Conflict Tips

- `*.unity`, `*.prefab`, `*.mat`, `*.asset` can conflict easily
- One owner per scene is safer during a short game jam
- Prefer additive scenes or split responsibilities when possible
- Test the project in Unity after resolving conflicts
