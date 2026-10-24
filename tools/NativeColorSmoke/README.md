# Native color adapter integration

Run `tools/NativeColorSmoke/Run.ps1` from the repository. Requires installed VS C++ build tools and Windows SDK. No installation, focus, keyboard, mouse, existing VS window, desktop capture, or external-process patching is performed.

The test compiles the actual `LiteTreeColors.cs` against small index/palette stubs and a native fixture DLL. The fixture creates hidden test-owned windows and substitutes recording functions in its own GDI imports. The product adapter must scope changes to the observed window/UI thread, change text ink for TextOutW/ExtTextOutW/glyph output, restore HDC ink after rendering, honor both preference gates, and restore behavior after Dispose. It also checks unknown/empty text and re-enabling.

This is adapter integration evidence, **not installed VA/VS equivalence**. It cannot prove that the real VS rendering module imports the same GDI functions, uses window-backed HDCs, or that index symbol resolution matches VA. Those need actual paired runtime verification. Test output explicitly retains this limitation.
