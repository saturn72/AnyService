using System.Collections.Generic;
using AnyService.Services.FileStorage;

namespace AnyService.SampleApp.Models
{
    public class MultipartSampleModel : IFileContainer
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public IEnumerable<FileModel> Files { get; set; }
    }
}