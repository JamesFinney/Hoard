using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;


namespace Hoard.Data
{
    [BsonIgnoreExtraElements]
    public class Entry : DocumentBase
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

        [BsonElement("imageContent")]
        public string ImageContent { get; set; }

        [BsonElement("imageFormat")]
        public string ImageFormat { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }

        [BsonElement("tags")]
        public List<string> Tags { get; set; }

        [BsonIgnoreIfNull]
        public double? Score { get; set; }


        public Entry()
        {
            Tags = new List<string>();
        }
    }
}
