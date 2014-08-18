using Sherpa.Library.Taxonomy.Model;

namespace Sherpa.Library.Taxonomy
{
    public interface ITaxonomyManager
    {
        void WriteTaxonomyToTermStore();
        void ValidateConfiguration(GtTermSetGroup group);
    }
}
