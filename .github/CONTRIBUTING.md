# Contributing to Rimjob

Thanks for helping improve Rimjob.

Rimjob is an independently maintained multiplayer project by **Ignis & Avis**. Contributions should support the project's current direction: separate player ownership, shared-world multiplayer, reliable synchronisation and clear client/server behaviour.

## Before opening a pull request

- Build and test the change against the supported RimWorld version.
- Explain whether the change affects the client, server or both.
- For multiplayer behaviour, test with at least a host and one joining player where practical.
- Do not weaken pawn ownership boundaries or allow one player to control another player's pawns unless that behaviour is explicitly part of the change.
- Keep protocol or persistence changes backwards-compatible where practical, and clearly call out breaking changes.
- Include relevant logs, reproduction steps and screenshots for bug fixes or UI changes.

## AI-assisted development

AI-assisted development is allowed in this repository. Contributors remain responsible for reviewing, understanding, testing and licensing everything they submit. Generated code should not be treated as correct merely because it compiles.

## Attribution

Rimjob originated from the RimWorld Together codebase. Do not remove inherited copyright, licence or contributor attribution from code that came from the upstream project.

## Pull requests

Keep pull requests focused. A good PR explains:

1. What problem it solves.
2. What changed.
3. How it was tested.
4. Any multiplayer compatibility or migration implications.

By submitting a contribution, you confirm that you have the right to submit it under the licensing terms applicable to this repository.
