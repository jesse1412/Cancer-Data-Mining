using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace WekaReader.Visualization
{
    public static class ReportGenerator
    {
        public static void GenerateReport(string OutputFilePath,
            int UserID,
            string[] Classifications,
            string[] VisualizationImagePaths = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html><html><head><style>div.a {text-align:center;}div.b {text-align:left;}</style></head><body>");
            sb.Append("<div class=\"a\">");
            sb.Append(@"<h1>CDSS Diagnostic Report</h1>");
            sb.Append(@"<h2>User: " + UserID + @"</h2>");
            sb.Append(@"<h2>" + DateTime.Now + "</h2>");
            sb.Append(@"</div>");
            sb.Append("<div class=\"b\">");
            if (Classifications.Length > 0)
            {
                sb.Append(@"<h2>Classifications:</h2>");
                foreach (string s in Classifications)
                {
                    sb.Append("<p>" + s + "</p>");
                }
            }
            if (VisualizationImagePaths.Length > 0)
            {
                sb.Append(@"<h2>Visualizations:</h2>");
                foreach (string s in VisualizationImagePaths)
                {
                    sb.Append("<img src=\"" + s + "\" alt=\"" + Path.GetFileName(s) + "\">");
                }
            }
            sb.Append(@"</div></body></html>");
            using (StreamWriter w = new StreamWriter(OutputFilePath))
            {
                w.Write(sb);
            }
        }
    }
}
