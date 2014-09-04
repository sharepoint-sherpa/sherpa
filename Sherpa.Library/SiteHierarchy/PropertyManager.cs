using System;
using System.Collections.Generic;
using Microsoft.SharePoint.Client;

namespace Sherpa.Library.SiteHierarchy
{
    public class PropertyManager
    {
        public void SetProperties(ClientContext context, Web webToConfigure, Dictionary<string, string> properties)
        {
            foreach (KeyValuePair<string, string> property in properties)
            {
                SetProperty(context, webToConfigure, property);
            }
        }

        public void SetProperty(ClientContext context, Web webToConfigure, KeyValuePair<string, string> property)
        {
            var webProperties = webToConfigure.AllProperties;
            webProperties[property.Key] = property.Value;

            webToConfigure.Update();
            context.ExecuteQuery();
        }

        /// <summary>
        /// Just a handy debug-method not used
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webToConfigure"></param>
        public void PrintProperties(ClientContext context, Web webToConfigure)
        {
            var webProperties = webToConfigure.AllProperties;
            context.Load(webProperties);
            context.ExecuteQuery();

            foreach (KeyValuePair<string, object> propertyValue in webProperties.FieldValues)
            {
                Console.WriteLine("Property " + propertyValue.Key + ": " + propertyValue.Value);
            }
        }
    }
}
