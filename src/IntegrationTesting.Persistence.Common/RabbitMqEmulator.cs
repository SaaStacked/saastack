using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.RabbitMq;

namespace IntegrationTesting.Persistence.Common
{
    /// <summary>
    /// An emulator for running RabbitMQ for integration testing.
    /// </summary>
    public class RabbitMqEmulator
    {
        private const string DockerImageName = "rabbitmq:3-management";
        private const int RabbitMqPort = 5672;
        private const int RabbitMqUiPort = 15672;
        
        private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage(DockerImageName)
            .WithName("rabbitmq-emulator")
            .WithPortBinding(RabbitMqPort, true)
            .WithPortBinding(RabbitMqUiPort, assignRandomHostPort: true)
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RabbitMqPort))
            .Build();

        /// <summary>
        /// Gets the AMQP connection string for the running RabbitMQ container.
        /// </summary>
        /// <returns>The connection string in the format: "amqp://guest:guest@hostname:port/".</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the emulator has not been started.
        /// </exception>
        public string GetConnectionString()
        {
            if (!IsRunning())
            {
                throw new InvalidOperationException(
                    "RabbitMQ emulator must be started before getting the connection string.");
            }

            var hostname = _rabbitMqContainer.Hostname;
            var port = _rabbitMqContainer.GetMappedPublicPort(RabbitMqPort);
            return $"amqp://guest:guest@{hostname}:{port}/";
        }

        public string GetManagementUri()
        {
            return $"http://{_rabbitMqContainer.Hostname}:{_rabbitMqContainer.GetMappedPublicPort(RabbitMqUiPort)}";
        }

        /// <summary>
        /// Determines whether the RabbitMQ container is running.
        /// </summary>
        /// <returns>True if running; otherwise, false.</returns>
        private bool IsRunning()
        {
            return _rabbitMqContainer.State == TestcontainersStates.Running;
        }

        /// <summary>
        /// Starts the RabbitMQ container.
        /// </summary>
        public async Task StartAsync()
        {
            await _rabbitMqContainer.StartAsync();
        }

        /// <summary>
        /// Stops and disposes the RabbitMQ container.
        /// </summary>
        public async Task StopAsync()
        {
            await _rabbitMqContainer.DisposeAsync();
        }
    }
}
