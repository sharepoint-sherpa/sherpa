using System.Collections.Generic;

namespace Sherpa.Library.ContentTypes.Model
{
    public sealed class ShCalculatedProps
    {
        public ShResultType ResultType { get; set; }
        public string Formula { get; set; }
        public List<ShFieldRefs> FieldRefs { get; set; }
    }
}