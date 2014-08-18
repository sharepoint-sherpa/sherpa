using System.Collections.Generic;

namespace Sherpa.Library.ContentTypes.Model
{
    public sealed class GtCalculatedProps
    {
        public GtResultType ResultType { get; set; }
        public string Formula { get; set; }
        public List<GtFieldRefs> FieldRefs { get; set; }
    }
}