namespace Virgil.SDK
{
    using Virgil.SDK.Cryptography;
    using Virgil.SDK.Storage;

    internal class ServiceLocator
    {
        private static ServiceContainer Container { get; set; }

        public static TService Resolve<TService>()
        {
            if (Container != null)
            {
                return Container.Resolve<TService>();
            }

            Container = new ServiceContainer();

            InitializeDefault();

            return Container.Resolve<TService>();
        }

        private static void InitializeDefault()
        {
            Container.RegisterSingleton<IKeyStorage, VirgilKeyStorage>();
            Container.RegisterTransient<ICrypto, VirgilCrypto>();
        }

        public static void SetServiceResolver(ServiceContainer container)
        {
            Container = container;
        }
    }
}