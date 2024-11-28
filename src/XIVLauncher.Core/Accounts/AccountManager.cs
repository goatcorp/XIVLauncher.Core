using System.Collections.ObjectModel;

using Newtonsoft.Json;

using Serilog;

namespace XIVLauncher.Core.Accounts;

public class AccountManager
{
    public ObservableCollection<XivAccount> Accounts = [];

    public XivAccount? CurrentAccount
    {
        get { return this.Accounts.Count > 1 ? this.Accounts.FirstOrDefault(a => a.Id == Program.Config.CurrentAccountId) : this.Accounts.FirstOrDefault(); }
        set => Program.Config.CurrentAccountId = value?.Id;
    }

    public AccountManager(FileInfo configFile)
    {
        this.configFile = configFile;
        this.Load();

        this.Accounts.CollectionChanged += this.Accounts_CollectionChanged;
    }

    private void Accounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        this.Save();
    }

    public void UpdatePassword(XivAccount account, string password)
    {
        Log.Information("UpdatePassword() called");
        var existingAccount = this.Accounts.FirstOrDefault(a => a.Id == account.Id);
        if (existingAccount is not null) existingAccount.Password = password;
    }

    public void UpdateLastSuccessfulOtp(XivAccount account, string lastOtp)
    {
        var existingAccount = this.Accounts.FirstOrDefault(a => a.Id == account.Id);
        if (existingAccount is not null) existingAccount.LastSuccessfulOtp = lastOtp;
        this.Save();
    }

    public void AddAccount(XivAccount account)
    {
        var existingAccount = this.Accounts.FirstOrDefault(a => a.Id == account.Id);

        Log.Verbose($"existingAccount: {existingAccount?.Id}");

        if (existingAccount != null && existingAccount.Password != account.Password)
        {
            Log.Verbose("Updating password...");
            existingAccount.Password = account.Password;
            return;
        }

        if (existingAccount != null)
            return;

        this.Accounts.Add(account);
    }

    public void RemoveAccount(XivAccount account)
    {
        this.Accounts.Remove(account);
    }

    #region SaveLoad

    private readonly FileInfo configFile;

    public void Save()
    {
        File.WriteAllText(this.configFile.FullName, JsonConvert.SerializeObject(this.Accounts, Formatting.Indented));
    }

    public void Load()
    {
        if (!this.configFile.Exists)
        {
            this.Save();
            return;
        }

        this.Accounts = JsonConvert.DeserializeObject<ObservableCollection<XivAccount>>(File.ReadAllText(this.configFile.FullName));

        // If the file is corrupted, this will be null anyway
        this.Accounts ??= [];
    }

    #endregion
}
