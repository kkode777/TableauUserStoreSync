using System.Xml.Serialization;

namespace TableauRestApiLib.Models
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3761.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tableau.com/api", TypeName = "tsRequest")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tableau.com/api", IsNullable = false, ElementName = "tsRequest")]
    public partial class TsRequest
    {
        [XmlElement(ElementName = "credentials")]
        public TableauCredentialsType Credentials { get; set; }

        [XmlElement(ElementName = "group")]
        public GroupType Group { get; set; }

        [XmlElement(ElementName = "user")]
        public UserType User { get; set; }
    }
}
