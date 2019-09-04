using System.Collections.Generic;
using AnyService.Services;

namespace AnyService.SampleApp.Models
{
    public class FormModel : IFileContainer
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public IEnumerable<FileModel> Files { get; set; }
    }
}