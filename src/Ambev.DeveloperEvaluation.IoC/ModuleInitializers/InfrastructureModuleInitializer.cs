using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.NoSql;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();

        RegisterMongoDB(builder);
    }

    private static void RegisterMongoDB(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration["MongoDB:ConnectionString"]
            ?? "mongodb://localhost:27017";
        var databaseName = builder.Configuration["MongoDB:DatabaseName"]
            ?? "developer_evaluation";

        builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        builder.Services.AddScoped<IMongoDatabase>(provider =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        builder.Services.AddScoped<ISaleEventRepository, SaleEventRepository>();
    }
}
