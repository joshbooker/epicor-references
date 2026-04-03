namespace EFx.References.Implementation
{
    [Epicor.Functions.FunctionName("GetAvailableReferences")]
    internal sealed partial class GetAvailableReferencesImpl :
        Function<Epicor.Functions.FunctionInput, GetAvailableReferencesOutput>
    {
        //
        // Arguments
        //

        private System.Boolean Success;
        private System.String Message;
        private System.Data.DataSet Assemblies;

        public GetAvailableReferencesImpl(Epicor.Functions.IFunctionHost host)
            : base(host)
        {
        }

        protected override string FunctionID => "GetAvailableReferences";

        protected override void Prepare(Epicor.Functions.FunctionInput functionInput)
        {
            this.Success = default(System.Boolean);
            this.Message = default(System.String);
            this.Assemblies = default(System.Data.DataSet);
        }

        protected override GetAvailableReferencesOutput PrepareOutput(Epicor.Functions.IDataCensor dataCensor)
        {
            var @return =
                new GetAvailableReferencesOutput
                {
                    Success = this.Success,
                    Message = this.Message,
                    Assemblies = this.Assemblies,
                };

            return @return;
        }
    }
}
