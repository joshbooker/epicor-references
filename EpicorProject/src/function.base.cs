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
    }
}
