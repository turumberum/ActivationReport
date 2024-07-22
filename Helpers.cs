using ActivationReport;
using ActivationReport.Models;
using CommunityToolkit.Maui.Storage;
using Microsoft.EntityFrameworkCore;

internal static class Helpers
{
    public static List<Company> LoadCompaniesData(bool report = false)
    {
        using (var db = new AppDBContext())
        {
            if (report)
            {
                return db.Companies.Where(c => c.MonthlyReport == true).Include(company => company.Cards).ToList();
            }
            else
            {
                return db.Companies.Include(company => company.Cards).ToList();
            }
        }
    }
    public static void OpenFolder(string path) => System.Diagnostics.Process.Start("explorer.exe", path);

    public static async Task<string?> PickFolder(string? optionalSaveParameter = null)
    {
        CancellationTokenSource source = new();
        CancellationToken token = source.Token;
        var result = await FolderPicker.Default.PickAsync(token);

        if (result.IsSuccessful)
        {
            if (optionalSaveParameter != null)
            {
                Preferences.Default.Set(optionalSaveParameter, result.Folder.Path);
            }
            return result.Folder.Path;
        }
        else
        {
            return null;
        }
    }
}