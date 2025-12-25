using System.Collections.ObjectModel;

using Newtonsoft.Json;

using Serilog;

namespace XIVLauncher.Core.Accounts;

// TODO: Store XivAccounts by username instead of using Id.
public class AccountManager
{
    public ObservableCollection<XivAccount> Accounts = new();

    public XivAccount? CurrentAccount
    {
        get { return Accounts.Count > 1 ? Accounts.FirstOrDefault(a => a.Id == Program.Config.CurrentAccountId) : Accounts.FirstOrDefault(); }
        set => Program.Config.CurrentAccountId = value?.Id;
    }

    public AccountManager(FileInfo configFile)
    {
        this.configFile = configFile;
        Load();

        Accounts.CollectionChanged += Accounts_CollectionChanged;
    }

    private void Accounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Save();
    }

    public void UpdatePassword(XivAccount account, string password)
    {
        Log.Information("UpdatePassword() called");
        var existingAccount = Accounts.FirstOrDefault(a => a.Id == account.Id);
        if (existingAccount is not null) existingAccount.Password = password;
    }

    public void UpdateFreeTrial(XivAccount account, bool freeTrial)
    {
        Log.Information("UpdateFreeTrial() called");
        var existingAccount = Accounts.FirstOrDefault(a => a.Id == account.Id);
        if (existingAccount is not null) existingAccount.IsFreeTrial = freeTrial;
        Save();
    }

    public void UpdateLastSuccessfulOtp(XivAccount account, string lastOtp)
    {
        var existingAccount = Accounts.FirstOrDefault(a => a.Id == account.Id);
        if (existingAccount is not null) existingAccount.LastSuccessfulOtp = lastOtp;
        Save();
    }

    public void AddAccount(XivAccount account)
    {
        var existingAccount = Accounts.FirstOrDefault(a => a.Id == account.Id);

        Log.Verbose($"existingAccount: {existingAccount?.Id}");

        if (existingAccount != null && existingAccount.Password != account.Password)
        {
            Log.Verbose("Updating password...");
            existingAccount.Password = account.Password;
            return;
        }

        if (existingAccount != null)
            return;

        Accounts.Add(account);
    }

    public void RemoveAccount(XivAccount account)
    {
        Accounts.Remove(account);
    }

    #region SaveLoad

    private readonly FileInfo configFile;

    public void Save()
    {
        File.WriteAllText(this.configFile.FullName, JsonConvert.SerializeObject(Accounts, Formatting.Indented));
    }

    public void Load()
    {
        if (!this.configFile.Exists)
        {
            Save();
            return;
        }
        
        var accounts = JsonConvert.DeserializeObject<ObservableCollection<XivAccount>>(File.ReadAllText(this.configFile.FullName));
        accounts ??= new ObservableCollection<XivAccount>();
        Accounts = accounts;
    }

    #endregion
}
