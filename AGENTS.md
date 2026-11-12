# Kursor — Agent & Orchestrator Instructions

Whenever working in `C:\dev\heyclicky-clone` (including after context compaction or inside a subagent):

1. **Always read [`docs/INDEX.md`](file:///C:/dev/heyclicky-clone/docs/INDEX.md) first.**
2. It indexes our complete reverse-engineered specifications of **HeyClicky v1.1.5**:
   - [`docs/01-architecture-overview.md`](file:///C:/dev/heyclicky-clone/docs/01-architecture-overview.md) — Architecture, assemblies, sidecars, native DLLs, and `%LOCALAPPDATA%\HeyClicky` schemas.
   - [`docs/02-api-contracts-and-worker-spec.md`](file:///C:/dev/heyclicky-clone/docs/02-api-contracts-and-worker-spec.md) — Full REST/SSE API client & backend worker specification.
   - [`docs/03-voice-ears-mouth-dictation.md`](file:///C:/dev/heyclicky-clone/docs/03-voice-ears-mouth-dictation.md) — Gemini/Deepgram STT ("Ears"), OpenAI Realtime/Gemini TTS ("Mouth"), and AI Dictation pipeline.
   - [`docs/04-drawing-2d-3d-and-ink-engine.md`](file:///C:/dev/heyclicky-clone/docs/04-drawing-2d-3d-and-ink-engine.md) — ClearScript V8 2D/3D procedural screen drawing, D3D11/D2D1 shaders, and Screen Ink Notebook.
   - [`docs/05-platform-win32-and-ui-shell.md`](file:///C:/dev/heyclicky-clone/docs/05-platform-win32-and-ui-shell.md) — Win32 click-through overlays, WASAPI audio, DXGI/WGC screen capture, UIA, and WinUI 3 Notch/ArcDock shell.
   - [`docs/06-agents-proactive-and-memory.md`](file:///C:/dev/heyclicky-clone/docs/06-agents-proactive-and-memory.md) — Local Codex/H1 agent harness, background jobs, Composio MCP OAuth, Proactive Agents, and OneBrain memory.
   - [`docs/07-kursor-roadmap.md`](file:///C:/dev/heyclicky-clone/docs/07-kursor-roadmap.md) — Master engineering roadmap for building **Kursor**.
3. All 7 decompiled `.NET 9.0` assemblies (**2,098 C# files**) are stored in [`reference/decompiled/`](file:///C:/dev/heyclicky-clone/reference/decompiled/).
4. Use subagents for heavy research/coding tasks so the main orchestrator context stays lean, and update [`docs/INDEX.md`](file:///C:/dev/heyclicky-clone/docs/INDEX.md) whenever new architectural docs or modules are added.
