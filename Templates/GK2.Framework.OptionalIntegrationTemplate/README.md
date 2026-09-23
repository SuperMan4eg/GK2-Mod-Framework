# Optional integration template

This template demonstrates a standalone BepInEx mod plus a separate optional GK2 Mod Framework bridge.

- `Main/` must remain free of references to `GK2.Framework.dll`.
- `FrameworkBridge/` may reference the framework and the main mod.
- If GK2 Mod Framework is absent, BepInEx skips only the bridge and the main mod still loads.
- The bridge can register the main mod's existing `ConfigFile` so matching Section/Key/type bindings reuse the same `ConfigEntry<T>` values.

See `docs/OPTIONAL_INTEGRATION.md` for the tested pattern and limitations.
