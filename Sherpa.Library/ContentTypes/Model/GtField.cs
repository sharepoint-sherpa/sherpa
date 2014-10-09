using System;
using System.Text;

namespace Sherpa.Library.ContentTypes.Model
{
    public class GtField
    {
        public Guid ID { get; set; }
        public string InternalName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public string Type { get; set; }
        public string[] Choices { get; set; }
        public bool FillInChoice { get; set; }
        public GtCalculatedProps CalculatedProps { get; set; }
        public string Format { get; set; }
        public string Default { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public int? NumLines { get; set; }
        public string ShowField { get; set; }
        
        public bool Required { get; set; }
        public bool Hidden { get; set; }
        public bool ShowInNewForm { get; set; }
        public bool ShowInEditForm { get; set; }
        public bool ShowInDisplayForm { get; set; }

        public Guid SspId { get; set; }
        public Guid TermSetId { get; set; }
        public string TermSetName { get; set; }
        public bool OpenTermSet { get; set; }

        public GtField()
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
                    return GetSelfClosingFieldXml(required, "ShowField=\"Term1033\" Indexed=\"TRUE\"");
                }
                case ("TaxonomyFieldTypeMulti"):
                {
                    return GetSelfClosingFieldXml(required, "ShowField=\"Term1033\" Mult=\"TRUE\"");
                }
                case ("Choice"):
                {
                    return GetFieldWithContentXml(required, "FillInChoice=\"" + FillInChoice.ToString().ToUpper() + "\"", GetChoiceFieldXmlContent());
                }
                case ("MultiChoice"):
                {
                    return GetFieldWithContentXml(required, "FillInChoice=\"" + FillInChoice.ToString().ToUpper() + "\"", GetChoiceFieldXmlContent());
                }
                case ("Calculated"):
                {
                    return GetFieldWithContentXml(required, string.Format("ResultType=\"{0}\"",CalculatedProps.ResultType), GetCalculatedFieldXmlContent());
                }
                case("Number"):
                {
                    var options = (Min != null ? "Min=\"" + Min +"\"" : "") + (Max != null ? " Max=\"" + Max +"\"" : "");
                    return GetSelfClosingFieldXml(required, options);
                }
                case ("Note"):
                {
                    var options = NumLines != null && NumLines != 0 ? "NumLines=\"" + NumLines + "\"" : "";
                    return GetSelfClosingFieldXml(required, options);
                }

                default:
                {
                    return GetSelfClosingFieldXml(required, string.Empty);
                }
            }
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

        /// <summary>
        /// Not too happy with how this is done
        /// </summary>
        /// <param name="required"></param>
        /// <param name="additionalProperties"></param>
        /// <returns></returns>
        private string GetSelfClosingFieldXml(bool required, string additionalProperties)
        {
            var format = !string.IsNullOrEmpty(Format) ? " Format=\"" + Format + "\"" : string.Empty;
            var showField = !string.IsNullOrEmpty(ShowField) ? " ShowField=\"" + ShowField + "\"" : string.Empty;
            var openTermSet = OpenTermSet ? " CreateValuesInEditForm=\"TRUE\" Open=\"TRUE\"" : string.Empty;
            return String.Format(
                "<Field ID=\"{0}\" Name=\"{1}\" DisplayName=\"{2}\" Type=\"{3}\" Hidden=\"{4}\" " +
                "Group=\"{5}\" Description=\"{6}\" Required=\"{7}\" "+
                "ShowInNewForm=\"{8}\" ShowInEditForm=\"{9}\" ShowInDisplayForm=\"{10}\"" + 
                "{11}{12}{13} {14}/>",
                ID.ToString("B"), InternalName, DisplayName, Type, Hidden, 
                Group, Description, required.ToString().ToUpper(), 
                ShowInNewForm, ShowInEditForm, ShowInDisplayForm,
                format, showField, openTermSet, additionalProperties);
        }
        private string GetFieldWithContentXml(bool required, string additionalProperties, string fieldContent)
        {
            return GetSelfClosingFieldXml(required, additionalProperties)
                    .Replace("/>", String.Format(">{0}</Field>", fieldContent));
        }

        public override string ToString()
        {
            return GetFieldAsXml();
        }
    }
}
