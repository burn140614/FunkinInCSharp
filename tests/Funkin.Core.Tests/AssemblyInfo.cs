using Xunit;

// The port keeps a handful of statics for parity with the original (FunkinPaths, Mods, the note kind
// manager, SaveManager). Tests that exercise them must not run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
