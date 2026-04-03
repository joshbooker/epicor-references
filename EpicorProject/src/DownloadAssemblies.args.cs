namespace EFx.References.Implementation
{
    public sealed class DownloadAssembliesInput : Epicor.Functions.FunctionInput
    {
        public System.Data.DataSet Assemblies { get; set; }
    }

    public sealed class DownloadAssembliesOutput : Epicor.Functions.FunctionOutput
    {
        public System.Boolean Success { get; set; }
        public System.String Message { get; set; }
        public System.String ZipBase64 { get; set; }
    }
}
