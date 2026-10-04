# SyncPlay reactions: implementation plan

## Goal

While watching in a SyncPlay group, a viewer can choose from a small set of emoji reactions. Every viewer using the modified Jellyfin Web client sees the reaction briefly over the video, including the sender. Reactions do not pause playback, change the SyncPlay queue, or persist after display.

## Current architecture and scope

- This repository is the .NET server. `Jellyfin.Api/Controllers/SyncPlayController.cs` accepts authenticated SyncPlay requests. `Emby.Server.Implementations/SyncPlay/SyncPlayManager.cs` maps sessions to groups, and `Group.cs` sends group updates through `ISessionManager.SendSyncPlayGroupUpdate` over the existing WebSocket connection.
- The group update wire model lives in `MediaBrowser.Model/SyncPlay/`, including `GroupUpdateType`, `GroupUpdate<T>`, and the concrete update types.
- The visible player and SyncPlay controls live in the separate `jellyfin/jellyfin-web` repository. This repo's `Dockerfile.local` currently copies a prebuilt web client from `jellyfin/jellyfin:unstable`; editing this server alone cannot add the reaction button or animation.
- First version targets Jellyfin Web. Other clients will not display reactions until they implement the new event. Existing clients should continue to play normally.

## Proposed behavior

1. Show a reaction button in the video player's controls only when the current session belongs to a SyncPlay group. Open a small picker with a fixed set, initially: 👍 ❤️ 😂 😮 👏 🎉. Use stable IDs such as `like`, `heart`, `laugh`, `wow`, `clap`, and `celebrate` on the wire; map IDs to emoji in the client.
2. Clicking a choice sends one authenticated request to the server. The server identifies the sender from the session, checks that it is still in a group, validates the ID against the allowlist, and limits how often a session may react (proposed: at most one per second). The client must not supply a user name, group ID, arbitrary text, or arbitrary Unicode for the server to trust.
3. The server broadcasts a small event containing the group ID, reaction ID, and sender's user ID/display name to **all** sessions in that group. The sender sees the same server event as everyone else, so timing and behavior stay consistent.
4. Each updated Web client checks that the event belongs to its current group, queues a short overlay animation (about 2–3 seconds), and removes it. Cap simultaneous overlays and clear them when leaving the group, changing videos, or closing the player. Reactions have no database or playback state.

## Step-by-step work

### 1. Prepare the two repositories and a repeatable build

1. Check out `jellyfin-web` beside this repository (for example, `../jellyfin-web`) at a revision compatible with this server's 13.0 development branch. Record the exact web commit used during development.
2. Find the web client's SyncPlay service, WebSocket `SyncPlayGroupUpdate` handler, player control components, and localization patterns before editing. Confirm how it represents the active group and how it unsubscribes from events when a player closes.
3. Update `Dockerfile.local` and `compose.yaml` to build the local web checkout with its required Node/npm versions and copy its `dist` output into `/jellyfin/jellyfin-web`. Use an explicit second build context or an equivalent local build workflow; avoid silently pulling a prebuilt UI when testing reactions.
4. Keep the existing `/config`, `/cache`, and `/media` mounts. Confirm `docker compose up -d --build` serves the custom client and the compiled server; expose a visible build identifier or inspect the image contents so an old cached web bundle is not mistaken for the new one.

### 2. Add a reaction request and event to the server

1. Add a request DTO under `Jellyfin.Api/Models/SyncPlayDtos/` with only a reaction ID. Validate missing, unknown, and oversized values. Document the allowlist in one server-side location.
2. Add `POST /SyncPlay/Reaction` to `SyncPlayController` with the existing SyncPlay access and in-group authorization policies. Resolve the session with `RequestHelpers.GetSession`, as the other SyncPlay endpoints do. Return `204` when accepted, `400` for an invalid reaction, `401/403` through existing authorization, and a clear rate-limit response (prefer `429`) for rapid repeats.
3. Add a reaction payload and concrete group update in `MediaBrowser.Model/SyncPlay/`. Append `Reaction` to `GroupUpdateType` without renumbering existing enum values. Include a group ID and server-derived sender identity in the event. Use the established `GroupUpdate<T>` and `SyncPlayGroupUpdate` WebSocket envelope so existing clients can ignore the unfamiliar update type.
4. Add an operation on `ISyncPlayManager`/`SyncPlayManager` that looks up the sender's **current** group, rechecks membership under the group lock, applies the per-session limit, and sends the reaction update to that group's participants. Do not route through the playback state machine: reactions must work when playing, paused, or waiting and must not alter queue/state timestamps.
5. Snapshot recipient session IDs while holding the group lock, then send/await WebSocket tasks without holding the lock. Ensure a session that leaves or switches groups cannot broadcast to its previous group. Decide whether a temporarily disconnected member is skipped without failing delivery to other members.

### 3. Add the Jellyfin Web interaction

1. Add the picker to the player's existing control system. Make it usable with mouse, touch, and keyboard, with accessible labels and a close/Escape path. Show it only while joined to a SyncPlay group.
2. Send the selected reaction through the client's API layer. Disable or briefly throttle the picker after sending; show a small non-blocking error if the request fails or the session has left the group.
3. Extend the WebSocket SyncPlay group-update handler with the new reaction type. Filter by current group ID, map the stable reaction ID to the local emoji, and render an overlay that does not intercept player controls. Respect reduced-motion preferences with a static or minimal-motion variant.
4. Bound the overlay queue and timers. Dispose listeners and timers on group leave, player teardown, navigation, and account switch. Ignore unknown reaction IDs from newer servers gracefully.
5. Add any user-facing labels to the web client's localization files using its existing translation workflow.

### 4. Verify behavior and compatibility

1. Server tests: accepted reaction reaches exactly the current group's sessions; no cross-group delivery; sender identity cannot be spoofed; invalid IDs and non-members are rejected; rate limiting works; reaction does not change playback state. Add controller authorization/validation coverage alongside `tests/Jellyfin.Server.Integration.Tests/Controllers/SyncPlayControllerTests.cs` and manager tests alongside `tests/Jellyfin.Server.Implementations.Tests/SyncPlay/`.
2. Web tests: picker visibility and keyboard behavior; event-to-emoji mapping; overlay cleanup; unknown IDs; errors and rapid clicks. Run the web repo's relevant unit/lint/build commands.
3. Manual two-viewer check: open separate browser sessions, join one SyncPlay group, play a video, react from each viewer, and confirm both see each reaction once. Repeat while paused, after one viewer leaves, and with two separate groups. Confirm seek/pause synchronization remains unchanged.
4. Test the rebuilt Compose image. Verify the custom web assets are loaded, the server logs show no WebSocket or serialization errors, and the video still plays. Keep `/config` data backed up before testing against a newer unstable server build because schema migrations may change it.

## Completion criteria

- A group member can choose a reaction during playback and all members on the modified Web client see it promptly and briefly.
- Reactions are scoped to the sender's current group, validated, rate limited, and never persisted or used to change playback state.
- Leaving the group removes the control and pending overlays; unknown reaction events do not break older or newer clients.
- The local Compose build includes both modified repositories, and automated plus two-viewer manual checks pass.

## Decisions to settle before UI polish

- Final emoji list and whether to show the sender's name beside the emoji. The first implementation can use the six IDs above and a name label.
- Whether reactions should also show on non-video screens while a group is active. This plan limits them to the video player.
- Whether other Jellyfin clients need support in the first release. This plan covers Jellyfin Web only.

## Update after first implementation

- The web changes now live in the sibling `media-server-frontend/` fork. Compose builds that checkout directly.
- The reaction picker stays open after selecting an emoji. The overlay shows only the emoji, at varied positions over video, with upward motion and a fade. The server still derives sender identity for the event, but the web overlay does not show it.
- The session rate limit is 300 ms to support repeated reactions while limiting bursts. The client uses the same interval.
