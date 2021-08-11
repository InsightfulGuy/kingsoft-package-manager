
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.IO.Compression;
using Spectre.Console;
using System.Reflection;

namespace kpm
{
	public class PackFileAction
	{
		public string Type { get; set; }
		public string Uri { get; set; }
		public string Path { get; set; }
		public string Path2 { get; set; }
	}

	public class PackageFile
	{
		public PackFileAction[] Actions { get; set; }
	}

	public class PackageRegistryEntree
	{
		public string Id { get; set; }
		public string Author { get; set; }
		public string Description { get; set; }
		public string PackageFileUri { get; set; }
	}

	public class PackageRegistryFile
	{
		public string ServerName { get; set; }
		public PackageRegistryEntree[] Entrees { get; set; }
	}

	public class PackageManager
	{
		private static Dictionary<string, string> Nodes { get; set; }
		private static WebClient _WebClient { get; set; }

		public static void InitPM()
		{
			string confFilePath = $"{Program.AppPath}/packRegUris.conf";
			Nodes = new Dictionary<string, string>();
			string mainNodeUri = "http://kingsoft.dyndns-home.com:19138/kpm/Node.json";
			if (!File.Exists(confFilePath)) File.Create(confFilePath).Close();
			if (string.IsNullOrEmpty(File.ReadAllText(confFilePath))) File.WriteAllText(confFilePath, $"main:{mainNodeUri}");

			string[] configFileContents = File.ReadAllLines(confFilePath);

			for (int i = 0; i < configFileContents.Length; i++)
			{
				string line = configFileContents[i];
				if (line.StartsWith("main:"))
				{
					mainNodeUri = line.Remove(0, "main:".Length);
					Nodes.Add("main", mainNodeUri);
				}
				else if (!line.StartsWith("#")) Nodes.Add(line.Split(':')[0], line.Remove(0, $"{line.Split(':')[0]}:".Length));
			}
		}

		public static void InstallPackageById(string id)
		{
			bool installPackage(PackageRegistryEntree entree)
			{
				ExecutePackageFile(_WebClient.DownloadString(entree.PackageFileUri));
				return true;
			}

			ListPackages(installPackage, id, "id");
		}

		public static void SearchPackages(Func<PackageRegistryEntree, bool> printResult, string query, string qType)
		{
			ListPackages(printResult, query, qType, false);
		}

		private static void ListPackages(Func<PackageRegistryEntree, bool> entreeFunction, string query, string qType, bool justOneResult=true)
		{
			bool resFound = false;
			foreach (KeyValuePair<string, string> node in Nodes)
			{
				_WebClient = new WebClient();

				PackageRegistryFile registryFile = 
					JsonConvert.DeserializeObject<PackageRegistryFile>(_WebClient.DownloadString(node.Value));


				for (int i = 0; i < registryFile.Entrees.Length; i++)
				{
					PackageRegistryEntree entree = registryFile.Entrees[i];

					switch (qType)
					{
						case "id":
							if (justOneResult)
							{
								if (entree.Id == query)
								{
									entreeFunction(entree);
									return;
								}
							}
							else
							{
								if (entree.Id.Contains(query))
								{
									entreeFunction(entree);
									resFound = true;
								}
							}
							
							break;
						default:
							return;
					}
				}
			}
			if(!resFound) entreeFunction(new PackageRegistryEntree() { Id="<ERROR>" });
		}

		public static void ExecutePackageFile(string packageFileContent)
		{
			string DecodeFilePath(string inPath)
			{
				string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				desktopPath = desktopPath.Replace("\\", "/");
				string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
				commonStartMenuPath = commonStartMenuPath.Replace("\\", "/");

				string programsPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				programsPath = programsPath.Replace("\\", "/");

				string programs86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
				programs86Path = programs86Path.Replace("\\", "/");

				if (inPath.Contains("{Current}")) inPath = inPath.Replace(
					(inPath.Contains("{Current}/") && Program.CurrentPath == "") 
					? "{Current}/" : "{Current}", Program.CurrentPath != "" ? Program.CurrentPath : "");

				if (inPath.Contains("{Desktop}"))
					inPath = inPath.Replace("{Desktop}", desktopPath);

				if (inPath.Contains("{StartMenu}"))
					inPath = inPath.Replace("{StartMenu}", commonStartMenuPath);

				if (inPath.Contains("{Programs}"))
					inPath = inPath.Replace("{Programs}", programsPath);

				if (inPath.Contains("{Programs86}"))
					inPath = inPath.Replace("{Programs86}", programs86Path);

				return inPath;
			}

			PackageFile packageFile = JsonConvert.DeserializeObject<PackageFile>(packageFileContent);
			AnsiConsole.Render(new Text("\n\n"));
			AnsiConsole.Status().Start("installing package...", ctx =>
			{
				ctx.Spinner = Spinner.Known.Aesthetic;


				for (int i = 0; i < packageFile.Actions.Length; i++)
				{
					PackFileAction action = packageFile.Actions[i];

					if (action.Path != null) action.Path = DecodeFilePath(action.Path);
					if (action.Path2 != null) action.Path2 = DecodeFilePath(action.Path2);

					switch (action.Type)
					{
						case "download":

							if (!File.Exists(action.Path)) File.Create(action.Path).Close();
							_WebClient.DownloadFile(new Uri(action.Uri), action.Path);
							AnsiConsole.MarkupLine($"[grey]LOG:[/] downloaded file from [u blue link={action.Uri}]uri[/]");
							ctx.Refresh();
							break;
						case "mkdir":
							Directory.CreateDirectory(action.Path);
							AnsiConsole.MarkupLine($"[grey]LOG:[/] made Directory \"{action.Path}\"");
							ctx.Refresh();
							break;
						case "mkfile":
							if (!File.Exists(action.Path)) File.Create(action.Path).Close();
							else File.WriteAllText(action.Path, "");
							AnsiConsole.MarkupLine($"[grey]LOG:[/] made File \"{action.Path}\"");
							ctx.Refresh();
							break;
						case "movdir":
							Directory.Move(action.Path, action.Path2);
							AnsiConsole.MarkupLine($"[grey]LOG:[/] moved directory from \"{action.Path}\" to \"{action.Path2}\"");
							break;
						case "movfile":
							File.Move(action.Path, action.Path2);
							AnsiConsole.MarkupLine($"[grey]LOG:[/] moved file from \"{action.Path}\" to \"{action.Path2}\"");
							break;
						case "remdir":
							Directory.Delete(action.Path);
							break;
						case "remfile":
							File.Delete(action.Path);
							break;
						case "unzip":
							if (!Directory.Exists(action.Path2)) Directory.CreateDirectory(action.Path2);
							ZipFile.ExtractToDirectory(action.Path, action.Path2);
							AnsiConsole.MarkupLine($"extracted Directory \"{action.Path}\" to \"{action.Path2}\"");
							break;
						default:
							break;
					}
				}
			});
		}
	}
}
