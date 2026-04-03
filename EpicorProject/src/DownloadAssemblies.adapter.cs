using ITuple = System.Runtime.CompilerServices.ITuple;

namespace EFx.References.Implementation
{
    partial class DownloadAssembliesImpl
    {
        protected override ITuple AdapterRun(ITuple input)
        {
            Epicor.Functions.SignatureGuard.NumberOfItems(input.Length, 1);

            var typedInput = (System.Tuple<System.Data.DataSet>)input;
            var functionInput =
                new DownloadAssembliesInput
                {
                    Assemblies = typedInput.Item1,
                };

            var result = this.Run(functionInput);

            return System.Tuple.Create(
                result.Success,
                result.Message,
                result.ZipBase64);
        }

        protected override object[] AdapterRun(params object[] input)
        {
            Epicor.Functions.SignatureGuard.NumberOfItems(input.Length, 1);

            var functionInput =
                new DownloadAssembliesInput
                {
                    Assemblies = (System.Data.DataSet)input[0],
                };

            var result = this.Run(functionInput);

            return
                new object[]
                {
                    result.Success,
                    result.Message,
                    result.ZipBase64,
                };
        }
    }
}
