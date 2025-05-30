# RecordTimeSaved
A simple cli tool that can be used to track time saved by automations and get a result out of it

## Building

This project uses the .NET 8 SDK. Make sure you have it installed. You can check your version with:

```bash
dotnet --version
```

To build the project, run:

```bash
dotnet build
```

## Running

You can run the CLI directly with:

```bash
dotnet run --project RecordTimeSaved/RecordTimeSaved.csproj -- <mode> [args]
```

Or, after building, you can run the compiled binary:

```bash
./RecordTimeSaved/RecordTimeSaved/bin/Debug/net8.0/RecordTimeSaved <mode> [args]
```

### Usage

- To record time saved by a tool:
  ```bash
  RecordTimeSaved record <tool> <timeInSeconds>
  ```
  Example:
  ```bash
  RecordTimeSaved record "MyAutomation" 120
  ```
- To read the total time saved:
  ```bash
  RecordTimeSaved read
  ```

The database is stored in your local application data folder by default.

