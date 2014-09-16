using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using Sherpa.Library.Taxonomy.Model;

namespace Sherpa.Library.Taxonomy
{
    public class TaxonomyManager
    {
        private readonly ShTermGroup _termGroup;

        public TaxonomyManager(){}
        public TaxonomyManager(ShTermGroup termGroup)
        {
            _termGroup = termGroup;
        }

        public void WriteTaxonomyToTermStore(ClientContext context)
        {
            ValidateConfiguration(_termGroup);
            TermStore termStore = GetTermStore(context);

            if (!IsCurrentUserTermStoreAdministrator(context, termStore))
            {
                throw new Exception("Couldn't verify admin access. You must be a term store administrator to perform this operation");
            }

            var termGroup = termStore.Groups.ToList().FirstOrDefault(g => g.Id == _termGroup.Id) ??
                            termStore.CreateGroup(_termGroup.Title, _termGroup.Id);
                
            context.Load(termGroup, x => x.TermSets);
            context.ExecuteQuery();

            foreach (var termSet in _termGroup.TermSets)
            {
                var spTermSet = termStore.GetTermSet(termSet.Id);
                context.Load(spTermSet, x => x.Terms);
                context.ExecuteQuery();
                if (spTermSet.ServerObjectIsNull != null && spTermSet.ServerObjectIsNull.Value)
                {
                    spTermSet = termGroup.CreateTermSet(termSet.Title, termSet.Id, termStore.DefaultLanguage);
                    if (!string.IsNullOrEmpty(termSet.CustomSortOrder))
                        spTermSet.CustomSortOrder = termSet.CustomSortOrder;
                    context.Load(spTermSet, x => x.Terms);
                    context.ExecuteQuery();
                }

                foreach (var term in termSet.Terms)
                {
                    CreateTerm(context, termStore, term, spTermSet);
                }
            }
        }

        private void CreateTerm(ClientContext context, TermStore termStore, ShTerm shTerm, TermSetItem parentTerm)
        {
            var spTerm = termStore.GetTerm(shTerm.Id);
            context.Load(spTerm, t => t.Terms);
            context.ExecuteQuery();
            if (spTerm.ServerObjectIsNull != null && spTerm.ServerObjectIsNull.Value)
            {
                spTerm = parentTerm.CreateTerm(shTerm.Title, termStore.DefaultLanguage, shTerm.Id);
                if (!string.IsNullOrEmpty(shTerm.CustomSortOrder)) spTerm.CustomSortOrder = shTerm.CustomSortOrder;
                spTerm.IsAvailableForTagging = !shTerm.NotAvailableForTagging;
                context.Load(spTerm);
                context.ExecuteQuery();
            }
            foreach (ShTerm childTerm in shTerm.Terms)
            {
                CreateTerm(context, termStore, childTerm, spTerm);
            }
        }

        public ShTermGroup ExportTaxonomyGroupToConfig(ClientContext context, string groupName)
        {
            TermStore termStore = GetTermStore(context);

            var spTermGroup = termStore.Groups.ToList().FirstOrDefault(g => g.Name == groupName);
            context.Load(spTermGroup, x => x.TermSets);
            context.ExecuteQuery();

            if (spTermGroup == null)
            {
                Console.WriteLine("Couldn't find a taxonomy group with the name " + groupName);
                return null;
            }
            var shTermGroup = new ShTermGroup(spTermGroup.Id, spTermGroup.Name);

            foreach (var spTermSet in spTermGroup.TermSets)
            {
                var shTermSet = new ShTermSet(spTermSet.Id, spTermSet.Name);
                AddTermsToConfig(context, spTermSet, shTermSet);
                shTermGroup.TermSets.Add(shTermSet);
            }
            return shTermGroup;
        }

        private void AddTermsToConfig(ClientContext context, TermSetItem spTerm, ShTermSetItem termItem)
        {
            context.Load(spTerm, t => t.Terms);
            context.ExecuteQuery();

            foreach (var spChildTerm in spTerm.Terms)
            {
                var shTerm = new ShTerm(spChildTerm.Id, spChildTerm.Name);
                AddTermsToConfig(context, spChildTerm, shTerm);
                termItem.Terms.Add(shTerm);
            }
        }

        /// <summary>
        /// Since the administrator members like TermStore.DoesUserHavePermissions aren't available in the client API, this is currently how we check if user has permissions
        /// </summary>
        /// <param name="context"></param>
        /// <param name="termStore"></param>
        /// <returns></returns>
        private bool IsCurrentUserTermStoreAdministrator(ClientContext context, TermStore termStore)
        {
            const string testGroupName = "SherpaTemporaryTestGroup";
            var testGroupGuid = new Guid("0972a735-b89a-400f-a858-b80e29492b62");
            try
            {
                var termGroup = termStore.CreateGroup(testGroupName, testGroupGuid);
                context.ExecuteQuery();

                termGroup.DeleteObject();
                context.ExecuteQuery();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public TermStore GetTermStore(ClientContext context)
        {
            TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(context);
            taxonomySession.UpdateCache();
            context.Load(taxonomySession);
            context.ExecuteQuery();

            TermStore termStore = taxonomySession.GetDefaultSiteCollectionTermStore();
            context.Load(termStore, x => x.Groups, x => x.Id, x=> x.DefaultLanguage);
            context.ExecuteQuery();

            return termStore;
        }

        public Guid GetTermStoreId(ClientContext context)
        {
            return GetTermStore(context).Id;
        }

        /// <summary>
        /// Checks that no ID's in the configuration file contains duplicate Ids. 
        /// Also checks that no terms at the same level have duplicate names.
        /// Both scenarios will lead to problems with 'rogue' terms and term sets
        /// </summary>
        /// <param name="shTermGroup"></param>
        /// <returns></returns>
        public void ValidateConfiguration(ShTermGroup shTermGroup)
        {
            if (shTermGroup == null) throw new ArgumentNullException("shTermGroup");

            var termIdsForEnsuringUniqueness = new List<Guid> {shTermGroup.Id};
            foreach (var termSet in shTermGroup.TermSets)
            {
                if (termIdsForEnsuringUniqueness.Contains(termSet.Id)) 
                    throw new NotSupportedException("One or more term items has the same Id which is not supported. Termset Id " + termSet.Id);
                
                termIdsForEnsuringUniqueness.Add(termSet.Id);
                foreach (var term in termSet.Terms)
                {
                    if (termIdsForEnsuringUniqueness.Contains(term.Id))
                        throw new NotSupportedException("One or more term items has the same Id which is not supported. Term Id " + term.Id);

                    termIdsForEnsuringUniqueness.Add(term.Id);
                    // Terms at the same level cannot have duplicate names
                    string currentTermTitle = term.Title;
                    if (termSet.Terms.Count(t => t.Title.Equals(currentTermTitle)) != 1) 
                        throw new NotSupportedException("Found duplicate term names at the same level which is not supported. Term name " + term.Title);
                }
            }
        }
    }
}
