using Xunit;

namespace Okafor.NET.Tests;

/// <summary>
/// Tests for the real wwwroot/js/pwa-register.js -- service worker registration, the install
/// prompt lifecycle, and clearing locally-cached PWA data on logout. Every assertion reads the
/// actual shipped file. Previously every test here built its own hand-typed copy of a script
/// snippet and asserted it against itself, which passed even where the copy no longer matched
/// the real file (e.g. the fake logout-cleanup snippet asserted a `console.error` call that the
/// real catch block has never had).
/// </summary>
public class PWARegistrationTests
{
    private static readonly string RegisterScript = ReadRepoFile("wwwroot/js/pwa-register.js");

    [Fact]
    public void Script_AvoidsOptionalChainingAndNullishCoalescing_ForOlderMobileBrowsers()
    {
        Assert.DoesNotContain("?.", RegisterScript);
        Assert.DoesNotContain("??", RegisterScript);
    }

    [Fact]
    public void ServiceWorkerRegistration_IsDeferredToLoad_AndFailureIsHandled()
    {
        Assert.Matches("window\\.addEventListener\\(\"load\"[\\s\\S]{0,150}navigator\\.serviceWorker\\.register\\(\"/service-worker\\.js\"\\)\\.catch\\(function", RegisterScript);
    }

    [Fact]
    public void InstallButton_ClickHandler_GuardsAgainstNulledPrompt()
    {
        // If appinstalled already fired and cleared installPrompt, a stale click must re-enable
        // the button instead of throwing on a null .prompt() call.
        Assert.Matches("if \\(!installPrompt\\)\\s*\\{[\\s\\S]{0,150}button\\.disabled = false;", RegisterScript);
        Assert.Contains("installPrompt.prompt();", RegisterScript);
        Assert.Contains("installPrompt.userChoice.finally(function", RegisterScript);
    }

    [Fact]
    public void InstallButton_IsAccessibleAndDynamicallyCreated()
    {
        Assert.Contains("document.createElement(\"button\")", RegisterScript);
        Assert.Contains("button.type = \"button\";", RegisterScript);
        Assert.Contains("button.dataset.pwaInstall = \"true\";", RegisterScript);
        Assert.Contains("button.setAttribute(\"aria-label\", \"Install Okafor Hospital app\");", RegisterScript);
        Assert.Contains("button.className = \"pwa-install-button\";", RegisterScript);
    }

    [Fact]
    public void AppInstalled_ClearsPromptReference_AndRemovesInstallButton()
    {
        Assert.Matches("addEventListener\\(\"appinstalled\"[\\s\\S]{0,120}installPrompt = null;", RegisterScript);
        Assert.Matches("addEventListener\\(\"appinstalled\"[\\s\\S]{0,200}button\\.remove\\(\\);", RegisterScript);
    }

    [Fact]
    public void LogoutSubmit_TriggersLocalAppDataCleanup()
    {
        // Cleanup must run on the logout form submit, matched by its real action URL, not just a
        // generic submit listener that could fire on every form on the page.
        Assert.Matches("addEventListener\\(\"submit\"[\\s\\S]{0,250}/Account/Logout[\\s\\S]{0,100}clearLocalAppData\\(\\);", RegisterScript);
    }

    [Fact]
    public void ClearLocalAppData_GuardsEveryBrowserStorageApiBeforeUse()
    {
        Assert.Contains("window.okaforEncryptedOfflineStore && typeof window.okaforEncryptedOfflineStore.clearAll === \"function\"", RegisterScript);
        Assert.Contains("window.okaforPwaAppointments && typeof window.okaforPwaAppointments.clear === \"function\"", RegisterScript);
        Assert.Contains("if (window.sessionStorage)", RegisterScript);
        Assert.Contains("if (window.localStorage)", RegisterScript);
        Assert.Contains("if (window.indexedDB)", RegisterScript);
    }

    [Fact]
    public void ClearLocalAppData_StorageCleanupIsWrappedInTryCatch_SoLogoutAlwaysCompletes()
    {
        Assert.Matches("function clearLocalAppData\\(\\)\\s*\\{[\\s\\S]{0,20}try\\s*\\{", RegisterScript);
        Assert.Contains("} catch (err) {", RegisterScript);
    }

    [Fact]
    public void ClearAppCaches_OnlyDeletesOkaforPrefixedCaches_AndIsGuardedAndBestEffort()
    {
        Assert.Contains("if (!window.caches || typeof window.caches.keys !== \"function\")", RegisterScript);
        Assert.Contains("cacheName.indexOf(\"okafor-pwa-\") === 0;", RegisterScript);
        // Cache cleanup failure must not throw and block logout.
        Assert.Matches("clearAppCaches[\\s\\S]*\\.catch\\(function \\(\\) \\{", RegisterScript);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }
}
