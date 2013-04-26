using System.IO;
using RazorEngine;

namespace EmailSender
{
    public class EmailTemplateResolver
    {
        public static string GetEmailBody(string templatePath, dynamic model)
        {
            var template = File.ReadAllText(templatePath);
            var body = Razor.Parse(template, model);
            return body;
        }
    }
}