using BookStore.Application.Abstractions;
using BookStore.Infrastructure.OpenLibrary;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<BookStoreDbContext>(o =>
            o.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=bookstore.db"));
        services.AddScoped<IBookRepository, BookRepository>();

        services.Configure<OpenLibraryOptions>(config.GetSection(OpenLibraryOptions.SectionName));
        services.AddHttpClient<IBookCatalog, OpenLibraryClient>((sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<OpenLibraryOptions>>().Value;
            http.BaseAddress = new Uri(o.SearchBaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
            http.DefaultRequestHeaders.UserAgent.ParseAdd(o.UserAgent);
        });

        return services;
    }
}
