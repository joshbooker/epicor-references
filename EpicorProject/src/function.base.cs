namespace EFx.References.Implementation
{
    internal abstract class Function<TInput, TOutput> :
        Epicor.Functions.FunctionBase<Erp.ErpContext, TInput, TOutput>
        where TInput : Epicor.Functions.FunctionInput
        where TOutput : Epicor.Functions.FunctionOutput
    {
        private readonly System.Lazy<ReferencesLibraryProxy> thisLibraryLazy;

        protected Function(Epicor.Functions.IFunctionHost host)
            : base(host)
        {
            this.thisLibraryLazy = new System.Lazy<ReferencesLibraryProxy>(
                () => new ReferencesLibraryProxy(host));
        }

        protected sealed override string LibraryID => "References";

        protected ReferencesLibraryProxy ThisLib => this.thisLibraryLazy.Value;

        /// <summary>
        /// Test seam: assign before invoking the adapter to replace live Epicor service
        /// calls with test doubles. When <c>null</c> (production), the inherited
        /// <see cref="Epicor.Functions.FunctionBase{TDataContext,TInput,TOutput}.CallService{TService}"/>
        /// is used unchanged.
        /// </summary>
        internal IServiceCallProvider? ServiceCallProvider { get; set; }

        /// <summary>
        /// Shadows <c>FunctionBase.CallService</c> so that a test-injected
        /// <see cref="ServiceCallProvider"/> can intercept service calls without
        /// requiring a live Epicor runtime. Production behaviour is unchanged because
        /// <see cref="ServiceCallProvider"/> is <c>null</c> at runtime.
        /// </summary>
        protected new void CallService<TService>(System.Action<TService> action)
        {
            if (this.ServiceCallProvider != null)
                this.ServiceCallProvider.CallService(action);
            else
                base.CallService(action);
        }
    }
}

