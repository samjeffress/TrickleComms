using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SmsWeb.Models
{
    /// <summary>
    /// From http://www.prideparrot.com/blog/archive/2012/8/uploading_and_returning_files 
    /// </summary>
    public class FileTypesAttribute : ValidationAttribute
    {
        private readonly List<string> _types;

        public FileTypesAttribute(string types)
        {
            _types = types.Split(',').ToList();
        }

        public override bool IsValid(object value)
        {
            if (value == null) 
                return true;


            var fileExt = string.Empty;
            if (value is HttpPostedFile)
                fileExt = System.IO.Path.GetExtension((value as HttpPostedFile).FileName).Substring(1);
            if (value  is HttpPostedFileWrapper)
                fileExt = System.IO.Path.GetExtension((value as HttpPostedFileWrapper).FileName).Substring(1);
            return _types.Contains(fileExt, StringComparer.OrdinalIgnoreCase);
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format("Invalid file type. Only the following types {0} are supported.", String.Join(", ", _types));
        }
    }
}