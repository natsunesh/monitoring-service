using Monitor_zakupki;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<UserSettings>(
    builder.Configuration.GetSection("UserSettings"));

builder.Services.Configure<MainSettings>(
    builder.Configuration.GetSection("MainSettings"));


var host = builder.Build();
host.Run();




