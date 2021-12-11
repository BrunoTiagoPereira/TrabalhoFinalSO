using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.App.Helpers
{
    public class ValidationResult
    {
        public ValidationResult()
        {
            Errors = new List<ValidationError>();
        }
        public List<ValidationError> Errors { get; set; }

        public bool HasErrors => Errors.Count > 0;
    }

    public class ValidationError
    {
        public string Error { get; set; }
    }
}
