using System;

using Epicor.Functions;

namespace EFx.References.Implementation
{
    internal class ReferencesLibraryProxy
    {
        #region Data members

        private readonly IFunctionHost host;

        private static readonly LibraryId LibraryId = new LibraryId("References");

        #endregion // Data members

        internal ReferencesLibraryProxy(IFunctionHost host)
        {
            this.host = host;
        }

        public (System.Boolean Success, System.String Message, System.String ZipBase64) DownloadAssemblies(
            System.Data.DataSet Assemblies)
        {
            var functionRefId = new FunctionRefId(LibraryId, new FunctionId("DownloadAssemblies"));
            using (var adapter = this.host.GetFunctionAdapter(functionRefId))
            {
                var @return = adapter.Run(
                    Assemblies);

                return (
                    (System.Boolean)@return[0],
                    (System.String)@return[1],
                    (System.String)@return[2]);
            }
        }

        public (System.Boolean Success, System.String Message, System.Data.DataSet Assemblies) GetAvailableReferences()
        {
            var functionRefId = new FunctionRefId(LibraryId, new FunctionId("GetAvailableReferences"));
            using (var adapter = this.host.GetFunctionAdapter(functionRefId))
            {
                var @return = adapter.Run();

                return (
                    (System.Boolean)@return[0],
                    (System.String)@return[1],
                    (System.Data.DataSet)@return[2]);
            }
        }
    }
}
