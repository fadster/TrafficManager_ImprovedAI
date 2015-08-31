using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;
using System.Threading;

namespace TrafficManager_ImprovedAI
{
    /// <summary>
    /// This class takes an object, and generates a key to it. There are several possibilities:
    /// This generator can generate keys of type integer,float,double. The generated key is not necessarly
    /// unique!
    /// </summary>
    public class MD5HashGenerator
    {
        private static readonly Object locker = new Object();

        /// <summary>
        /// Generates a hashed - key for an instance of a class.
        /// The hash is a classic MD5 hash (e.g. BF20EB8D2C4901112179BF5D242D996B). So you can distinguish different 
        /// instances of a class. Because the object is hashed on the internal state, you can also hash it, then send it to
        /// someone in a serialized way. Your client can then deserialize it and check if it is in
        /// the same state.
        /// The method just just estimates that the object implements the ISerializable interface. What's
        /// needed to save the state or so, is up to the implementer of the interface.
        /// <b>The method is thread-safe!</b>
        /// </summary>
        /// <param name="sourceObject">The object you'd like to have a key out of it.</param>
        /// <returns>An string representing a MD5 Hashkey corresponding to the object or null if the object couldn't be serialized.</returns>
        /// <exception cref="ApplicationException">Will be thrown if the key cannot be generated.</exception>
        public static String GenerateKey(Object sourceObject)
        {
            String hashString = "";

            //Catch unuseful parameter values
            if (sourceObject == null) {
                throw new ArgumentNullException("Null as parameter is not allowed");
            } else {
                //We determine if the passed object is really serializable.
                try {
                    //Now we begin to do the real work.
                    hashString = ComputeHash(ObjectToByteArray(sourceObject));
                    return hashString;
                } catch (AmbiguousMatchException ame) {
                    throw new ApplicationException("Could not definitly decide if object is serializable. Message:" + ame.Message);
                }
            }
        }

        /// <summary>
        /// Converts an object to an array of bytes. This array is used to hash the object.
        /// </summary>
        /// <param name="objectToSerialize">Just an object</param>
        /// <returns>A byte - array representation of the object.</returns>
        /// <exception cref="SerializationException">Is thrown if something went wrong during serialization.</exception>
        private static byte[] ObjectToByteArray(Object objectToSerialize)
        {
            MemoryStream fs = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            try {
                //Here's the core functionality! One Line!
                //To be thread-safe we lock the object
                lock (locker) {
                    formatter.Serialize(fs, objectToSerialize);
                }
                return fs.ToArray();
            } catch (SerializationException se) {
                UnityEngine.Debug.Log("Error occured during serialization. Message: " + se.Message);
                return null;
            } finally {
                fs.Close();
            }
        }

        /// <summary>
        /// Generates the hashcode of an given byte-array. The byte-array can be an object. Then the
        /// method "hashes" this object. The hash can then be used e.g. to identify the object.
        /// </summary>
        /// <param name="objectAsBytes">bytearray representation of an object.</param>
        /// <returns>The MD5 hash of the object as a string or null if it couldn't be generated.</returns>
        private static string ComputeHash(byte[] objectAsBytes)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            try {
                byte[] result = md5.ComputeHash(objectAsBytes);

                // Build the final string by converting each byte
                // into hex and appending it to a StringBuilder
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < result.Length; i++) {
                    sb.Append(result[i].ToString("X2"));
                }

                // And return it
                return sb.ToString();
            } catch (ArgumentNullException ane) {
                //If something occured during serialization, this method is called with an null argument. 
                UnityEngine.Debug.Log("Hash has not been generated. - " + ane);
                return null;
            }
        }
    }
}