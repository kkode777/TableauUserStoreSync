using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TableauRestApiLib
{
    public static class XmlExtensions
    {
        public static string Serialize<T>(this T value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true
                };
                var nameSpaces = new XmlSerializerNamespaces();
                nameSpaces.Add("", "");
                var xmlserializer = new XmlSerializer(typeof(T));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter, settings))
                {
                    xmlserializer.Serialize(writer, value, nameSpaces);
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while serializing XML to {default(T).GetType()}. Exception details -- {ex.StackTrace}");
            }
        }

        public static T Deserialize<T>(string xmlString)
        {
            T returnObject = default(T);
            if (string.IsNullOrEmpty(xmlString)) return default(T);

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextReader reader = new StringReader(xmlString))
                {
                    returnObject = (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred", ex);
            }
            return returnObject;
        }
    }
}
