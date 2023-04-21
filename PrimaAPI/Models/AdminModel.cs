using System.ComponentModel.DataAnnotations;

namespace ProgettoLogin.Models;

public class AdminModel
{
    public List<(string Url, int SavePerc)> topSavedSites { get; set; } = new();
    public int numberTopSavedSites { get; set; } = 5;
    public int allSitesCount { get; set; }
    public List<(DateTime DateRecording, int NumberOfAccount)> chartData { get; set; } = new();
    public int numberOfDaysToShow { get; set; }
}
