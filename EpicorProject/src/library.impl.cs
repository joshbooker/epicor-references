using System;
using System.Collections.Generic;

using Epicor.Functions;

namespace EFx.References.Implementation
{
    public sealed class LibraryImpl : IFunctionsFactory
    {
        #region Data members

        private readonly IReadOnlyDictionary<FunctionId, Func<IFunctionHost, IFunctionAdapter>> adapters;

        private readonly IReadOnlyDictionary<FunctionId, IFunctionRestAdapter> restAdapters;

        private readonly LibraryId libraryId = new LibraryId("References");

        #endregion // Data members

        public LibraryImpl()
        {
            this.adapters =
                new Dictionary<FunctionId, Func<IFunctionHost, IFunctionAdapter>>
                {
                    [new FunctionId("DownloadAssemblies")] =
                        host => new DownloadAssembliesImpl(host),
                    [new FunctionId("GetAvailableReferences")] =
                        host => new GetAvailableReferencesImpl(host),
                };

            this.restAdapters =
                new Dictionary<FunctionId, IFunctionRestAdapter>
                {
                    [new FunctionId("DownloadAssemblies")] =
                        new FunctionRestAdapter<DownloadAssembliesInput, DownloadAssembliesOutput>(
                            this.libraryId,
                            new FunctionId("DownloadAssemblies"),
                            host => new DownloadAssembliesImpl(host)),
                    [new FunctionId("GetAvailableReferences")] =
                        new FunctionRestAdapter<FunctionInput, GetAvailableReferencesOutput>(
                            this.libraryId,
                            new FunctionId("GetAvailableReferences"),
                            host => new GetAvailableReferencesImpl(host)),
                };
        }

        public IFunctionAdapter CreateAdapter(FunctionId functionId, IFunctionHost host)
        {
            return this.adapters.TryGetValue(functionId, out var factory)
                ? factory(host)
                : null;
        }

        public IFunctionRestAdapter CreateRestAdapter(FunctionId functionId)
        {
            return this.restAdapters.TryGetValue(functionId, out var adapter)
                ? adapter
                : null;
        }
    }
}
