namespace Virgil.SDK.Clients
{
    /// <summary>
    /// Represents all exposed virgil services
    /// </summary>
    public interface IServiceHub
    {
        /// <summary>
        /// Gets a client that handle requests for <c>Virgil Card</c> resources.
        /// </summary>
        ICardsServiceClient Cards { get; }

        /// <summary>
        /// Gets a client that handle requests <c>Identity</c> service resources.
        /// </summary>
        IIdentityServiceClient Identity { get; }
    }
}