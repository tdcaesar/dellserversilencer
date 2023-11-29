using DellServerSilencer;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        (hostContext, services) =>
    {
        services.AddHostedService<Worker>();
        services.Configure<Settings>(
            hostContext.Configuration.GetSection("Settings"));
    })
    .Build();

host.Run();