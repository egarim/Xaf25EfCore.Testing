using DevExpress.CodeParser;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xaf25EfCore.Testing.Blazor.Server;
using Xaf25EfCore.Testing.Module;
using Xaf25EfCore.Testing.Module.BusinessObjects;

namespace XUnitTest
{
    public class TestUsingStartup
    {

        private IServiceProvider? serviceProvider;
        private TestingBlazorApplication? xafApplication;
        [Fact]
        public void Test1()
        {
            try
            {
                Console.WriteLine("Testing that ValidationModule works correctly with Startup configuration...");

                var configData = new Dictionary<string, string?>
                {
                    ["ConnectionString"] = "Data Source=ValidationTest;Mode=Memory;Cache=Shared"
                };
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(configData)
                    .Build();

                var services = new ServiceCollection();
                var startup = new Startup(configuration);
                startup.ConfigureServices(services);

                var objectSpaceProvider = new DevExpress.ExpressApp.EFCore.EFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
                    (builder, _) => builder
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .UseLazyLoadingProxies()
                        .UseChangeTrackingProxies()
                );

                services.AddSingleton<TestingBlazorApplication>(serviceProvider =>
                {
                    var app = new TestingBlazorApplication();
                    app.ServiceProvider = serviceProvider;

                    var testingModule = new TestingModule();
                    app.Modules.Add(testingModule);

                    app.Setup("TestApplication", objectSpaceProvider);

                    return app;
                });

                serviceProvider = services.BuildServiceProvider();
                xafApplication = serviceProvider.GetRequiredService<TestingBlazorApplication>();

                Console.WriteLine("Application modules count: " + xafApplication.Modules.Count);

                foreach (var module in xafApplication.Modules)
                {
                    Console.WriteLine("Module found: " + module.GetType().Name);
                }

                var validationModule = xafApplication.Modules.FirstOrDefault(m => m.GetType().Name.Contains("Validation"));
                Assert.NotNull(validationModule); // xUnit assertion

                Console.WriteLine("ValidationModule found: " + validationModule?.GetType().Name);

                var validator = serviceProvider.GetService<DevExpress.Persistent.Validation.IValidator>();
                Assert.NotNull(validator); // xUnit assertion

                Console.WriteLine("IValidator service is properly registered");

                var testObjectSpaceProvider = xafApplication.ObjectSpaceProviders.First();
                var objectSpace = testObjectSpaceProvider.CreateObjectSpace();

                var customer = objectSpace.CreateObject<Customer>();
                customer.Name = "Validation Test Customer";

                objectSpace.CommitChanges();

                Console.WriteLine("Business object creation and persistence works without validation service errors");
                Console.WriteLine("ValidationModule integration test passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ValidationModule test failed: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
                throw;
            }
        }
    }
}
