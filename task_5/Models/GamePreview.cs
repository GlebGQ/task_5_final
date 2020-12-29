using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace task_5.Models
{
    public class GamePreview
    {
        public String GameId { get; set; }

        public String GameName { get; set; }

        public List<string> Tags { get; set; }

        public String CreatorName { get; set; }
    }
}
