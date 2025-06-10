# Xaf25EfCore.Testing

## ExampleTest Class Explained

The ExampleTest class in `Tests\UnitTest1.cs` is a unit test designed to validate the functionality of the `CustomerController.SetCustomerActive` action in a DevExpress XAF (eXpressAppFramework) application. This test ensures that when the action is executed, a customer's `Active` property is correctly set to `true`.

### Key Components in the Test

#### 1. Database Setup
- Uses an EF Core in-memory database for testing
- Creates a clean database for each test using a unique GUID
- Configures the database to use lazy loading and change tracking proxies
objectSpaceProvider = new EFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
    (builder, _) => builder
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .UseLazyLoadingProxies()
        .UseChangeTrackingProxies()
);
#### 2. Application Setup
- Creates an instance of `TestApplication` (a minimal XAF application)
- Adds the `TestingModule` to the application
- Sets up the application with the object space provider
application = new TestApplication();
TestingModule xafFunctionalTestModule = new TestingModule();
application.Modules.Add(xafFunctionalTestModule);
application.Setup("TestApplication", objectSpaceProvider);
#### 3. Security Configuration
- Creates an admin user with password "123456"
- Creates an "Administrators" role with full administrative privileges
- Configures security with a `SecurityStrategyComplex` implementation
var userAdmin = ObjectSpace.CreateObject<ApplicationUser>();
userAdmin.UserName = "Admin";
userAdmin.SetPassword("123456");
// ...
PermissionPolicyRole adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
adminRole.Name = "Administrators";
adminRole.IsAdministrative = true;
userAdmin.Roles.Add(adminRole);
// ...
application.Security = Login("Admin", "123456", ObjectSpace);
#### 4. Testing the Controller
- Creates a `CustomerController` instance
- Creates a new `Customer` object with `Active` set to `false`
- Creates a detail view for the customer
- Associates the controller with the view
- Executes the `SetCustomerActive` action
- Verifies that the customer's `Active` property was set to `true`
var customerController = application.CreateController<CustomerController>();
var CustomerOs = application.CreateObjectSpace(typeof(Customer));
var customer = CustomerOs.CreateObject<Customer>();
customer.Name = "Test Customer";
customer.Active = false;
var CustomerDetailView = application.CreateDetailView(CustomerOs, customer, true);
customerController.SetView(CustomerDetailView);
customerController.SetCustomerActive.DoExecute();
Assert.That(customer.Active);
### Test Flow Diagram
sequenceDiagram
    participant Test as ExampleTest
    participant OSP as ObjectSpaceProvider
    participant App as TestApplication
    participant OS as ObjectSpace
    participant Controller as CustomerController
    participant Customer as Customer
    participant View as DetailView
    participant Security as SecurityStrategyComplex
    
    Test->>OSP: Create EFCoreObjectSpaceProvider with in-memory DB
    Test->>OSP: CreateObjectSpace as UpdateOs
    Test->>UpdateOs: EnsureCreated
    Test->>App: Create TestApplication
    Test->>App: Add TestingModule
    Test->>App: Setup with objectSpaceProvider
    Test->>OSP: CreateObjectSpace as ObjectSpace
    Test->>ObjectSpace: EnsureCreated
    Test->>ObjectSpace: Create ApplicationUser (Admin)
    Test->>ObjectSpace: Set password "123456"
    Test->>ObjectSpace: CommitChanges
    Test->>ObjectSpace: Create/Get AdminRole
    Test->>ObjectSpace: Set AdminRole.IsAdministrative = true
    Test->>ObjectSpace: Add role to userAdmin
    Test->>ObjectSpace: CommitChanges
    Test->>Security: Create with Login("Admin", "123456", ObjectSpace)
    Test->>App: Set Security = security
    Test->>App: CreateController<CustomerController>
    Test->>App: CreateObjectSpace for Customer
    Test->>OS: Create Customer with Active=false
    Test->>App: CreateDetailView for Customer
    Test->>Controller: SetView(CustomerDetailView)
    Test->>Controller: SetCustomerActive.DoExecute()
    Test->>Customer: Set Active=true (internal)
    Test->>Test: Assert(customer.Active)
### Key Methods and Their Purpose

#### Setup Method
This is a standard NUnit setup method. It's empty in this test because all setup is done in the test method itself.

#### Test1 Method
The main test method that:
1. Sets up the database and application environment
2. Creates necessary objects and security
3. Tests the controller action
4. Verifies the expected result

#### Login Method
Creates and configures a security system for the application:
- Creates an authentication mechanism
- Sets up security strategy for role-based access control
- Logs in the specified user
- Returns the configured security system

#### TearDown Method
Cleans up resources by disposing the objectSpaceProvider and application objects.

### XAF Framework Components Used

- **EFCoreObjectSpaceProvider**: Creates an object space to interact with the Entity Framework Core database
- **TestApplication**: A minimal XAF application implementation for testing
- **SecurityStrategyComplex**: Provides role-based security for the application
- **CustomerController**: The controller being tested, which contains the SetCustomerActive action
- **DetailView**: A view that displays a single object instance in detail

### Test Conclusion

The test confirms that when the `SetCustomerActive` action is executed on a customer through the CustomerController, the customer's Active property is correctly updated to true. This validates that the controller's business logic functions as expected within the XAF framework environment.

The test is designed to run in an isolated, in-memory database environment, which allows for fast and repeatable tests without affecting any external data sources.