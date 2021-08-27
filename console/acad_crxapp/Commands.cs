using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json;
using System.IO;
using System.Text;

[assembly: CommandClass(typeof(CrxApp.Commands))]
[assembly: ExtensionApplication(null)]

namespace CrxApp
{
    public class Parameters
    {
        public bool ExtractBlockNames { get; set; }
        public bool ExtractLayerNames { get; set; }
    }

    public class Commands
    {
        static void SendProgress(string msg)
        {
            //he Forge Design Automation infrastructure recognizes this output and passes `msg` to the onProgress argument. In this sample, the 
            //onProgress argument points to the Websocket so the client will receive these messages directly.
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"!ACESAPI:acesHttpOperation(onProgress,null,null,{msg},null)\n");
        }

        [CommandMethod("MyTestCommands", "test", CommandFlags.Modal)]
        static public void Test()
        {
            //prompt for input json and output folder
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var res1 = ed.GetFileNameForOpen("Specify parameter file");
            if (res1.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                return;

            try
            {
                //get parameter from input json
                var parameters = JsonConvert.DeserializeObject<Parameters>(File.ReadAllText(res1.StringResult));
                //extract layer names and block names from drawing as requested and place results in JSON string
                var db = doc.Database;

                var sb = new StringBuilder();
                using (var writer = new JsonTextWriter(new StringWriter(sb)))
                {
                    writer.WriteStartObject();
                    if (parameters.ExtractLayerNames)
                    {
                        writer.WritePropertyName("layers");
                        writer.WriteStartArray();
                        dynamic layers = db.LayerTableId;
                        foreach (dynamic layer in layers)
                            writer.WriteValue(layer.Name);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                    //send progress message to the client with the layer names as payload.
                    SendProgress(sb.ToString());
                }

                sb.Clear();
                using (var writer = new JsonTextWriter(new StringWriter(sb)))
                {
                    writer.WriteStartObject();
                    if (parameters.ExtractBlockNames)
                    {
                        writer.WritePropertyName("blocks");
                        writer.WriteStartArray();
                        dynamic blocks = db.BlockTableId;
                        foreach (dynamic block in blocks)
                            writer.WriteValue(block.Name);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                    // send some more progress this time with the block names.
                    SendProgress(sb.ToString());
                }
                
            }
            catch (System.Exception e)
            {
                ed.WriteMessage("Error: {0}", e);
            }
        }
    }
}
