using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library.ContentTypes.Model;

namespace Sherpa.Library.ContentTypes
{
    public class ContentTypeManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ClientContext ClientContext { get; set; }
        private List<ShContentType> ContentTypes { get; set; }

        /// <summary>
        /// For creating fields and content types
        /// </summary>
        public ContentTypeManager(ClientContext clientContext, List<ShContentType> contentTypes)
        {
            ContentTypes = contentTypes;
            ValidateConfiguration(ContentTypes);
            ClientContext = clientContext;
        }

        public void CreateContentTypes()
        {
            Web web = ClientContext.Web;
            ContentTypeCollection existingContentTypes = web.ContentTypes;
            ClientContext.Load(existingContentTypes);
            ClientContext.ExecuteQuery();

            foreach (ShContentType contentType in ContentTypes)
            {
                if ( existingContentTypes.Any(item => item.Id.ToString().Equals(contentType.ID.ToString(CultureInfo.InvariantCulture)) ) )
                {
                    // We want to add fields even if the content type exists
                    AddSiteColumnsToContentType(contentType);
                }
                else
                {
                    Log.Debug("Creating content type " + contentType.DisplayName);
                    var contentTypeCreationInformation = contentType.GetContentTypeCreationInformation();
                    var newContentType = existingContentTypes.Add(contentTypeCreationInformation);
                    ClientContext.ExecuteQuery();

                    // Update display name (internal name will not be changed)
                    newContentType.Name = contentType.DisplayName;
                    newContentType.Update(true);
                    ClientContext.ExecuteQuery();

                    AddSiteColumnsToContentType(contentType);
                }
            }
        }

        private void AddSiteColumnsToContentType(ShContentType configContentType)
        {
            Log.Debug("Attempting to add fields to content type " + configContentType.DisplayName);

            Web web = ClientContext.Web;
            ContentTypeCollection contentTypes = web.ContentTypes;
            ClientContext.Load(contentTypes);
            ClientContext.ExecuteQuery();
            ContentType contentType = contentTypes.GetById(configContentType.ID);
            FieldCollection webFields = web.Fields;
            ClientContext.Load(contentType);
            ClientContext.Load(webFields);
            ClientContext.ExecuteQuery();

            foreach (var fieldName in configContentType.Fields)
            {
                // Need to load content type fields every iteration because fields are added to the collection
                Field webField = webFields.GetByInternalNameOrTitle(fieldName);
                FieldLinkCollection contentTypeFields = contentType.FieldLinks;
                ClientContext.Load(contentTypeFields);
                ClientContext.Load(webField);
                ClientContext.ExecuteQuery();

                var fieldLink = contentTypeFields.FirstOrDefault(existingFieldName => existingFieldName.Name == fieldName);
                if (fieldLink == null)
                {
                    var link = new FieldLinkCreationInformation { Field = webField };
                    fieldLink = contentType.FieldLinks.Add(link);
                }

                fieldLink.Required = configContentType.RequiredFields.Contains(fieldName);
                if (configContentType.HiddenFields.Contains(fieldName))
                {
                    fieldLink.Hidden = true;
                    fieldLink.Required = false;
                }
                contentType.Update(true);
                ClientContext.ExecuteQuery();
            }
        }


        public void DeleteAllCustomContentTypes()
        {
            Web web = ClientContext.Web;
            ContentTypeCollection existingContentTypes = web.ContentTypes;
            ClientContext.Load(existingContentTypes);
            ClientContext.ExecuteQuery();

            var contentTypeGroups = new List<string>();
            foreach (ShContentType contentType in ContentTypes.Where(contentType => !contentTypeGroups.Contains(contentType.Group)))
            {
                contentTypeGroups.Add(contentType.Group);
            }
            List<ContentType> contentTypes = existingContentTypes.ToList().OrderBy(ct => ct.Id.ToString()).Where(ct => contentTypeGroups.Contains(ct.Group)).ToList();

            for (int i = contentTypes.Count - 1; i >= 0; i--)
            {
                contentTypes[i].DeleteObject();
                try
                {
                    ClientContext.ExecuteQuery();
                }
                catch
                {
                    Console.WriteLine("Could not delete content type '" + contentTypes[i].Name + "'");
                }
            }
        }

        public void ValidateConfiguration(List<ShContentType> contentTypes)
        {
            Log.Debug("Trying to validate content type configuration");
            var contentTypeIdsForEnsuringUniqueness = new List<string>();
            var contentTypeInternalNamesForEnsuringUniqueness = new List<string>();
            foreach (var contentType in contentTypes)
            {
                if (contentTypeIdsForEnsuringUniqueness.Contains(contentType.ID))
                    throw new NotSupportedException("One or more content types have the same Id which is not supported. Content Type Id " + contentType.ID);
                if (contentTypeInternalNamesForEnsuringUniqueness.Contains(contentType.InternalName))
                    throw new NotSupportedException("One or more content types have the same InternalName which is not supported. Content Type Id " + contentType.InternalName);

                contentTypeIdsForEnsuringUniqueness.Add(contentType.ID);
                contentTypeInternalNamesForEnsuringUniqueness.Add(contentType.InternalName);
            }
        }
    }
}
