using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;


namespace Hoard.Data
{
    [BsonIgnoreExtraElements]
    public class Project : DocumentBase
    {
        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; }
    }
}
