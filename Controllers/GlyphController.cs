using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Drawing.Text;
using System.Drawing;

using TheArtOfDev.HtmlRenderer.WinForms;

namespace DynamicGlyphGenerator.Controllers
{
    public class GlyphController : Controller
    {
        public static bool FontsInstantiated = false;

        public ActionResult FontAwesome(bool generateFiles = false)
        {
            // Grab the CSS path (to be read in using the appropriate Provider within Visual Studio)
            var path = Server.MapPath("~/Content/css/font-awesome.css");
            // Grab all CSS classes from the style sheet
            var css = System.IO.File.ReadAllText(path);
            // Read only the CSS Classes (only those with :before and beginning with .fa-)
            var classes = Regex.Matches(css, @"\.[-]?([_a-zA-Z][_a-zA-Z0-9-]*):before|[^\0-\177]*\\[0-9a-f]{1,6}(\r\n[ \n\r\t\f])?|\\[^\n\r\f0-9a-f]*")
                               .Cast<Match>()
                               .Where(m => m.Value.StartsWith(".fa-"))
                               .Distinct();

            // Create a dictionary that maps class names to their respective unicode values
            Dictionary<string, string> mappings = new Dictionary<string, string>();

            // Using those classes, grab the content value that appears within each of them, which will map the unicode values to the appropriate
            // class
            foreach(var cssClass in classes)
            {
                // Find the cooresponding code for each class
                var unicode = Regex.Match(css, cssClass + "[^\"]*\"(.*)\"[^}]*}");
                if (unicode != null && unicode.Groups.Count > 1)
                {
                    // Grab the match and store it
                    mappings.Add(cssClass.Value, String.Format("&#x{0};",unicode.Groups[1].Value.TrimStart('\\')));
                }
            }

            // Save all of the glyphs as content
            SaveAllGlyphs(mappings);
            
            // View the mappings
            return View(mappings);
        }

        // Method to render a single glyph based on it's unicode value
        //[ValidateInput(false)]
        //public ActionResult RenderFontAwesomeGlyph(string glyph)
        //{
        //    // Ensure the available font familes are ready
        //    if (!FontsInstantiated) { InstantiateFonts(); }

        //    // Generate the HTML to render
        //    using (var glyphStream = new MemoryStream())
        //    {
        //        // Generate the glyph (16x16, transparent, PNG) using the specific font that was instantiated 
        //        // for the renderer
        //        var html = String.Format("<span style='font-family: FontAwesome; display:block;'>{0}</span>", glyph);
        //        HtmlRender.RenderToImageGdiPlus(html, new Size(16, 16)).Save(glyphStream, System.Drawing.Imaging.ImageFormat.Png);
        //        using (var sr = new StreamReader(glyphStream))
        //        {
        //            return File(sr.ReadToEnd(), "image/png");
        //        }
        //    }
        //}

        private void InstantiateFonts()
        {
            // Build a collection of fonts
            var fonts = new PrivateFontCollection();

            // Get the appropriate fonts
            foreach (var fontFamily in Directory.GetFiles(Server.MapPath("~/Content/fonts"), "*.ttf"))
            {
                fonts.AddFontFile(fontFamily);
            }

            // Add each of the true type fonts to this renderer
            foreach (var trueTypeFont in fonts.Families)
            {
                HtmlRender.AddFontFamily(trueTypeFont);
            }
            FontsInstantiated = true;
        }

        private void SaveAllGlyphs(Dictionary<string,string> glyphMappings)
        {
            // Ensure the available font familes are ready
            if (!FontsInstantiated) { InstantiateFonts(); }

            // Ensure we have something to work with
            if (glyphMappings != null && glyphMappings.Any())
            {
                // Designate a Location to save the Glyphs in
                var glyphsFolder = Server.MapPath("~/Glyphs");

                // Generate each Glyph (based on it's mapping)
                foreach(var glyph in glyphMappings)
                {
                    // Construct the path and file name for the file
                    var glyphPath = Path.Combine(glyphsFolder, String.Format("{0}.png", glyph.Key.Replace(":before","").TrimStart('.')));

                    using (var glyphStream = System.IO.File.Create(glyphPath))
                    {
                        // Generate the glyph (16x16, transparent, PNG) using the specific font that was instantiated 
                        // for the renderer
                        var html = String.Format("<span style='font-family: FontAwesome;display:block;font: normal normal normal 14px/1 FontAwesome;text-align: center;'>{0}</span>", glyph.Value);
                        HtmlRender.RenderToImageGdiPlus(html, new Size(16, 16)).Save(glyphStream, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }
    }
}