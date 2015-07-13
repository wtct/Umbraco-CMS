﻿using System;
using System.Text.RegularExpressions;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using UmbracoSettings = Umbraco.Core.Configuration.UmbracoSettings;

namespace Umbraco.Web.Templates
{
	//NOTE: I realize there is only one class in this namespace but I'm pretty positive that there will be more classes in 
	//this namespace once we start migrating and cleaning up more code.

	/// <summary>
	/// Utility class used for templates
	/// </summary>
	public static class TemplateUtilities
	{
        internal static void ParseInternalLinks(ref string text, bool preview)
	    {
            // save and set for url provider
            var inPreviewMode = UmbracoContext.Current.InPreviewMode;
            UmbracoContext.Current.InPreviewMode = preview;

            try
            {
                ParseInternalLinks(ref text);
            }
            finally
            {
                // restore
                UmbracoContext.Current.InPreviewMode = inPreviewMode;
            }
	    }

	    /// <summary>
	    /// Parses the string looking for the {localLink} syntax and updates them to their correct links.
	    /// </summary>
	    /// <param name="text"></param>
	    /// <returns></returns>
	    public static void ParseInternalLinks(ref string text)
		{
			//don't attempt to proceed without a context as we cannot lookup urls without one
			if (UmbracoContext.Current == null || UmbracoContext.Current.RoutingContext == null) return;

			var urlProvider = UmbracoContext.Current.UrlProvider;

			// Parse internal links
			var tags = Regex.Matches(text, @"href=""[/]?(?:\{|\%7B)localLink:([0-9]+)(?:\}|\%7D)", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
			foreach (Match tag in tags)
				if (tag.Groups.Count > 0)
				{
					var id = tag.Groups[1].Value; //.Remove(tag.Groups[1].Value.Length - 1, 1);
					var newLink = urlProvider.GetUrl(int.Parse(id));
					text = text.Replace(tag.Value, "href=\"" + newLink);
				}
		}

		// static compiled regex for faster performance
		private readonly static Regex ResolveUrlPattern = new Regex("(=[\"\']?)(\\W?\\~(?:.(?![\"\']?\\s+(?:\\S+)=|[>\"\']))+.)[\"\']?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

	    /// <summary>
	    /// The RegEx matches any HTML attribute values that start with a tilde (~), those that match are passed to ResolveUrl to replace the tilde with the application path.
	    /// </summary>
	    /// <param name="text"></param>
	    /// <param name="preview"></param>
	    /// <returns></returns>
	    /// <remarks>
	    /// When used with a Virtual-Directory set-up, this would resolve all URLs correctly.
	    /// The recommendation is that the "ResolveUrlsFromTextString" option (in umbracoSettings.config) is set to false for non-Virtual-Directory installs.
	    /// </remarks>
	    public static string ResolveUrlsFromTextString(string text)
		{
		    if (UmbracoSettings.ResolveUrlsFromTextString == false) return text;

		    using (var timer = DisposableTimer.DebugDuration(typeof(IOHelper), "ResolveUrlsFromTextString starting", "ResolveUrlsFromTextString complete"))
		    {
		        // find all relative urls (ie. urls that contain ~)
		        var tags = ResolveUrlPattern.Matches(text);
		        LogHelper.Debug(typeof(IOHelper), "After regex: " + timer.Stopwatch.ElapsedMilliseconds + " matched: " + tags.Count);
		        foreach (Match tag in tags)
		        {
		            var url = "";
		            if (tag.Groups[1].Success)
		                url = tag.Groups[1].Value;

		            // The richtext editor inserts a slash in front of the url. That's why we need this little fix
		            //                if (url.StartsWith("/"))
		            //                    text = text.Replace(url, ResolveUrl(url.Substring(1)));
		            //                else
		            if (String.IsNullOrEmpty(url) == false)
		            {
		                var resolvedUrl = (url.Substring(0, 1) == "/") ? IOHelper.ResolveUrl(url.Substring(1)) : IOHelper.ResolveUrl(url);
		                text = text.Replace(url, resolvedUrl);
		            }
		        }
		    }

		    return text;
		}

	}
}
