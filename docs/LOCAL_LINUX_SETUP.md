# Local Linux Setup

This project is verified on Fedora Linux with the .NET SDK installed under `~/.dotnet`.

## Required Tools

- .NET 10 SDK
- Docker and Docker Compose, for SQL Server development
- Node.js and npm, for Tailwind CSS builds

Fedora Node/npm install:

```bash
sudo dnf install -y nodejs npm
```

If .NET is installed in the user profile, add it to your shell path:

```bash
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
```

To make that permanent, add the same line to your shell profile, such as `~/.bashrc`.

## Quick Backend Run

Use Testing mode when you only need the app running without SQL Server:

```bash
ASPNETCORE_ENVIRONMENT=Testing ASPNETCORE_URLS=http://localhost:5187 \
  $HOME/.dotnet/dotnet run --project Okafor-.NET.csproj --no-launch-profile
```

Open:

```text
http://localhost:5187
http://localhost:5187/health
```

## Hot Reload On Linux

Fedora may show this error when running `dotnet watch run`:

```text
The configured user limit (128) on the number of inotify instances has been reached
```

That is an OS file-watcher limit, not a project build failure. Use the repo script, which enables polling mode for the `dotnet watch` process itself:

```bash
./scripts/dev-watch.sh
```

You can also run the equivalent command manually:

```bash
DOTNET_USE_POLLING_FILE_WATCHER=1 dotnet watch run --project Okafor-.NET.csproj --no-launch-profile
```

For a permanent OS-level fix, raise Fedora's inotify limits:

```bash
printf "fs.inotify.max_user_instances=1024\nfs.inotify.max_user_watches=524288\n" | sudo tee /etc/sysctl.d/99-okafor-watch.conf
sudo sysctl --system
```

## SQL Server With Docker

Copy the sample environment file and set a strong password:

```bash
cp .env.example .env
docker compose up -d
```

Then set the development connection string to:

```text
Server=localhost,1433;Database=OkaforHospitalDb;User Id=sa;Password=<your-password>;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Apply migrations:

```bash
$HOME/.dotnet/dotnet ef database update
```

## Local Path Warning

The current workspace path contains an ampersand: `B&P Okafor Memorial`.
On this machine, `dotnet build Okafor-.NET.sln` fails from that path because MSBuild escapes the ampersand incorrectly inside the generated solution metaproject.

Workarounds:

```bash
$HOME/.dotnet/dotnet build Okafor-.NET.csproj
$HOME/.dotnet/dotnet build tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj
```

For a cleaner long-term local setup, clone the repo into a path without `&`, for example:

```text
~/src/okafor-memorial
```

## Frontend CSS

After installing Node/npm:

```bash
npm install
npm run build:css
```

During design work:

```bash
npm run watch:css
```
