using ITuple = System.Runtime.CompilerServices.ITuple;

namespace EFx.References.Implementation
{
    partial class GetAvailableReferencesImpl
    {
        protected override ITuple AdapterRun(ITuple input)
        {
            var functionInput = new Epicor.Functions.FunctionInput();

            var result = this.Run(functionInput);

            return System.Tuple.Create(
                result.Success,
                result.Message,
                result.Assemblies);
        }

        protected override object[] AdapterRun(params object[] input)
        {
            var functionInput = new Epicor.Functions.FunctionInput();

            var result = this.Run(functionInput);

            return
                new object[]
                {
                    result.Success,
                    result.Message,
                    result.Assemblies,
                };
        }
    }
}
