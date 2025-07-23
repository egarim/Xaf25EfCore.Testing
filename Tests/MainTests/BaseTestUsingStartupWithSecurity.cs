using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Tests.Infrastructure;
using Xaf25EfCore.Testing.Blazor.Server;
using Xaf25EfCore.Testing.Module;
using Xaf25EfCore.Testing.Module.BusinessObjects;
using Xaf25EfCore.Testing.Module.Controllers;
using Xaf25EfCore.Testing.Module.Services;


namespace Tests.MainTests
{
    public abstract class BaseTestUsingStartupWithSecurity
    {
        [SetUp]
        public virtual void Setup()
        {
        }

    


        [TearDown]
        public virtual void TearDown()
        {
            // TearDown is now empty since each test manages its own resources
        }
        protected virtual IConfigurationRoot DefaultConfigurationGetConfiguration(Dictionary<string, string?> Values=null)
        {
            // Create configuration similar to TestUsingStartup
            var configData = new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Data Source=SecurityValidationTest;Mode=Memory;Cache=Shared"
            };
            if (Values != null)
            {
                foreach (var kvp in Values)
                {
                
                    configData[kvp.Key] = kvp.Value;
                }
            }
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
            return configuration;
        }
        protected virtual XafEnvironment CreateTestInstance(Startup startup)
        {
           

            // Initialize services with Startup configuration
            var services = new ServiceCollection();
           
            startup.ConfigureServices(services);

            Console.WriteLine("Startup services configured successfully");

            // Create object space provider for testing
            var objectSpaceProvider = new EFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
                (builder, _) => builder
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .UseLazyLoadingProxies()
                    .UseChangeTrackingProxies()
            );

            // Ensure database is created and check compatibility
            var updateOs = objectSpaceProvider.CreateObjectSpace() as EFCoreObjectSpace;
            updateOs.DbContext.Database.EnsureCreated();

            // Check database schema compatibility like in ExampleTest
            Exception exception;
            var state = objectSpaceProvider.CheckDatabaseSchemaCompatibility(out exception);
            Console.WriteLine($"Database schema compatibility state: {state}");

            // Add TestingBlazorApplication to services with proper configuration for testing
            services.AddSingleton(serviceProvider =>
            {
                var app = new TestingBlazorApplication();
                app.ServiceProvider = serviceProvider;
                
                // Configure for testing - disable database schema checking
                app.CheckCompatibilityType = CheckCompatibilityType.ModuleInfo;
                
                var testingModule = new TestingModule();
                app.Modules.Add(testingModule);
                
                app.Setup("TestApplication", objectSpaceProvider);
                
                return app;
            });

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            var xafApplication = serviceProvider.GetRequiredService<TestingBlazorApplication>();

            return new XafEnvironment
            {
                ServiceProvider = serviceProvider,
                XafApplication = xafApplication,
                ObjectSpaceProvider = objectSpaceProvider
            };
        }
        protected virtual SecurityStrategyComplex Login(string userName, string password, IObjectSpace loginObjectSpace, IObjectSpaceProvider objectSpaceProvider)
        {
            AuthenticationStandard authentication = new AuthenticationStandard();
            var security = new SecurityStrategyComplex(typeof(ApplicationUser), typeof(PermissionPolicyRole), authentication);
            security.RegisterEFCoreAdapterProviders(objectSpaceProvider);

            authentication.SetLogonParameters(new AuthenticationStandardLogonParameters(userName, password));
            security.Logon(loginObjectSpace);

         

            return security;
        }
    }
}