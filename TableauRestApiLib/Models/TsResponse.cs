using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace TableauRestApiLib.Models
{

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3761.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tableau.com/api", TypeName = "tsResponse")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tableau.com/api", IsNullable = false, ElementName = "tsResponse")]
    public partial class TsResponse
    {
        [XmlElement(ElementName = "credentials")]
        public TableauCredentialsType Credentials { get; set; }

        [XmlElement(ElementName = "pagination")]
        public PaginationType PaginationType { get; set; }

        [XmlElement(ElementName = "error")]
        public ErrorType Error { get; set; }

        [XmlElement(ElementName = "group")]
        public GroupType Group { get; set; }

        [XmlElement(ElementName = "user")]
        public UserType User { get; set; }

        [XmlElement(ElementName = "users")]
        public UserListType UserList { get; set; }

        [XmlElement(ElementName = "groups")]
        public GroupListType GroupList { get; set; }
    }
}
