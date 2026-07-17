# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Role

`MonoGame` is a forked copy of the MonoGame graphics framework, consumed by **Visual Music** as a
`ProjectReference` to the Windows DirectX build. Visual Music uses it for real-time 3D note visualization
(via `SongRenderer` / `MonoGameHost`) and for building `.fx` content through the MonoGame content pipeline.

- Project referenced by the app:
  [MonoGame.Framework/MonoGame.Framework.WindowsDX.csproj](MonoGame.Framework/MonoGame.Framework.WindowsDX.csproj)
- Built as part of the repo-root `VisualMusic.sln`.
- Content: Visual Music's `Content/Content.mgcb` is built by the MonoGame content builder tool (pinned in
  [../../VisualMusic/.config/dotnet-tools.json](../../VisualMusic/.config/dotnet-tools.json)); built `.xnb`
  files are copied into the app output `Content\` folder.

## Working in this fork

This is a large upstream fork. Treat it as upstream and change only what Visual Music requires — keep
patches minimal so future merges stay clean. Prefer app-side workarounds in `VisualMusic/` when possible.

See [../../CLAUDE.md](../../CLAUDE.md) for the repo-wide picture and
[../../VisualMusic/CLAUDE.md](../../VisualMusic/CLAUDE.md) for the rendering pipeline.
