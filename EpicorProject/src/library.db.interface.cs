using System.Linq;

using Erp;
using Erp.Tables;

using Ice;
using Ice.Tables;

namespace EFx.References.Implementation
{
    internal interface ILibraryContext
    {
        IQueryable<BpDirective> BpDirective { get; }
        IQueryable<QueryHdr> QueryHdr { get; }
        IQueryable<ReportStyle> ReportStyle { get; }
        IQueryable<RptDataDef> RptDataDef { get; }
        IQueryable<XXXDef> XXXDef { get; }
        IQueryable<ZBODef> ZBODef { get; }
        IQueryable<ZDataField> ZDataField { get; }
        IQueryable<ZDataTable> ZDataTable { get; }
    }
}
