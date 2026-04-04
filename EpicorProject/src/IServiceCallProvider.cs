namespace EFx.References.Implementation
{
    /// <summary>
    /// Seam that intercepts Epicor service calls in <see cref="Function{TInput,TOutput}"/>.
    /// Inject a test implementation via <see cref="Function{TInput,TOutput}.ServiceCallProvider"/>
    /// to run <c>Run()</c> methods without a live Epicor server.
    /// </summary>
    internal interface IServiceCallProvider
    {
        void CallService<TService>(System.Action<TService> action);
    }
}
