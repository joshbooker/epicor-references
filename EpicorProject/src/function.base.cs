namespace EFx.References.Implementation
{
    internal abstract class Function<TInput, TOutput> :
        Epicor.Functions.FunctionBase<Erp.ErpContext, TInput, TOutput>
        where TInput : Epicor.Functions.FunctionInput
        where TOutput : Epicor.Functions.FunctionOutput
    {
        private readonly System.Lazy<ReferencesLibraryProxy> thisLibraryLazy;

        private readonly System.Lazy<LibraryContext> dbLazy;
        
        protected Function(Epicor.Functions.IFunctionHost host)
            : base(host)
        {
            this.thisLibraryLazy = new System.Lazy<ReferencesLibraryProxy>(
                () => new ReferencesLibraryProxy(host));
            this.dbLazy = new System.Lazy<LibraryContext>(
                () => host.CreateContext<Erp.ErpContext, LibraryContext>(LibraryContext.Create));
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

        protected ILibraryContext Db => this.dbLazy.Value;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.dbLazy.IsValueCreated
                    && this.dbLazy.Value is System.IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}

