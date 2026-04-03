using Epicor.Customization.Bpm;
using Epicor.Data;
using Epicor.Hosting;
using Epicor.Utilities;
using Erp;
using Erp.Tables;
using Ice;
using Ice.Contracts;
using Ice.Customization.Sandbox;
using Ice.ExtendedData;
using Ice.Tables;
using Ice.Tablesets;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace EFx.References.Implementation
{
    partial class GetAvailableReferencesImpl
    {
        [Epicor.Customization.CustomCode]
        protected override void Run()
        {
            var FunctionID = this.ToString().Split(".").Last().Replace("Impl", "");
            var Messages = new List<string>(); Messages.Add($"Begin {FunctionID} ->{Environment.NewLine}");
            Action<string> AddMessage = (s) => {if(!(String.IsNullOrEmpty(s) || String.IsNullOrWhiteSpace(s))) {Messages.Add( $"  {FunctionID}:{Environment.NewLine}" + string.Join(Environment.NewLine, s.Split(Environment.NewLine).Select(x => $"    {x}")) + Environment.NewLine);}};
            Func<object, string> Pretty = (o) => Newtonsoft.Json.JsonConvert.SerializeObject(o, Newtonsoft.Json.Formatting.Indented);  
                
            try
            {
                Assemblies = new DataSet();
                var asmTable = Assemblies.Tables.Add("Assemblies");
                asmTable.Columns.Add("AssemblyName", typeof(string));
                asmTable.Columns.Add("FileName", typeof(string));
                asmTable.Columns.Add("Version", typeof(string));
            
            
                //Epicor.System missing, but available, wtf lol..
                asmTable.Rows.Add("Epicor.System", "Epicor.System.dll", "");
                
                CallService<Ice.Contracts.BpMethodSvcContract>(bpMethod =>
                {
                    var bpMethodList = bpMethod.GetAvailableReferences("Assemblies");
                    
                    //Message = Pretty(bpMethodList);
            
                    bpMethodList.ForEach(x =>
                    {
                        asmTable.Rows.Add(x.Name, x.FileName, x.Version);
                    });        
                });
               
                Success = true;    
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
                AddMessage(Pretty(ex));
            }
            finally
            {
                Messages.Add($"<- End {FunctionID}");
                Message = String.Join(Environment.NewLine, Messages);
            }
        }
    }
}
