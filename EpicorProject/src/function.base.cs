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
