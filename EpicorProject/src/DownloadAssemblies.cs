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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;

namespace EFx.References.Implementation
{
    partial class DownloadAssembliesImpl
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
             
              Func<Dictionary<string, byte[]>, byte[]> ZipDictionary = (dicFiles) =>
              {
                  byte[] retBytes = null;
                  
                  using (MemoryStream zipMS = new MemoryStream())
                  {
                  
                      using (ZipArchive zipArchive = new ZipArchive(zipMS, ZipArchiveMode.Create, true))
                      {
                          dicFiles.Keys.ToList().ForEach(df =>
                          {
                              var zipArchiveEntry = zipArchive.CreateEntry(df, CompressionLevel.Fastest);
                          
                              using (var zipStream = zipArchiveEntry.Open())
                              {
                                  zipStream.Write(dicFiles[df], 0, dicFiles[df].Length);
                              }
                          });
                      }
                      
                      zipMS.Flush();
                      retBytes = zipMS.ToArray();
                  };
                  
                  return retBytes;    
              };
                
                var fileDictionary = new Dictionary<string, byte[]>();
                CallService<Ice.Contracts.EcfToolsSvcContract>(ecf =>
                {
                    Assemblies.Tables["Assemblies"].AsEnumerable().ToList().ForEach(x =>
                    {
                        var data = ecf.GetAssemblyBytes(x.Field<string>("AssemblyName"));
                        fileDictionary.Add(x.Field<string>("FileName"), data);
                    });
                });
            
                if(fileDictionary.Count == 0 ) return; //exit early
                
                ZipBase64 = Convert.ToBase64String( ZipDictionary(fileDictionary) );
            
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
