# Developing the media-server forks

These are separate forks of Jellyfin's 13.0 development sources. The upstream READMEs
remain useful for Jellyfin generally; this guide describes the local paired setup.

## Checkout layout

```text
dial/                         # workspace, not a Git repository
├── media-server/              # backend repository
└── media-server-frontend/     # frontend repository
```

To create that layout:

```sh
mkdir -p dial
cd dial
git clone https://github.com/Dhruv-Raghu/media-server.git
git clone https://github.com/Dhruv-Raghu/media-server-frontend.git
```

The backend's ignored `jellyfin-web/` folder is an older local copy. Compose builds
`../media-server-frontend`; edit and commit frontend changes there.

## Run both through Docker

Prerequisites: a running Docker daemon and Docker Compose with BuildKit support for
`additional_contexts`. This route builds Node/.NET code inside containers.

From `media-server/`:

```sh
mkdir -p media
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 jellyfin
```

Open `http://localhost:8096`. API documentation is at
`http://localhost:8096/api-docs/swagger/index.html`. Add `/media` as a library path
in server setup after placing test media in the local `media/` folder.

`Dockerfile.local` builds the sibling frontend with Node 24 using
`npm run build:development`, publishes the backend with .NET 10, and copies both
into `jellyfin/jellyfin:unstable`, which supplies FFmpeg/native dependencies.
The image is intended for development; the final base tag is mutable. Record source
commits and the image used when reproducing a problem.

Compose exposes TCP 8096 and UDP 7359. Named volumes persist `/config` and `/cache`;
`./media` is mounted read-only at `/media`. Source edits require rebuilding the image;
this workflow does not provide live reload.

```sh
docker compose logs -f jellyfin
docker compose down
```

`down` preserves named volumes. `down -v` deletes them, including server configuration
and database data. Use disposable data for development and back up existing configuration
before running newer unstable code that may migrate the database.

## Native backend with a built web client

Install the .NET SDK compatible with `global.json` (target .NET 10), Node >=24,
npm >=11, and Jellyfin FFmpeg. From `media-server-frontend/`:

```sh
npm ci
npm run build:development
```

From `media-server/`, use separate disposable paths for development data:

```sh
dotnet run --project Jellyfin.Server --no-launch-profile -- \
  --webdir ../media-server-frontend/dist \
  --datadir /tmp/dial-jellyfin-data \
  --cachedir /tmp/dial-jellyfin-cache
```

If FFmpeg isn't found in PATH, add `--ffmpeg /absolute/path/to/ffmpeg`.
Stop the Compose server first if it occupies 8096. This mode serves the built client
and supports first-time setup. Do not point a native process at a database currently
in use by another server.

## Frontend live reload against the backend

Complete first-time server setup using the integrated client above or Compose. Then
keep a backend running and start the frontend from `media-server-frontend/`:

```sh
npm ci
npm start
```

Open the URL printed by Webpack and select/connect to `http://localhost:8096` when
prompted. Use the same hostname consistently for server selection and authentication.
For an API-only native backend, replace `--webdir ...` with `--nowebclient` in the
native command. The upstream README notes setup limitations with a separately hosted
client, so complete setup before using that mode.

The app uses Webpack. `vite.config.ts` configures Vitest; it is not a Vite application.

## Verification and troubleshooting

Run backend tests separately from Docker builds:

```sh
dotnet build Jellyfin.sln
dotnet test tests/Jellyfin.Server.Implementations.Tests --filter FullyQualifiedName~SyncPlayManagerTests
```

For frontend work, run the relevant type, lint, style, test, and build commands in
[the frontend package](../media-server-frontend/package.json); its `AGENTS.md` lists examples.

- Docker socket errors: start the Docker daemon and retry `docker compose ps`.
- Missing .NET SDK: use the container build or install a compatible SDK for native tests.
- Missing web build context: restore the sibling directory layout or update Compose explicitly.
- Old UI: rebuild both sources, reload the browser, and inspect loaded assets/browser console.
  The client logs its version/build/commit at startup; a commit alone does not identify
  uncommitted source changes.
- Playback problems: check server logs, FFmpeg availability, and library access first.

For feature implementation, see [FEATURE_DEVELOPMENT.md](docs/FEATURE_DEVELOPMENT.md).
Current reaction behavior and historical verification are documented in
[the reaction notes](task-history/syncplay-reactions/README.md).
