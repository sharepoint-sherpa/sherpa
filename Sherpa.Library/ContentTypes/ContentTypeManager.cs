using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SharePoint.Client;
using Sherpa.Library.ContentTypes.Model;

namespace Sherpa.Library.ContentTypes
{
    public class ContentTypeManager
    {
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
                if ( existingContentTypes.Any( item => item.Id.ToString().Equals(contentType.ID.ToString(CultureInfo.InvariantCulture)) ) )
                {
                    // We want to add fields even if the content type exists (?)
                    AddSiteColumnsToContentType(contentType);
                }
                else
                {
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
            Web web = ClientContext.Web;
            ContentTypeCollection contentTypes = web.ContentTypes;
            ClientContext.Load(contentTypes);
            ClientContext.ExecuteQuery();
            ContentType contentType = contentTypes.GetById(configContentType.ID);
            FieldCollection fields = web.Fields;
            ClientContext.Load(contentType);
            ClientContext.Load(fields);
            ClientContext.ExecuteQuery();

            foreach (var fieldName in configContentType.Fields)
            {
                // Need to load content type fields every iteration because fields are added to the collection
                Field webField = fields.GetByInternalNameOrTitle(fieldName);
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
                fieldLink.Hidden = configContentType.HiddenFields.Contains(fieldName);
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
