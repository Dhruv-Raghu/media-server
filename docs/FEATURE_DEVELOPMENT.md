# Adding features across the forks

Start with [local setup](../DEVELOPMENT.md) and each repository's `AGENTS.md`.
Identify whether the change affects the server, client, or their shared contract before editing.

## Trace a feature through the system

SyncPlay reactions provide an existing example:

| Layer | Entry point | Responsibility |
| --- | --- | --- |
| Player UI | frontend `src/apps/legacy/controllers/playback/video/index.html` and `index.js` | Picker, request action, event subscription, overlay cleanup |
| Client request | frontend `src/plugins/syncPlay/core/Controller.js` | Authenticated `POST /SyncPlay/Reaction` |
| API | `Jellyfin.Api/Controllers/SyncPlayController.cs` and `Models/SyncPlayDtos/ReactionRequestDto.cs` | Validation, access/in-group policies, session lookup, status codes |
| Service | `Emby.Server.Implementations/SyncPlay/SyncPlayManager.cs` and `Group.cs` | Current membership, locking, throttling, group-scoped delivery |
| Contracts | `MediaBrowser.Controller/SyncPlay/` and `MediaBrowser.Model/SyncPlay/` | Service interface, result, payload, update type |
| Transport | `Emby.Server.Implementations/Session/SessionManager.cs` | Existing `SyncPlayGroupUpdate` WebSocket envelope |
| Client event | frontend `src/plugins/syncPlay/core/Manager.js` | Filter current group, dispatch reaction event |
| Presentation | frontend `src/styles/videoosd.scss` and `src/strings/en-us.json` | Animation, reduced motion, source-language labels |

The current endpoint accepts `{ "ReactionId": "heart" }`; it returns 204 on delivery,
400 for invalid input, 403 when membership is absent, and 429 for throttling, with
existing authentication/authorization also applied. The update includes `GroupId`,
`Type: "Reaction"`, and `Data` containing `ReactionId`, `UserId`, and `UserName`.
The server derives identity; the current UI displays only the emoji.

## Implementation workflow

1. Define observable behavior and the smallest contract. Specify permissions, invalid
   input, lifecycle, failures, and whether data is persistent or transient.
2. Follow existing API/service/model boundaries. For group operations, recheck membership
   under the relevant lock; await network tasks outside the lock. Avoid coupling transient
   events to the playback state machine.
3. Preserve enum values and established serialized names. Consider clients that don't
   implement the new feature. Verify compatibility rather than assuming it.
4. Prefer the generated TypeScript SDK for frontend API calls. Fork-only endpoints may
   need a small authenticated adapter until SDK support exists; do not hand-edit generated code.
5. Find the actual view used by each relevant layout. The modern video route reuses the
   legacy view, so changing only a React wrapper may miss the shared player controls.
6. Add accessible labels, keyboard/touch behavior, reduced-motion support, bounded transient
   UI, and teardown on navigation/group changes. Keep localization additions in `en-us.json`.
7. Test the behavior at the lowest useful layer, then verify the integrated client/server path.
   Commit separately in both repositories using Conventional Commits.

## Verification for group/player features

Choose checks based on the change. For reactions, useful cases include valid/invalid input,
permissions, server-derived identity, delivery to the sender and current group only,
rate limiting, leaving/switching groups, and unchanged pause/seek/queue behavior.

Relevant backend test locations:

- `tests/Jellyfin.Server.Implementations.Tests/SyncPlay/SyncPlayManagerTests.cs`
- `tests/Jellyfin.Server.Integration.Tests/Controllers/SyncPlayControllerTests.cs`

For a manual watch-party check, use two independent browser profiles/sessions, join the
same group, play media, and send from each viewer. Repeat while paused, after leaving,
and with separate groups. Exercise relevant layouts, keyboard controls, overlay cleanup,
and reduced motion. Ensure the rebuilt image serves both modified sources.

Record the commands and outcomes, source revisions, and remaining gaps in feature notes.
Use historical verification as context, not a claim that new changes passed tests.
