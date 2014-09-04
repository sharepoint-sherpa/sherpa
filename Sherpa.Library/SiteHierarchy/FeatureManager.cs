using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.SiteHierarchy
{
    public class FeatureManager
    {
        public void ActivateFeatures(ClientContext clientContext, Web webToConfigure, List<GtFeature> siteFeatures, List<GtFeature> webFeatures, bool onlyContentTypeDependencyFeatures)
        {
            if (siteFeatures != null)
            {
                foreach (var featureActivation in siteFeatures.Where(f => f.ContentTypeDependency == onlyContentTypeDependencyFeatures))
                {
                    ActivateSiteCollectionFeature(clientContext, featureActivation);
                }
            }
            if (webFeatures != null)
            {
                foreach (var featureActivation in webFeatures.Where(f => f.ContentTypeDependency == onlyContentTypeDependencyFeatures))
                {
                    ActivateWebFeature(clientContext, featureActivation, webToConfigure);
                }
            }
        }

        public void ActivateFeatures(ClientContext clientContext, Web webToConfigure, List<GtFeature> siteFeatures, List<GtFeature> webFeatures)
        {
            ActivateFeatures(clientContext, webToConfigure, siteFeatures, webFeatures, false);
        }

        private void ActivateWebFeature(ClientContext clientContext, GtFeature feature, Web web)
        {
            var featureCollection = web.Features;
            if (feature.ReactivateAlways) DeActivateFeatureInCollection(clientContext, feature, featureCollection);
            ActivateFeatureInCollection(clientContext, feature, featureCollection, FeatureDefinitionScope.Site);
        }

        private void ActivateSiteCollectionFeature(ClientContext clientContext, GtFeature feature)
        {
            var siteCollection = clientContext.Site;
            var featureCollection = siteCollection.Features;
            if (feature.ReactivateAlways) DeActivateFeatureInCollection(clientContext, feature, featureCollection);
            ActivateFeatureInCollection(clientContext, feature, featureCollection, FeatureDefinitionScope.Site);
        }


        /// <summary>
        /// FeaturedefinitionScope.None is the way to go OOTB features. http://stackoverflow.com/questions/17803291/failing-to-activate-a-feature-using-com-in-sharepoint-2010
        /// Both Site Collection and Web scoped custom features MUST use Scope.Site.
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="featureInfo"></param>
        /// <param name="featureCollection"></param>
        /// <param name="scope"></param>
        private static void ActivateFeatureInCollection(ClientContext clientContext, GtFeature featureInfo, FeatureCollection featureCollection, FeatureDefinitionScope scope)
        {
            clientContext.Load(featureCollection);
            clientContext.ExecuteQuery();

            if (!DoesFeatureExistInCollection(featureCollection, featureInfo.FeatureId))
            {
                try
                {
                    Console.WriteLine("Activating feature " + featureInfo.FeatureName);
                    featureCollection.Add(featureInfo.FeatureId, true, scope);
                    clientContext.ExecuteQuery();
                }
                catch (ServerException)
                {
                    // Out of the box features will bomb using other scopes than FeatureDefinitionScope.None
                    // This is why we first try with the correct scope, then fallback to Scope.None
                    featureCollection.Add(featureInfo.FeatureId, true, FeatureDefinitionScope.None);
                    clientContext.ExecuteQuery();
                }
            }
        }

        private static void DeActivateFeatureInCollection(ClientContext clientContext, GtFeature featureInfo, FeatureCollection featureCollection)
        {
            clientContext.Load(featureCollection);
            clientContext.ExecuteQuery();

            if (DoesFeatureExistInCollection(featureCollection, featureInfo.FeatureId))
            {
                Console.WriteLine("Deactivating feature " + featureInfo.FeatureName);
                featureCollection.Remove(featureInfo.FeatureId, true);
                clientContext.ExecuteQuery();
            }
        }

        private static bool DoesFeatureExistInCollection(FeatureCollection featureCollection, Guid featureId)
        {
            var featureEnumerator = featureCollection.GetEnumerator();
            while (featureEnumerator.MoveNext())
            {
                if (featureEnumerator.Current.DefinitionId.Equals(featureId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
