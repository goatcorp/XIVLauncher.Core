using KeySharp;

using Serilog;

namespace XIVLauncher.Core.Accounts.Secrets.Providers;

public class KeychainSecretProvider : ISecretProvider
{
    public const string PACKAGE = "dev.goats.xivlauncher";
    public const string SERVICE = "SEID";

    public bool IsAvailable { get; private set; }

    public KeychainSecretProvider()
    {
        this.IsAvailable = SetDummyAndCheck();
    }

    public bool SetDummyAndCheck()
    {
        /*
         * We need to set a dummy entry here to ensure that libsecret unlocks the keyring.
         * This is a problem with libsecret: http://crbug.com/660005
         */
        try
        {
            const string DUMMY_SVC = "XIVLauncher Safe Storage Control";
            const string DUMMY_NAME = "XIVLauncher";
            const string DUMMY_PW = "Honi soit qui mal y pense";

            Log.Verbose("Testing setting dummy passwords to keyring");
            Keyring.SetPassword(PACKAGE, DUMMY_SVC, DUMMY_NAME, DUMMY_PW);

            Log.Verbose("Testing getting dummy password from keyring");
            var saved = Keyring.GetPassword(PACKAGE, DUMMY_SVC, DUMMY_NAME);
            return saved == DUMMY_PW;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not init the keychain");
        }

        return false;
    }

    public string? GetPassword(string accountName)
    {
        try
        {
            Log.Information("Getting password from keychain");
            return Keyring.GetPassword(PACKAGE, SERVICE, accountName);
        }
        catch (KeyringException ex)
        {
            if (ex.Type == ErrorType.NotFound)
                return null;

            throw;
        }
    }

    public void SavePassword(string accountName, string password)
    {
        Log.Information("Setting password to keychain");
        Keyring.SetPassword(PACKAGE, SERVICE, accountName, password);
    }

    public void DeletePassword(string accountName)
    {
        Log.Information("Deleting password from keychain");
        Keyring.DeletePassword(PACKAGE, SERVICE, accountName);
    }
}
