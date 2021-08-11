using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;
using Newtonsoft.Json;
using UtilityLib.Conversion;
using System.IO;
using System.Windows.Forms;
using System.Windows;

namespace kpm
{
	class Program
	{
		public static string CurrentPath { get; set; }
		public static string AppPath { get; set; }
		public static bool EnableGui { get; set; }

		private static Table RenderCommandTable(string title, string[] columns, string[][] rows)
		{
			Table commands = new Table();
			commands.AddColumns(columns);
			for (int i = 0; i < rows.Length; i++)
			{
				string[] row = rows[i];
				commands.AddRow(row);
			}
			commands.Centered().RoundedBorder().Title = new TableTitle(title);
			return commands;
		}

		private static void RenderCommandTable(Table commandTable)
		{
			AnsiConsole.Render(new Text("\n\n"));
			AnsiConsole.Render(commandTable);
			return;
		}

		#region installCommand
		private static readonly Table installCommand =
			RenderCommandTable(
				"install",
				new string[3]
				{
					"Parameter ID",
					"Usage",
					"description"
				},
				new string[2][]
				{
					new string[3]
					{
						"-id",
						"kpm install -id <unique id of the package>",
						"installs the package with the entered id"
					},
					new string[3]
					{
						"-p",
						"kpm install -p <installation path>",
						"installs the package in the entered directory"
					}
				}
				);
		#endregion

		#region updateCommand
		private static readonly Table updateCommand =
			RenderCommandTable(
				"update",
				new string[3]
				{
					"Parameter ID",
					"Usage",
					"description"
				},
				new string[1][]
				{
					new string[3] { "<null>", "kpm update", "updates all packages if updates are available" }
				}
				);
		#endregion

		#region searchCommand
		private static readonly Table searchCommand =
			RenderCommandTable(
				"search",
				new string[3]
				{
					"Parameter ID",
					"Usage",
					"description"
				},
				new string[1][]
				{
					new string[3]
					{
						"-id",
						"kpm search -id <search query>",
						"lists all packages which id contains the entered query"
					}
				}
				);
		#endregion

		static void Main(string[] args)
		{
			EnableGui = false;

			string AppName = "kpm.exe";
			AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			AppPath = AppPath.Remove(AppPath.Length - 1 - AppName.Length);
			PackageManager.InitPM();
			var starwarsFont = FigletFont.Load($"{AppPath}/block.flf");


			void RenderLogo()
			{
				AnsiConsole.Render(
					new FigletText(starwarsFont, "KPM")
					.Centered()
					.Color(Color.Green)
				);
				AnsiConsole.Render(
					new Markup("[green u link=http://tim.krals.ch]kingsoft products[/]").Centered()
				);
			}

			RenderLogo();

			switch (args.Length)
			{
				case 0:
					RenderCommandTable(installCommand);
					RenderCommandTable(updateCommand);
					RenderCommandTable(searchCommand);
					break;
				case 1:
					switch (args[0])
					{
						case "-gui":
							EnableGui = true;
							kpmHelpForm form = new kpmHelpForm();
							Application.Run(form);
							break;
						default:
							Console.WriteLine(CurrentPath);
							PackageManager.ExecutePackageFile(File.ReadAllText(args[0]));
							CurrentPath = args[0];
							Console.ReadLine();
							break;
					}
					break;
				default:
					if (args.Length >= 2)
					{
						switch (args[0])
						{
							#region case:test
							case "test":
								PackageFile file = new PackageFile()
								{
									Actions = new PackFileAction[1] {
								new PackFileAction() {
									Type = "download",
									Path ="test1/lol.txt",
									Uri="http://tim.krals.ch" } }
								};

								PackageRegistryFile file2 = new PackageRegistryFile()
								{
									ServerName = "KingsoftMainNode",
									Entrees = new PackageRegistryEntree[1] {
								new PackageRegistryEntree() {
									Id="test", Author="Tim Kral",
									Description="it's just a test",
									PackageFileUri="" } }
								};

								Console.WriteLine("");
								Console.WriteLine("PFA");
								Console.WriteLine(JsonConvert.SerializeObject(file, Formatting.Indented));
								Console.WriteLine("");
								Console.WriteLine("PRF");
								Console.WriteLine(JsonConvert.SerializeObject(file2, Formatting.Indented));
								return;
							#endregion
							case "install":
								string path = Environment.CurrentDirectory;
								string id = "";
								for (int i = 1; i < args.Length; i++)
								{
									switch (args[i])
									{
										case "-id":
											i++;
											id = args[i];
											break;
										case "-p":
											i++;
											path = args[i];
											break;
										default:
											break;
									}
								}
								CurrentPath = path;
								PackageManager.InstallPackageById(id);
								return;
							case "search":
								string type = "";
								string value = "";
								for (int i = 1; i < args.Length; i++)
								{
									switch (args[i])
									{
										case "-id":
											i++;
											value = args[i];
											type = "id";
											break;
										default:
											break;
									}
								}
								Table table = new Table().Centered();
								table.AddColumn(new TableColumn("Id"));
								table.AddColumn(new TableColumn("Author"));
								table.AddColumn(new TableColumn("Package-File-Uri"));
								table.AddColumn(new TableColumn("Description"));
								table.Title = new TableTitle("Results");
								AnsiConsole.Render(new Text("\n\n"));
								AnsiConsole.Live(table).Start(ctx =>
								{
									ctx.Refresh();
									bool PrintPackage(PackageRegistryEntree entree)
									{
										if (entree.Id == "<ERROR>")
										{
											table.AddRow(
											new IRenderable[4] {
										new Markup("0"),
										new Markup("null"),
										new Markup("null"),
										new Markup("no result found")
											});
											ctx.Refresh();
											return true;
										}
										table.AddRow(
											new IRenderable[4] {
										new Markup(entree.Id),
										new Markup(entree.Author),
										new Markup($"[u blue link={entree.PackageFileUri}]link[/]"),
										new Markup(entree.Description)
											});
										ctx.Refresh();
										return true;
									}

									PackageManager.SearchPackages(PrintPackage, value, type);
									ctx.Refresh();
								});

								return;
							default:
								break;
						}
					}
					break;
			}
		}
	}
}
