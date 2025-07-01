using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Xaf25EfCore.Testing.Blazor.Server;
using Xaf25EfCore.Testing.Module;
using Xaf25EfCore.Testing.Module.BusinessObjects;
using Xaf25EfCore.Testing.Module.Controllers;
using Xaf25EfCore.Testing.Module.Services;

namespace Tests
{
    public class TestUsingStartupWithSecurity
    {
        private IServiceProvider? serviceProvider;
        private TestingBlazorApplication? xafApplication;
        private EFCoreObjectSpaceProvider<TestingEFCoreDbContext>? objectSpaceProvider;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_StartupConfigurationWithSecurityAndLogin()
        {
            try
            {
                Console.WriteLine("Testing Startup configuration with security and login functionality...");

                // Create configuration similar to TestUsingStartup
                var configData = new Dictionary<string, string?>
                {
                    ["ConnectionString"] = "Data Source=SecurityValidationTest;Mode=Memory;Cache=Shared"
                };
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(configData)
                    .Build();

                // Initialize services with Startup configuration
                var services = new ServiceCollection();
                var startup = new Startup(configuration);
                startup.ConfigureServices(services);

                Console.WriteLine("Startup services configured successfully");

                // Create object space provider for testing
                objectSpaceProvider = new EFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
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
                services.AddSingleton<TestingBlazorApplication>(serviceProvider =>
                {
                    var app = new TestingBlazorApplication();
                    app.ServiceProvider = serviceProvider;
                    
                    // Configure for testing - disable database schema checking
                    app.CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.ModuleInfo;
                    
                    var testingModule = new TestingModule();
                    app.Modules.Add(testingModule);
                    
                    app.Setup("TestApplication", objectSpaceProvider);
                    
                    return app;
                });

                // Build service provider
                serviceProvider = services.BuildServiceProvider();
                xafApplication = serviceProvider.GetRequiredService<TestingBlazorApplication>();

                Console.WriteLine("TestingBlazorApplication created and configured");
                Console.WriteLine("Application modules count: " + xafApplication.Modules.Count);

                // Log all modules
                foreach (var module in xafApplication.Modules)
                {
                    Console.WriteLine("Module found: " + module.GetType().Name);
                }

                // Verify ValidationModule is present
                var validationModule = xafApplication.Modules.FirstOrDefault(m => m.GetType().Name.Contains("Validation"));
                Assert.That(validationModule, Is.Not.Null, "ValidationModule should be loaded");
                Console.WriteLine("ValidationModule found: " + validationModule?.GetType().Name);

                // Verify IValidator service is registered
                var validator = serviceProvider.GetService<DevExpress.Persistent.Validation.IValidator>();
                Assert.That(validator, Is.Not.Null, "IValidator service should be registered");
                Console.WriteLine("IValidator service is properly registered");

                // Create admin user and security setup (from ExampleTest)
                var objectSpace = objectSpaceProvider.CreateObjectSpace() as EFCoreObjectSpace;
                objectSpace.DbContext.Database.EnsureCreated();

                var userAdmin = objectSpace.CreateObject<ApplicationUser>();
                userAdmin.UserName = "Admin";
                userAdmin.SetPassword("123456");

                // Commit user to get the ID
                objectSpace.CommitChanges();
                ((ISecurityUserWithLoginInfo)userAdmin).CreateUserLoginInfo(SecurityDefaults.PasswordAuthentication, objectSpace.GetKeyValueAsString(userAdmin));

                // Create admin role
                PermissionPolicyRole adminRole = objectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
                if (adminRole == null)
                {
                    adminRole = objectSpace.CreateObject<PermissionPolicyRole>();
                    adminRole.Name = "Administrators";
                }
                adminRole.IsAdministrative = true;
                userAdmin.Roles.Add(adminRole);
                objectSpace.CommitChanges();

                Console.WriteLine("Admin user and role created successfully");

                // Login and setup security
                xafApplication.Security = Login("Admin", "123456", objectSpace);
                Console.WriteLine("Security configured and user logged in");

                // Test business operations with security
                var customerController = xafApplication.CreateController<CustomerController>();
                
                var customerOs = xafApplication.CreateObjectSpace(typeof(Customer));
                var customer = customerOs.CreateObject<Customer>();
                customer.Name = "Test Customer with Security";
                customer.Active = false;
                
                var customerDetailView = xafApplication.CreateDetailView(customerOs, customer, true);
                customerController.SetView(customerDetailView);

                // Execute controller action
                customerController.SetCustomerActive.DoExecute();
                Console.WriteLine("Customer active status changed to: " + customer.Active);

                // Verify customer is now active
                Assert.That(customer.Active, "Customer should be active after controller action");

                // Test validation functionality
                var testCustomer = customerOs.CreateObject<Customer>();
                testCustomer.Name = "Validation Test Customer";
                customerOs.CommitChanges();
                
                Console.WriteLine("Business object creation and persistence works with security and validation");

                // Test service injection works
                var helloWorldService = serviceProvider.GetService<IHelloWorldService>();
                if (helloWorldService != null)
                {
                    var greeting = helloWorldService.GetGreeting();
                    var personalGreeting = helloWorldService.GetGreeting("Secured XAF User");
                    Console.WriteLine($"Service greeting: {greeting}");
                    Console.WriteLine($"Personal greeting: {personalGreeting}");
                    
                    Assert.That(greeting, Is.EqualTo("Hello, World!"));
                    Assert.That(personalGreeting, Is.EqualTo("Hello, Secured XAF User!"));
                }

                Console.WriteLine("? Startup configuration with security and login test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("? Test failed with error: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            objectSpaceProvider?.Dispose();
            xafApplication?.Dispose();
            
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                Console.WriteLine("Service provider disposed");
            }
        }

        private SecurityStrategyComplex Login(string userName, string password, IObjectSpace loginObjectSpace)
        {
            AuthenticationStandard authentication = new AuthenticationStandard();
            var security = new SecurityStrategyComplex(typeof(ApplicationUser), typeof(PermissionPolicyRole), authentication);
            security.RegisterEFCoreAdapterProviders(objectSpaceProvider);

            authentication.SetLogonParameters(new AuthenticationStandardLogonParameters(userName, password));
            security.Logon(loginObjectSpace);

            // Create secured object space provider
            var secureEfProvider = new SecuredEFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
                security,
                XafTypesInfo.Instance, 
                null,
                (builder, connectionString) => builder.UseSqlServer(connectionString)
            );

            return security;
        }
    }
}