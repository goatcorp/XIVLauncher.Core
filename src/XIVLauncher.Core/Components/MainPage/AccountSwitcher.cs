using System.Numerics;

using Hexa.NET.ImGui;

using XIVLauncher.Core.Accounts;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.MainPage;

public class AccountSwitcher : Component
{
    private const string ACCOUNT_SWITCHER_POPUP_ID = "accountSwitcher";

    private readonly AccountManager manager;

    private bool doOpen = false;

    public event EventHandler<XivAccount>? AccountChanged;

    public AccountSwitcher(AccountManager manager)
    {
        this.manager = manager;
    }

    public void Open()
    {
        this.doOpen = true;
    }

    public override void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5));

        if (ImGui.BeginPopupContextItem(ACCOUNT_SWITCHER_POPUP_ID))
        {
            if (this.manager.Accounts.Count == 0)
            {
                ImGui.Text(Strings.NoSavedAccounts);
            }

            foreach (XivAccount account in this.manager.Accounts)
            {
                var name = account.UserName;

                if (account.UseSteamServiceAccount)
                    name += " (Steam)";

                if (account.UseOtp)
                    name += " (OTP)";

                if (account.IsFreeTrial)
                    name += " (Trial)";

                var textLength = ImGui.CalcTextSize(name).X;

                if (ImGui.Button(name + $"###{account.Id}", new Vector2(textLength + 15, 40)))
                {
                    this.AccountChanged?.Invoke(this, account);
                }
            }

            ImGui.EndPopup();
        }

        ImGui.PopStyleVar();

        if (this.doOpen)
        {
            this.doOpen = false;
            ImGui.OpenPopup(ACCOUNT_SWITCHER_POPUP_ID);
        }

        base.Draw();
    }
}
