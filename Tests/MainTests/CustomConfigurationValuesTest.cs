﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Infrastructure;
using Xaf25EfCore.Testing.Blazor.Server;
using Xaf25EfCore.Testing.Module.BusinessObjects;
using Xaf25EfCore.Testing.Module.Controllers;
using Xaf25EfCore.Testing.Module.Services;

namespace Tests.MainTests
{
  
    public class CustomConfigurationValuesTest : BaseTestUsingStartupWithSecurity
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
        }
        [Test]
        public void DemoTest()
        {
            XafEnvironment components = null;
            try
            {
                Console.WriteLine("Testing Startup configuration with security and login functionality...");

                Dictionary<string, string?> customValues = new Dictionary<string, string?>
                {
                    ["ConnectionString"] = "Data Source=CustomConfigTest;Mode=Memory;Cache=Shared",
                    ["CustomSetting1"] = "Value1",
                    ["CustomSetting2"] = "Value2"
                };  

                var configuration = DefaultConfigurationGetConfiguration(customValues);
                var startup = new Startup(configuration);
                // Create individual test instance
                components = CreateTestInstance(startup);

                Console.WriteLine("TestingBlazorApplication created and configured");
                Console.WriteLine("Application modules count: " + components.XafApplication.Modules.Count);

                // Log all modules
                foreach (var module in components.XafApplication.Modules)
                {
                    Console.WriteLine("Module found: " + module.GetType().Name);
                }

                // Verify ValidationModule is present
                var validationModule = components.XafApplication.Modules.FirstOrDefault(m => m.GetType().Name.Contains("Validation"));
                Assert.That(validationModule, Is.Not.Null, "ValidationModule should be loaded");
                Console.WriteLine("ValidationModule found: " + validationModule?.GetType().Name);

                // Verify IValidator service is registered
                var validator = components.ServiceProvider.GetService<DevExpress.Persistent.Validation.IValidator>();
                Assert.That(validator, Is.Not.Null, "IValidator service should be registered");
                Console.WriteLine("IValidator service is properly registered");

                // Create admin user and security setup (from ExampleTest)
                var objectSpace = components.ObjectSpaceProvider.CreateObjectSpace() as EFCoreObjectSpace;
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
                components.XafApplication.Security = Login("Admin", "123456", objectSpace, components.ObjectSpaceProvider);
                Console.WriteLine("Security configured and user logged in");

                // Test business operations with security
                var customerController = components.XafApplication.CreateController<CustomerController>();

                var customerOs = components.XafApplication.CreateObjectSpace(typeof(Customer));
                var customer = customerOs.CreateObject<Customer>();
                customer.Name = "Test Customer with Security";
                customer.Active = false;

                var customerDetailView = components.XafApplication.CreateDetailView(customerOs, customer, true);
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
                var helloWorldService = components.ServiceProvider.GetService<IHelloWorldService>();
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
            finally
            {
                // Clean up this test instance
                components?.Dispose();
            }
        }


        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        protected override XafEnvironment CreateTestInstance(Startup startup)
        {
            return base.CreateTestInstance(startup);
        }

        protected override IConfigurationRoot DefaultConfigurationGetConfiguration(Dictionary<string, string?> Values = null)
        {
            return base.DefaultConfigurationGetConfiguration(Values);
        }

        protected override SecurityStrategyComplex Login(string userName, string password, IObjectSpace loginObjectSpace, IObjectSpaceProvider objectSpaceProvider)
        {
            return base.Login(userName, password, loginObjectSpace, objectSpaceProvider);
        }
    }
}
