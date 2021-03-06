using Newtonsoft.Json.Linq;
using PX.Common;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
/// <summary>
/// Provides client script configuration
/// </summary>
public class ClientSideAppsHelper
{
	[PXInternalUseOnly]
	private static string GetExternalBandle(string fileName)
	{
		string foundedString = null;

		string fileContent = File.ReadAllText(fileName);
		var fileNameArray = fileName.Split(Path.DirectorySeparatorChar);
		var bundleOfFile = fileNameArray[fileNameArray.Length - 2];

		var m = Regex.Match(fileContent, "\"controls/" + bundleOfFile + "/vendor-bundle\":(\\s+|)\\[.*?\\]", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.Compiled);
		if (m.Success)
		{
			return m.Value;
		}

		return foundedString;
	}

	[PXInternalUseOnly]
	[Obsolete]
	public static string RenderScriptConfiguration()
	{

		var localizedNoResults = PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.SuggesterNothingFound);
		var cacheKey = "ClientSideAppsConfig:" + localizedNoResults;
		string cachedResult = HttpContext.Current.Cache[cacheKey] as string;
		if (cachedResult != null)
		{
			return cachedResult;
		}

		const string clientSideAppsRoot = "/scripts/ca/";
		var appRoot = HttpContext.Current.Request.ApplicationPath;
		var root = appRoot;

		if (root.Equals("/"))
		{
			root = "";
		}
		root = root + clientSideAppsRoot;

		var scriptDir = System.Web.HttpContext.Current.Server.MapPath("~" + clientSideAppsRoot);
		var bundleFiles = Directory.GetFiles(scriptDir, "*-bundle.js", SearchOption.AllDirectories);
		var requireMetaFiles = Directory.GetFiles(scriptDir, "require-meta.js", SearchOption.AllDirectories);

		var vendorBundleTicks = File.GetLastWriteTime(Path.Combine(scriptDir, "vendor-bundle.js")).Ticks;

		var latestBundleTicks = bundleFiles.Select(f => File.GetLastWriteTime(f).Ticks).Max();

		var metaBundles = string.Join(",", requireMetaFiles.Select(GetExternalBandle).ToArray());

		var bundles = bundleFiles
			.Select(x => x.Replace(scriptDir, "").Replace("\\", "/"))
			.ToArray()
			;

		var apps = bundles.Where(b => b.Contains("app-")).ToList();
		//var resources = bundles.Where(b => b.Contains("resources-")).ToArray();
		var controls = bundles.Where(b => b.Contains("controls-")).ToArray();
		var controlBundles = controls.Select(a =>
		{
			var bundleName = a.Replace(".js", "");
			var moduleName = bundleName.Replace("controls-bundle", "index");
			return string.Format(@"""{0}"":[""{1}""]", bundleName, moduleName);
		}).ToArray();



		var sb = new StringBuilder();
		sb.Append(@"
<script>
");
		sb.AppendFormat("var __svg_icons_path = \"{0}/Content/svg_icons/\";\n", appRoot);
		sb.Append(@"
window.ClientLocalizedStrings = {
");

		sb.AppendFormat("noResultsFound: \"{0}\",\n", HttpUtility.HtmlDecode(localizedNoResults).Replace("\"", "\\\""));
		sb.AppendLine("lastUpdate: {");
		sb.AppendFormat("JustNow: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateJustNow)).Replace("\"", "\\\""));
		sb.AppendFormat("MinsAgo: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateMinsAgo)).Replace("\"", "\\\""));
		sb.AppendFormat("HoursAgo: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateHoursAgo)).Replace("\"", "\\\""));
		sb.AppendFormat("DaysAgo: \"{0}\",\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateDaysAgo)).Replace("\"", "\\\""));
		sb.AppendFormat("LongAgo: \"{0}\"\n", HttpUtility.HtmlDecode(PX.Data.PXMessages.LocalizeNoPrefix(PX.Web.UI.Msg.LastUpdateLongAgo)).Replace("\"", "\\\""));

		sb.AppendLine("}}");




		sb.AppendFormat(@"
window.globalControlsModules=[""{0}""];
", String.Join("\",\"", controls.Select(x => x.Replace(".js", "").Replace("/controls-bundle", ""))));

		var bundleArray = apps.Select(a =>
		{
			var bundleName = a.Replace(".js", "");
			var moduleName = bundleName.Replace("app-bundle", "main");
			return string.Format(@"""{0}"":[""{1}""]", bundleName, moduleName);
		}).Union(controlBundles)

		.ToArray();


		sb.AppendFormat(@"
requirejs = {{
	baseUrl: ""{0}"",
	paths: {{
		root: """"
	}},
	waitSeconds: 30,
	urlArgs: ""b={2}"",
	packages: [],
	stubModules: [
	""text""
	],
	shim: {{}},
	bundles: {{{1}
	,{3}
	}}

}}
</script>", root, string.Join(",\n", bundleArray), latestBundleTicks, metaBundles);

		sb.AppendFormat(@"<script src=""{0}vendor-bundle.js?b={1}"" data-main=""apps/enhance/main"" defer></script>", root, vendorBundleTicks);

		sb.AppendFormat(@"<!--{0}-->", System.DateTime.UtcNow);

		var result = sb.ToString();

		HttpContext.Current.Cache.Insert(cacheKey, result, new System.Web.Caching.CacheDependency(bundleFiles), System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration);


		return result;
	}


}