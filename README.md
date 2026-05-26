# ClassicAssist (Avalonia port)

A cross-platform port of [Reetus/ClassicAssist](https://github.com/Reetus/ClassicAssist) — an assistant for [ClassicUO](https://github.com/andreakarasho/ClassicUO) with a UOSteam-like interface and Python macro syntax.

This is a **from-scratch WPF→Avalonia port**, unrelated to the older [Reetus/ClassicAssist.Avalonia](https://github.com/Reetus/ClassicAssist.Avalonia) attempt. The macro language, behavior, and overall UX are intentionally identical to upstream; the underlying UI framework is the only thing that changed.

## Status

Lightly live-tested on:

- **Windows 11**
- **Ubuntu** (Wayland session)

It is not heavily exercised yet — expect rough edges. Bug reports welcome.

## Requirements

- **.NET 10 SDK** (10.0.108 or newer)
- **ClassicUO 1.1.x** ([andreakarasho/ClassicUO](https://github.com/andreakarasho/ClassicUO))
  - On Linux, the Mono-hosted build (`./ClassicUO.bin.x86_64` via `MONO_THREADS_SUSPEND=preemptive`) works.
- A copy of the Ultima Online client files configured in ClassicUO's `settings.json` (`ultimaonlinedirectory`).

## How it loads

ClassicUO 1.1.x's plugin host is a `net48` (.NET Framework / Mono) entry point. This port runs on `net10.0` (modern .NET) for Avalonia. To bridge the two, the build produces a small **`net48` `ClassicAssist.PluginLoader.dll`** that CUO loads in-process; the loader then dlopens `libhostfxr` and starts a side-by-side **CoreCLR** to host the real `net10.0` `ClassicAssist.dll`.

The CUO `settings.json` entry points at the **loader**, not at `ClassicAssist.dll` directly.

## Install (binary)

1. Download the latest release zip from [Releases](https://github.com/Kazmaier001/ClassicAssist-Avalonia/releases).
2. Unzip into a directory of your choice, e.g. `~/ClassicAssist/` on Linux or `C:\Tools\ClassicAssist\` on Windows.
3. Edit ClassicUO's `settings.json` and add the loader to the `plugins` array:
   ```jsonc
   "plugins": [
     "/full/path/to/ClassicAssist/ClassicAssist.PluginLoader.dll"
   ]
   ```
4. Launch ClassicUO normally. The assistant window opens once you're logged in.

### Updates

There is **no in-app updater** in this port. The version-check and updater paths inherited from upstream point at Reetus's WPF release manifest, which would either nag with misleading "new version" messages or overwrite the install with an incompatible WPF build — so both are disabled. Check the releases page manually and re-do the unzip/copy step to update.

## Build from source

Two projects need building: the modern `net10` plugin and the `net48` loader shim.

```sh
git clone https://github.com/Kazmaier001/ClassicAssist-Avalonia.git
cd ClassicAssist-Avalonia

dotnet build ClassicAssist/ClassicAssist.csproj            -c Release
dotnet build ClassicAssist.PluginLoader/PluginLoader.csproj -c Release
```

### Stage a deployable plugin directory

**Linux / macOS (bash):**

```bash
rm -rf Output/Plugin && mkdir Output/Plugin

# Full net10 closure (Avalonia + Skia natives + ClassicAssist):
cp -r Output/net10.0/. Output/Plugin/

# net48 loader + its pdb:
cp Output/PluginLoader/ClassicAssist.PluginLoader.dll \
   Output/PluginLoader/ClassicAssist.PluginLoader.pdb \
   Output/Plugin/
```

**Windows (PowerShell):**

```powershell
Remove-Item -Recurse -Force Output\Plugin -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force Output\Plugin | Out-Null

# Full net10 closure (Avalonia + Skia natives + ClassicAssist):
Copy-Item -Recurse Output\net10.0\* Output\Plugin\

# net48 loader + its pdb:
Copy-Item Output\PluginLoader\ClassicAssist.PluginLoader.dll, `
          Output\PluginLoader\ClassicAssist.PluginLoader.pdb `
          -Destination Output\Plugin\
```

Point ClassicUO's `settings.json` `plugins` entry at `Output/Plugin/ClassicAssist.PluginLoader.dll`.

### Iterating on the plugin only

After the closure is staged, you can ship just the plugin DLLs without re-deploying the whole NuGet closure.

**Linux / macOS (bash):**

```bash
cp -f Output/net10.0/ClassicAssist.dll \
      Output/net10.0/ClassicAssist.pdb \
      Output/net10.0/ClassicAssist.Controls.dll \
      Output/net10.0/ClassicAssist.Controls.pdb \
      Output/net10.0/ClassicAssist.Shared.dll \
      Output/net10.0/ClassicAssist.Shared.pdb \
      Output/Plugin/
```

**Windows (PowerShell):**

```powershell
Copy-Item -Force Output\net10.0\ClassicAssist.dll, `
                 Output\net10.0\ClassicAssist.pdb, `
                 Output\net10.0\ClassicAssist.Controls.dll, `
                 Output\net10.0\ClassicAssist.Controls.pdb, `
                 Output\net10.0\ClassicAssist.Shared.dll, `
                 Output\net10.0\ClassicAssist.Shared.pdb `
                 -Destination Output\Plugin\
```

If you bump a NuGet package version (especially one with native assets like SkiaSharp), do a full clean+copy of `Output/net10.0/` into `Output/Plugin/` again — partial copies will lose updated `runtimes/linux-x64/native/` (or `runtimes/win-x64/native/`) payloads.

### Launcher

`ClassicAssist.Launcher` still targets `net10.0-windows` and is Windows-only. Linux/macOS users should launch ClassicUO directly with the `plugins` setting wired up as above.

## Macros

Same Python-based macro language as upstream ClassicAssist. Macro command reference:

[Macro Commands (upstream wiki)](https://github.com/Reetus/ClassicAssist/wiki/Macro-Commands)

## Known limitations

### Linux / Wayland

- **GIF recorder is stubbed** on Wayland. The X11 capture path (`XGetImage`) doesn't work under rootless XWayland, and a per-frame `gnome-screenshot` shell-out is too slow (~200 ms/frame) to be usable. A proper xdg-desktop-portal `ScreenCast` (PipeWire) implementation is the right fix and hasn't been written yet.
- **Per-window ("UO Only") screenshots** require triggering via a bound **hotkey** — the menu/UI path falls back to fullscreen.
- **Capturing CUO when another window is on top is not possible on Wayland.** There is no Wayland equivalent of Windows' `PrintWindow PW_RENDERFULLCONTENT`; by design, Wayland clients cannot read each other's surfaces.
- A few Win32-only features (some `NativeMethods.cs` paths) gracefully degrade on Linux rather than crash.

### General

- This port has not been broadly community-tested — assume any non-mainline feature path may have a porting bug. Please file reports.

## Differences from upstream

- **UI framework**: Avalonia 11 (cross-platform) instead of WPF.
- **Runtime**: targets `net10.0`; loaded into CUO's `net48` plugin host via an embedded CoreCLR (see *How it loads* above).
- **Entry point** in `settings.json` is `ClassicAssist.PluginLoader.dll`, not `ClassicAssist.dll`.
- Macro language, profile format, hotkey system, and on-screen behavior are intended to match upstream as closely as the framework swap allows.

## Issues / Feature Requests

Open an issue at <https://github.com/Kazmaier001/ClassicAssist-Avalonia/issues>.

If you hit something this port broke that works on upstream WPF ClassicAssist, please say so explicitly in the report — it helps separate "port regression" from "upstream behavior I don't like."

## Community

For general ClassicUO questions, the official ClassicUO Discord is the right place: <https://discord.gg/classicuo-458277173208547350>.

There is no dedicated community channel for this port yet.

## Credits

This is fundamentally **Reetus's work** — every feature, every macro command, the entire macro language and UX, the profile system, the hotkey engine, the agents, the filters. The port only swaps the UI framework underneath. All gratitude for the assistant itself belongs upstream: <https://github.com/Reetus/ClassicAssist>.

Icons made by [FreePik](https://www.flaticon.com/authors/freepik) and [SmashIcons](https://www.flaticon.com/authors/smashicons) from [FlatIcon](https://www.flaticon.com/).

## License

GNU General Public License v3.0 or later — same as upstream.

```
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
```

See [LICENSE.txt](LICENSE.txt) for the full text.
