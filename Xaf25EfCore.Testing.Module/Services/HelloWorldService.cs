namespace Xaf25EfCore.Testing.Module.Services
{
    public class HelloWorldService : IHelloWorldService
    {
        public string GetGreeting()
        {
            return "Hello, World!";
        }

        public string GetGreeting(string name)
        {
            return $"Hello, {name}!";
        }
    }
}