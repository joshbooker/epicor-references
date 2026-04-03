namespace EFx.References.Implementation
{
    [Epicor.Functions.FunctionName("DownloadAssemblies")]
    internal sealed partial class DownloadAssembliesImpl :
        Function<DownloadAssembliesInput, DownloadAssembliesOutput>
    {
        //
        // Arguments
        //

        private System.Data.DataSet Assemblies;
        private System.Boolean Success;
        private System.String Message;
        private System.String ZipBase64;

        public DownloadAssembliesImpl(Epicor.Functions.IFunctionHost host)
            : base(host)
        {
        }

        protected override string FunctionID => "DownloadAssemblies";

        protected override void Prepare(DownloadAssembliesInput functionInput)
        {
            this.Assemblies = functionInput.Assemblies;
            this.Success = default(System.Boolean);
            this.Message = default(System.String);
            this.ZipBase64 = default(System.String);
        }

        protected override DownloadAssembliesOutput PrepareOutput(Epicor.Functions.IDataCensor dataCensor)
        {
            var @return =
                new DownloadAssembliesOutput
                {
                    Success = this.Success,
                    Message = this.Message,
                    ZipBase64 = this.ZipBase64,
                };

            return @return;
        }
    }
}
