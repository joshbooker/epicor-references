using System;
using System.Linq;

using Erp;
using Erp.Tables;

using Ice;
using Ice.Tables;

namespace EFx.References.Implementation
{
    internal sealed class LibraryContext : ILibraryContext, IDisposable
    {
        #region Data members

        private ErpContext ctx;

        private readonly bool disposeCtx;

        #endregion // Data members

        private LibraryContext(ErpContext ctx, bool disposeCtx)
        {
            this.ctx = ctx;
            this.disposeCtx = disposeCtx;
        }

        public IQueryable<BpDirective> BpDirective => this.ctx.BpDirective;

        public IQueryable<QueryHdr> QueryHdr => this.ctx.QueryHdr;

        public IQueryable<ReportStyle> ReportStyle => this.ctx.ReportStyle;

        public IQueryable<RptDataDef> RptDataDef => this.ctx.RptDataDef;

        public IQueryable<XXXDef> XXXDef => this.ctx.XXXDef;

        public IQueryable<ZBODef> ZBODef => this.ctx.ZBODef;

        public IQueryable<ZDataField> ZDataField => this.ctx.ZDataField;

        public IQueryable<ZDataTable> ZDataTable => this.ctx.ZDataTable;

        internal static LibraryContext Create(ErpContext ctx, bool disposeCtx)
        {
            return new LibraryContext(ctx, disposeCtx);
        }

        public void Dispose()
        {
            this.ctx.RollbackChangesAtReadOnlyTables();

            if (this.disposeCtx && this.ctx != null)
            {
                this.ctx.Dispose();
                this.ctx = null;
            }
        }
    }
}
