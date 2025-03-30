# Meercat Monitor

A simple online status monitor with email notifications and status pages.

The project is small enough to review and understand.
The runtime dependencies are low, requiring only a dotnet runtime, or a self-contained build.

## Features

The `appsettings.json` configuration file allows you to configure groups, targets, email recipients, status webpage URL slug, etc.

Check results are stored to disk as jsonl files (one JSON per line). Currently without any limit.
