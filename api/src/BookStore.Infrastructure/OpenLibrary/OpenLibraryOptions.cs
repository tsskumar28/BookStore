namespace BookStore.Infrastructure.OpenLibrary;

public sealed class OpenLibraryOptions
{
    public const string SectionName = "OpenLibrary";

    public string SearchBaseUrl { get; set; } = "https://openlibrary.org/";
    public string CoversBaseUrl { get; set; } = "https://covers.openlibrary.org/";
    public string UserAgent { get; set; } = "BookStoreAssessment/1.0 (contact: dev@example.com)";
    public int TimeoutSeconds { get; set; } = 8;
    public int ResultLimit { get; set; } = 10;
}
