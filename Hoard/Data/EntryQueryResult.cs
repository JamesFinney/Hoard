using Hoard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hoard.Data
{
    public class Query
    {
        public string ProjectId { get; set; }
        public string UserId { get; set; }
        public string Search { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public string Sort { get; set; }
        public bool IsAscending { get; set; }

        public Query()
        {
            Skip = 0;
            Take = 25;
            Sort = "created";
            IsAscending = false;
        }
    }

    public class EntryQueryResult
    {
        public Query Query { get; set; }

        public long Total { get; set; }
        public long Returned { get; set; }
        public List<Entry> Results { get; set; }
    }


    public class NoteQueryResult
    {
        public Query Query { get; set; }

        public long Total { get; set; }
        public long Returned { get; set; }
        public List<Note> Results { get; set; }
    }
}
