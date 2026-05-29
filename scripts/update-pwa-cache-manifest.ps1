param(
    [string]$WwwRoot = (Join-Path $PSScriptRoot "..\wwwroot")
)

$resolvedRoot = (Resolve-Path -LiteralPath $WwwRoot).Path

$assetPatterns = @(
    "app-shell.html",
    "offline.html",
    "offline-appointments.html",
    "site.webmanifest",
    "favicon.ico",
    "css/app-shell.css",
    "css/tailwind.css",
    "css/site.css",
    "css/public-site.css",
    "js/site.js",
    "js/offline-state.js",
    "js/encrypted-offline-store.js",
    "js/pwa-register.js",
    "js/pwa-appointments.js",
    "js/portal-security.js",
    "js/push-notifications.js",
    "lib/bootstrap/dist/css/bootstrap.min.css",
    "lib/bootstrap/dist/js/bootstrap.bundle.min.js",
    "images/icons/*.svg",
    "images/icons/*.png"
)

$assets = foreach ($pattern in $assetPatterns) {
    Get-ChildItem -Path (Join-Path $resolvedRoot $pattern) -File -ErrorAction SilentlyContinue
}

$paths = $assets |
    Sort-Object FullName -Unique |
    ForEach-Object {
        "/" + ($_.FullName.Substring($resolvedRoot.Length).TrimStart("\") -replace "\\", "/")
    }

"const STATIC_ASSETS = ["
$paths | ForEach-Object {
    "    `"{0}`"," -f $_
}
"];"
