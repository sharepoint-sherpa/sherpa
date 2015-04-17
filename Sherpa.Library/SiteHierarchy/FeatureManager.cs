using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.SiteHierarchy
{
    public class FeatureManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void ActivateSiteCollectionFeatures(ClientContext clientContext, List<ShFeature> siteFeatures, bool onlyContentTypeDependencyFeatures)
        {
            foreach (var featureActivation in siteFeatures)
            {
                if (onlyContentTypeDependencyFeatures && featureActivation.ContentTypeDependency)
                {
                    ActivateSiteCollectionFeature(clientContext, featureActivation);
                }
                else if (!onlyContentTypeDependencyFeatures)
                {
                    ActivateSiteCollectionFeature(clientContext, featureActivation);
                }
            }
        }

        public void ActivateSiteCollectionFeatures(ClientContext clientContext, List<ShFeature> siteFeatures)
        {
            ActivateSiteCollectionFeatures(clientContext, siteFeatures, false);
        }

        public void ActivateWebFeatures(ClientContext clientContext, Web webToConfigure, List<ShFeature> webFeatures, bool onlyContentTypeDependencyFeatures)
        {
            foreach (var featureActivation in webFeatures)
            {
                if (onlyContentTypeDependencyFeatures && featureActivation.ContentTypeDependency)
                {
                    ActivateWebFeature(clientContext, featureActivation, webToConfigure);
                }
                else if (!onlyContentTypeDependencyFeatures)
                {
                    ActivateWebFeature(clientContext, featureActivation, webToConfigure);
                }
            }
        }

        public void ActivateWebFeatures(ClientContext clientContext, Web webToConfigure, List<ShFeature> webFeatures)
        {
            ActivateWebFeatures(clientContext, webToConfigure, webFeatures, false);
        }

        private void ActivateWebFeature(ClientContext clientContext, ShFeature feature, Web web)
        {
            Log.DebugFormat("Attempting to activate web feature {0}", feature.FeatureName);
            var featureCollection = web.Features;
            if (feature.ReactivateAlways) DeActivateFeatureInCollection(clientContext, feature, featureCollection);
            ActivateFeatureInCollection(clientContext, feature, featureCollection, FeatureDefinitionScope.Site);
        }

        private void ActivateSiteCollectionFeature(ClientContext clientContext, ShFeature feature)
        {
            Log.DebugFormat("Attempting to activate site collection feature {0}", feature.FeatureName);
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
        private static void ActivateFeatureInCollection(ClientContext clientContext, ShFeature featureInfo, FeatureCollection featureCollection, FeatureDefinitionScope scope)
        {
            clientContext.Load(featureCollection);
            clientContext.ExecuteQuery();

            if (!DoesFeatureExistInCollection(featureCollection, featureInfo.FeatureId))
            {
                try
                {
                    Log.Info("Activating feature   " + featureInfo.FeatureName);
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

        private static void DeActivateFeatureInCollection(ClientContext clientContext, ShFeature featureInfo, FeatureCollection featureCollection)
        {
            clientContext.Load(featureCollection);
            clientContext.ExecuteQuery();

            if (DoesFeatureExistInCollection(featureCollection, featureInfo.FeatureId))
            {
                Log.Info("Deactivating feature " + featureInfo.FeatureName);
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
