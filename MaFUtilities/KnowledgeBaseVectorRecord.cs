using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Text;

namespace MaFUtilities
{
    public class KnowledgeBaseVectorRecord
    {
        [VectorStoreKey]
        public required Guid Id { get; set; }

        [VectorStoreData]
        public required string Question { get; set; }

        [VectorStoreData]
        public required string Answer { get; set; }

        [VectorStoreVector(1536)]
        public string Vector => $"Q: {Question} - A: {Answer}";
    }
}
