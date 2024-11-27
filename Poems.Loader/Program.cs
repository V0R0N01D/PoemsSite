using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Poems.Common.Database;
using Poems.Loader.Services.Interfaces;
using Poems.Loader.Models;
using Poems.Loader.Services;
using Poems.Loader.HostedServices;

namespace Poems.Loader;

internal class Program
{
	static async Task Main(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);

		builder.Configuration.AddJsonFile("connection_strings.json");

		builder.Services.AddDbContext<PoemsContext>(options =>
			options.UseNpgsql(builder.Configuration.GetConnectionString("PoemsDatabase")));

		builder.Services.AddScoped<IFileInfoProvider, ConfigurationFileInfoProvider>((provider) =>
		{
			return new ConfigurationFileInfoProvider(provider.GetRequiredService<IConfiguration>());
		});
		builder.Services.AddScoped<IFileReader<PoemRecord>, CsvLazyFileReader>();
		builder.Services.AddScoped<IDataImporter<PoemRecord>, PoemsDataImporter>();

		builder.Services.AddHostedService<PoemsLoader>();

		var host = builder.Build();

		await host.RunAsync();
	}
}
