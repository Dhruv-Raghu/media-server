# Backend fork guidance

This is the Jellyfin backend fork. Read [DEVELOPMENT.md](DEVELOPMENT.md) for setup
and [feature development](docs/FEATURE_DEVELOPMENT.md) for changes spanning the web client.

## Architecture

- `Jellyfin.Server/Program.cs`, `Startup.cs`, and `CoreAppHost.cs`: startup and hosting.
- `Jellyfin.Api/Controllers/`, `Models/`, and `Auth/`: endpoints, request DTOs, authorization.
- `Emby.Server.Implementations/`: core library, session, and SyncPlay services;
  `ApplicationHost.cs` registers many services.
- `MediaBrowser.Controller/`: service interfaces and server contracts.
- `MediaBrowser.Model/`: shared API and WebSocket models.
- `Jellyfin.Server.Implementations/` and `src/Jellyfin.Database/`: newer services,
  EF Core persistence, and the default SQLite provider.
- `MediaBrowser.MediaEncoding/` and `src/Jellyfin.MediaEncoding.*`: media processing.
- `tests/`: unit and integration test projects.

The Emby/MediaBrowser names are inherited architecture. Follow existing boundaries
rather than renaming or moving unrelated code.

## Changes and verification

- Follow `.editorconfig` and project analyzers. Nullable checking is enabled;
  warnings are generally errors. Use async operations and cancellation tokens consistently.
- Keep authorization and request validation at the API boundary. Derive user/session
  identity on the server rather than trusting client-supplied identity.
- Preserve wire contracts and existing enum values. For SyncPlay, keep transient UI
  events separate from playback state and persistence.
- Add meaningful tests for behavior, permissions, isolation, and lifecycle changes.
  Use the relevant project/filter first; broaden checks when scope requires it.
- Typical commands: `dotnet build Jellyfin.sln` and
  `dotnet test tests/Jellyfin.Server.Implementations.Tests --filter FullyQualifiedName~SyncPlayManagerTests`.
  API and integration tests live in separate projects; choose those when changing endpoints.
- Verify integrated behavior with the sibling frontend using Compose when relevant.
  The Docker build excludes tests and does not run them.
- Report missing tools or unrun checks honestly; do not treat old verification notes as current results.

## Repository workflow

Use Conventional Commits, such as `feat(syncplay): add reactions` or
`fix(api): reject invalid input`. Keep unrelated edits out of commits.
Frontend work belongs in `../media-server-frontend`, not the ignored `jellyfin-web/` copy.
Do not commit media, runtime data, secrets, or generated outputs. Update feature notes
when behavior or verification changes, clearly distinguishing plans from completed work.
