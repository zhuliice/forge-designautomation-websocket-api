using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using Autodesk.Forge.DesignAutomation.Model;
using Autodesk.Forge.DesignAutomation;
using Autodesk.Forge.Core;
using System.Net;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Client
{
    class App
    {

        static readonly string PackageName = "MyTestPackage";
        static readonly string ActivityName = "MyTestActivity";
        static readonly string Label = "prod";
        static readonly string TargetEngine = "Autodesk.AutoCAD+24_1";

        DesignAutomationClient api;
        ForgeConfiguration config;
        public App(DesignAutomationClient api, IOptions<ForgeConfiguration> config)
        {
            this.api = api;
            this.config = config.Value;
        }
        public async Task RunAsync()
        {
            var (owner, token) = await GetOwnerAsync();
            if (token == null)
            {
                Console.WriteLine("Exiting.");
                return;
            }

            var myApp = await SetupAppBundleAsync(owner);
            var myActivity = await SetupActivityAsync(owner, myApp);

            await SubmitWorkItemAsync(token, myActivity);
        }

        private async Task SubmitWorkItemAsync(string token, string myActivity)
        {
            Console.WriteLine("Submitting workitem...");
            var wi = new WorkItem()
            {
                ActivityId = myActivity,
                Arguments = new Dictionary<string, IArgument>()
                {
                    { "input", new XrefTreeArgument() { Url = "http://download.autodesk.com/us/samplefiles/acad/blocks_and_tables_-_imperial.dwg" } },
                    { "params", new XrefTreeArgument() { Url = "data:application/json, {\"ExtractBlockNames\":true, \"ExtractLayerNames\":true}" } },
                }
            };

            var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri("wss://websockets.forgedesignautomation.io"), CancellationToken.None);
            var msg = "{\"action\":\"post-workitem\", \"data\":" + JsonConvert.SerializeObject(wi) + ", \"headers\": {\"Authorization\":\"" + token + "\"}}";
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                var res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                try
                {
                    var json = JObject.Parse(Encoding.UTF8.GetString(buffer));
                    switch (json["action"].ToString())
                    {
                        case "progress":
                            Console.WriteLine($"Progress data:{json["data"]}");
                            break;
                        case "status":
                            var workItemStatus = JsonConvert.DeserializeObject<WorkItemStatus>(json["data"].ToString());
                            Console.WriteLine($"Status: {workItemStatus.Status}.");
                            if (workItemStatus.Status != Status.Pending && workItemStatus.Status != Status.Inprogress)
                            {
                                var fname = await DownloadToDocsAsync(workItemStatus.ReportUrl, "Das-report.txt");
                                Console.WriteLine($"Downloaded {fname}.");
                                return; // we have reached some conclusion
                            }
                            break;
                        case "error":
                            Console.WriteLine(json["data"]);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Buffer: {Encoding.UTF8.GetString(buffer)}");
                    Console.WriteLine(e);
                }
            }
        }

        private async Task<string> SetupActivityAsync(string owner, string myApp)
        {
            Console.WriteLine("Setting up activity...");
            var myActivity = $"{owner}.{ActivityName}+{Label}";
            var actResponse = await this.api.ActivitiesApi.GetActivityAsync(myActivity, throwOnError: false);
            var activity = new Activity()
            {
                Appbundles = new List<string>()
                    {
                        myApp
                    },
                CommandLine = new List<string>()
                    {
                        $"\"$(engine.path)\\accoreconsole.exe\" /i \"$(args[input].path)\" /al \"$(appbundles[{PackageName}].path)\" /s \"$(settings[script].path)\""
                    },
                Engine = TargetEngine,
                Settings = new Dictionary<string, ISetting>()
                    {
                        { "script", new StringSetting() { Value = "_test params.json\n" } }
                    },
                Parameters = new Dictionary<string, Parameter>()
                    {
                        { "input", new Parameter() { Verb= Verb.Get, LocalName = "$(HostDwg)",  Required = true } },
                        { "params", new Parameter() { Verb= Verb.Get, LocalName = "params.json", Required = true} },
                    },
                Id = ActivityName
            };
            if (actResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Creating activity {myActivity}...");
                await api.CreateActivityAsync(activity, Label);
                return myActivity;
            }
            await actResponse.HttpResponse.EnsureSuccessStatusCodeAsync();
            Console.WriteLine("\tFound existing activity...");
            if (!Equals(activity, actResponse.Content))
            {
                Console.WriteLine($"\tUpdating activity {myActivity}...");
                await api.UpdateActivityAsync(activity, Label);
            }
            return myActivity;

            bool Equals(Activity a, Activity b)
            {
                Console.Write("\tComparing activities...");
                //ignore id and version
                b.Id = a.Id;
                b.Version = a.Version;
                var res = a.ToString() == b.ToString();
                Console.WriteLine(res ? "Same." : "Different");
                return res;
            }
        }

        private async Task<string> SetupAppBundleAsync(string owner)
        {
            Console.WriteLine("Setting up appbundle...");
            var myApp = $"{owner}.{PackageName}+{Label}";
            var appResponse = await this.api.AppBundlesApi.GetAppBundleAsync(myApp, throwOnError: false);
            var app = new AppBundle()
            {
                Engine = TargetEngine,
                Id = PackageName
            };
            var package = CreateZip();
            if (appResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"\tCreating appbundle {myApp}...");
                await api.CreateAppBundleAsync(app, Label, package);
                return myApp;
            }
            await appResponse.HttpResponse.EnsureSuccessStatusCodeAsync();
            Console.WriteLine("\tFound existing appbundle...");
            if (! await EqualsAsync(package, appResponse.Content.Package))
            {
                Console.WriteLine($"\tUpdating appbundle {myApp}...");
                await api.UpdateAppBundleAsync(app, Label, package);
            }
            return myApp;

            async Task<bool> EqualsAsync(string a, string b)
            {
                Console.Write("\tComparing bundles...");
                using (var aStream = File.OpenRead(a))
                {
                    var bLocal = await DownloadToDocsAsync(b, "das-appbundle.zip");
                    using (var bStream = File.OpenRead(bLocal))
                    {
                        using (var hasher = SHA256.Create())
                        {
                            var res = hasher.ComputeHash(aStream).SequenceEqual(hasher.ComputeHash(bStream));
                            Console.WriteLine(res ? "Same." : "Different");
                            return res;
                        }
                    }
                }
            }
        }

        private async Task<(string owner, string token)> GetOwnerAsync()
        {
            Console.WriteLine("Getting owner...");
            var resp = await api.ForgeAppsApi.GetNicknameAsync("me");
            return (resp.Content, resp.HttpResponse.RequestMessage.Headers.Authorization.ToString());
        }
        static string CreateZip()
        {
            Console.WriteLine("\tGenerating autoloader zip...");
            string zip = "package.zip";
            if (File.Exists(zip))
                File.Delete(zip);
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                string bundle = PackageName + ".bundle";
                string name = "PackageContents.xml";
                archive.CreateEntryFromFile(name, Path.Combine(bundle, name));
                name = "CrxApp.dll";
                archive.CreateEntryFromFile(name, Path.Combine(bundle, "Contents", name));
                name = "Newtonsoft.Json.dll";
                archive.CreateEntryFromFile(name, Path.Combine(bundle, "Contents", name));
            }
            return zip;

        }

        static async Task<string> DownloadToDocsAsync(string url, string localFile)
        {
            var fname = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), localFile);
            using (var client = new HttpClient())
            {
                var content = (await client.GetAsync(url)).Content;
                using (var output = System.IO.File.Create(fname))
                {
                    (await content.ReadAsStreamAsync()).CopyTo(output);
                    output.Close();
                }
            }
            return fname;
        }
    }
}
