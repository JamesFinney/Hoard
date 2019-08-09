using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hoard.Data
{
    public abstract class DocumentBase
    {
        [BsonId]
        public string Id { get; set; }

        public DocumentBase()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
