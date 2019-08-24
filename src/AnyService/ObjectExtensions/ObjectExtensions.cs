using System.IO;
using System.Xml.Serialization;

namespace System
{
    public static class ObjectExtensionsFunctions
    {
        public static T GetPropertyValueByName<T>(this object obj, string propertyName)
        {
            var pi = obj.GetType().GetProperty(propertyName);
            if (pi != null)
                return (T)pi.GetValue(obj);
            throw new InvalidOperationException();
        }

        public static T Clone<T>(this T source) where T : class
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(T));

                serializer.Serialize(stream, source);
                stream.Position = 0;
                return (T)serializer.Deserialize(stream);
            }
        }
    }
}