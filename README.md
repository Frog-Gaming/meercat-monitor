# Meercat Monitor

A simple online status monitor with email notifications and status pages.

The project is small enough to review and understand.
The runtime dependencies are low, requiring only a dotnet runtime, or a self-contained build.

## Features

The `appsettings.json` configuration file allows you to configure groups, targets, email recipients, status webpage URL slug, etc.

Check results are stored to disk as jsonl files (one JSON per line). Currently without any limit.

## Publish

To publish linux binaries, which will be placed under `artifacts/publish/MeercatMonitor/release_linux-x64/`:

```
dotnet publish --os linux
```

To publish windows binaries: 

```
dotnet publish --os windows
```

As per settings in `Directory.Build.props`, the default publish creates a single executable file, which is not self-contained (requires a [dotnet runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0/runtime)).

To publish a self-contained executable, which will be a bigger file but not require a dotnet runtime to be installed, pass `--self-contained true`.
