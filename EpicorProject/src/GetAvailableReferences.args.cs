namespace EFx.References.Implementation
{
    public sealed class GetAvailableReferencesOutput : Epicor.Functions.FunctionOutput
    {
        public System.Boolean Success { get; set; }
        public System.String Message { get; set; }
        public System.Data.DataSet Assemblies { get; set; }
    }
}
