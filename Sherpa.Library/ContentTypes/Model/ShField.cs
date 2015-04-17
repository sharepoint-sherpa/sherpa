using System;
using System.Text;

namespace Sherpa.Library.ContentTypes.Model
{
    public class ShField
    {
        public Guid ID { get; set; }
        public string InternalName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public string Type { get; set; }
        public string[] Choices { get; set; }
        public bool FillInChoice { get; set; }
        public ShCalculatedProps CalculatedProps { get; set; }
        public string Format { get; set; }
        public string Default { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public string ShowField { get; set; }

        public string List { get; set; }

        public int? NumLines { get; set; }
        public bool RichText { get; set; }
        public string RichTextMode { get; set; }
        
        public bool Required { get; set; }
        public bool Hidden { get; set; }
        public bool ShowInNewForm { get; set; }
        public bool ShowInEditForm { get; set; }
        public bool ShowInDisplayForm { get; set; }

        public Guid SspId { get; set; }
        public Guid TermSetId { get; set; }
        public string TermSetName { get; set; }
        public bool OpenTermSet { get; set; }

        // E.g. PeopleOnly
        public string UserSelectionMode { get; set; }

        public ShField()
        {
            ShowInDisplayForm = true;
            ShowInEditForm = true;
            ShowInNewForm = true;
        }
        public string GetFieldAsXml()
        {
            return GetFieldAsXml(false);
        }

        public string GetFieldAsXml(bool required)
        {
            switch (Type)
            {
                case ("TaxonomyFieldType"):
                {
                    return GetFieldXml(required, "ShowField=\"Term1033\" Indexed=\"TRUE\"");
                }
                case ("TaxonomyFieldTypeMulti"):
                {
                    return GetFieldXml(required, "ShowField=\"Term1033\" Mult=\"TRUE\"");
                }
                case ("Choice"):
                {
                    return GetFieldXml(required, "FillInChoice=\"" + FillInChoice.ToString().ToUpper() + "\"", GetChoiceFieldXmlContent());
                }
                case ("MultiChoice"):
                {
                    return GetFieldXml(required, "FillInChoice=\"" + FillInChoice.ToString().ToUpper() + "\"", GetChoiceFieldXmlContent());
                }
                case ("Calculated"):
                {
                    return GetFieldXml(required, string.Format("ResultType=\"{0}\"", CalculatedProps.ResultType), GetCalculatedFieldXmlContent());
                }
                case("Number"):
                {
                    var options = (Min != null ? "Min=\"" + Min +"\"" : "") + (Max != null ? " Max=\"" + Max +"\"" : string.Empty);
                    return GetFieldXml(required, options);
                }
                case("HTML"):
                {
                    const string additionalProps = "RichText=\"TRUE\" RichTextMode=\"FullHtml\" UnlimitedLengthInDocumentLibrary=\"TRUE\"";
                    return GetFieldXml(false, additionalProps);
                }
                case ("Lookup"):
                {
                    string additionalProps = String.Format("List=\"{0}\" ShowField=\"{1}\" UnlimitedLengthInDocumentLibrary=\"FALSE\"", List, ShowField);
                    return GetFieldXml(false, additionalProps);
                }
                case ("LookupMulti"):
                {
                    string additionalProps = String.Format("List=\"{0}\" ShowField=\"{1}\" UnlimitedLengthInDocumentLibrary=\"FALSE\"", List, ShowField);
                    return GetFieldXml(false, additionalProps);
                }
                case ("Note"):
                {
                    var options = NumLines != null && NumLines != 0 ? "NumLines=\"" + NumLines + "\"" : string.Empty;
                    options += RichText ? " RichText=\"TRUE\"" : string.Empty;
                    options += !string.IsNullOrEmpty(RichTextMode) ? String.Format(" RichTextMode=\"{0}\"", RichTextMode) : string.Empty;
                    return GetFieldXml(required, options);
                }
                case("UserMulti"):
                {
                    var options = string.Empty;
                    options += GetXmlProperty("UserSelectionMode", UserSelectionMode);
                    options += GetXmlProperty("Mult", "TRUE");
                    return GetFieldXml(required, options);
                }
                default:
                {
                    return GetFieldXml(required, string.Empty);
                }
            }
        }

        private string GetXmlProperty(string name, string value)
        {
            return !string.IsNullOrEmpty(value) ? name+"=\""+value+"\" " : string.Empty+" ";
        }

        private string GetCalculatedFieldXmlContent()
        {
            var c = new StringBuilder();
            c.Append("<Formula>").Append(CalculatedProps.Formula).Append("</Formula>");
            c.Append("<FieldRefs>");
            foreach (var fieldRef in CalculatedProps.FieldRefs)
            {
                c.AppendFormat("<FieldRef Name=\"{0}\" ID=\"{{{1}}}\" />", fieldRef.Name, fieldRef.ID);
            }
            c.Append("</FieldRefs>");
            return c.ToString();

        }

        private string GetChoiceFieldXmlContent()
        {
            var content =
                new StringBuilder(!string.IsNullOrEmpty(Default)
                    ? string.Format("<Default>{0}</Default>", Default)
                    : string.Empty);
            if (Choices != null && Choices.Length > 0)
            {
                content.AppendLine("<CHOICES>");
                foreach (var choice in Choices)
                {
                    content.AppendFormat("<CHOICE>{0}</CHOICE>", choice);
                }
                content.AppendLine("</CHOICES>");
            }
            return content.ToString();
        }

        private string GetFieldXml(bool required, string additionalProperties)
        {
            return GetFieldXml(required, additionalProperties, string.Empty);
        }

        private string GetFieldXml(bool required, string additionalProperties, string fieldContent)
        {
            var format = !string.IsNullOrEmpty(Format) ? " Format=\"" + Format + "\"" : string.Empty;
            var showField = !string.IsNullOrEmpty(ShowField) ? " ShowField=\"" + ShowField + "\"" : string.Empty;
            var openTermSet = OpenTermSet ? " CreateValuesInEditForm=\"TRUE\" Open=\"TRUE\"" : string.Empty;

            var fieldXml = new StringBuilder();
            fieldXml.AppendFormat(
                "<Field ID=\"{0}\" Name=\"{1}\" DisplayName=\"{2}\" Type=\"{3}\" Hidden=\"{4}\" " +
                "Group=\"{5}\" Description=\"{6}\" Required=\"{7}\" " +
                "ShowInNewForm=\"{8}\" ShowInEditForm=\"{9}\" ShowInDisplayForm=\"{10}\"" +
                "{11}{12}{13} {14}>",
                ID.ToString("B"), InternalName.Trim(), DisplayName.Trim(), Type, Hidden,
                Group, Description, required.ToString().ToUpper(),
                ShowInNewForm.ToString().ToUpper(), ShowInEditForm.ToString().ToUpper(), ShowInDisplayForm.ToString().ToUpper(),
                format, showField, openTermSet, additionalProperties);

            if (!string.IsNullOrEmpty(Default))
            {
                fieldXml.AppendFormat("<Default>{0}</Default>", Default);
            }
            if (!string.IsNullOrEmpty(fieldContent))
            {
                fieldXml.Append(fieldContent);
            }
            fieldXml.Append("</Field>");

            return fieldXml.ToString();
        }

        public override string ToString()
        {
            return GetFieldAsXml();
        }
    }
}
