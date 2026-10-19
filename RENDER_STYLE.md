# Blocky 3D: the models as lit geometry

Part of the rendering-style comparison: see `..\README.md` for the verdicts, measurements and
pictures. Shared plumbing lives in `render/base`; `git diff base/render/base` shows this style's own
change only. Run with `dotnet run --project Proto4X.Factory/Game`. Headless: `snapshot <dir>`,
`picking`, `bench-draw`, `sprites <dir>` (from `Proto4X.Factory/Headless`).

Keys: `Z`/`X` yaw, `PgUp`/`PgDn` pitch, wheel distance, `F2` perspective/orthographic, `F3` true
isometric (orthographic).

Look for: the ship at any yaw; `F3` (isometric from the same models); maximum pitch + `F2` (the
top-down look). Zoom out across distance 70 and 140 to see the two LOD steps. Frame cost climbs at mid
zoom over the F5 blocks - nothing is cached between frames yet.
