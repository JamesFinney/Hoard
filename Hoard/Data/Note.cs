using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;


namespace Hoard.Data
{
    [BsonIgnoreExtraElements]
    public class Note : DocumentBase
    {
        [BsonElement("projectId")]
        public string ProjectId { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("created")]
        public DateTime Created { get; set; }

        [BsonElement("modified")]
        public DateTime Modified { get; set; }

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; }

        [BsonElement("textContent")]
        public string TextContent { get; set; }

        [BsonIgnoreIfNull]
        public double? Score { get; set; }
    }
}
