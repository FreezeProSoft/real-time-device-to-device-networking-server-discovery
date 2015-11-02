using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;

namespace NetworkCommunication.Core
{   
    public static class SerializationHelper
    {
        static SerializationHelper()
        {
            binder = new CurrentAssemblyDeserializationBinder();
        }
        
        public static byte[] ToByteArray(object item)
        {
            try
            {
                var formatter = new BinaryFormatter();

                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, item);

                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                return null;
            }          
        }

        public static T ToObject<T>(byte[] bytes) where T: class
        {  
            try
            {
                var formatter = new BinaryFormatter();

                formatter.Binder = binder;

                using (var stream = new MemoryStream(bytes, 0, bytes.Length))
                {
                    return (T)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }  

        private static CurrentAssemblyDeserializationBinder binder;
    }

    public class CurrentAssemblyDeserializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(string.Format("{0}, {1}", typeName, System.Reflection.Assembly.GetExecutingAssembly().FullName));
        }
    }
}
