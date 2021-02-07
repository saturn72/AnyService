namespace AnyService.EntityFramework
{
    public sealed class EfRepositoryConfig
    {
        public bool CaseSensitiveOrderBy { get; set; }
        /// <summary>
        /// Gets or sets value for insertion batch size. Default is 100
        /// </summary>
        public int InsertBatchSize { get; set; } = 100;
        public int UpdateBatchSize { get; set; } = 100;
    }
}
