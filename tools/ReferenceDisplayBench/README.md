# Reference display benchmark

Run from the repository root:

    dotnet run --project tools/ReferenceDisplayBench/ReferenceDisplayBench.csproj --configuration Release -- .local/refactor-benchmark116.json

The Before* source files preserve the 0.0.115 context/filter algorithms with only a namespace change and a Core using directive. The baseline reproduces the old per-filter caption/context preparation. Both paths receive the same generated source, 1,200 reference locations, and eight queries. Every result caption and its order must match before timing starts.

Seven samples report raw elapsed values, median, and allocated bytes. Snapshot cold and warm preparation are separate from cached filtering. This is a pure managed-code benchmark; it excludes compiler requests, COM document access, WPF controls/layout, IDE startup, and whole-application memory. Do not report its ratio as whole-application acceleration. Before* helpers are compiled only into this benchmark.
