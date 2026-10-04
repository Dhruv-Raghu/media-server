# SyncPlay reactions development

The .NET server changes live in `media-server/`. The web player changes live in the sibling `media-server-frontend/` checkout, which is the user's Jellyfin Web fork (`https://github.com/Dhruv-Raghu/media-server-frontend`). Both checkouts started from compatible 13.0 development revisions; the frontend base commit was `2096bfaf1732c43f05d4a44462953ae06696648e`.

From `media-server/`, build and start both local sources with:

```bash
docker compose up -d --build
```

Compose uses `../media-server-frontend` as its `web` build context. The old ignored `media-server/jellyfin-web/` checkout is no longer used by Compose. Frontend edits should be made in `media-server-frontend/` and committed in that repository.

The picker appears in the video controls while joined to a SyncPlay group. It remains open after a selection so viewers can react again. The server broadcasts each reaction to the current group; updated web clients show a nameless emoji at a varied position over the video, floating upward and fading out. Reactions are limited to one per session every 300 ms. Other Jellyfin clients continue playing but do not display the reactions.

## Verification

- The original server reaction implementation passed the focused `SyncPlayManagerTests` run (7 tests).
- The original web bundle and server built in Docker, and a reaction selected in the player appeared over video.
- The updated frontend fork and server built successfully in Docker; the container is healthy.
- In the player, consecutive reactions kept the picker open. A received emoji rendered without a name, and its starting position fell inside the visible letterboxed video area.
