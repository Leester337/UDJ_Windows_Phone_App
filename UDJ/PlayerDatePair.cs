using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace UDJ
{
    [DataContract]
    public class PlayerDatePair
    {
        [IgnoreDataMember]
        public Player playerOfPair { get; set; }
        [DataMember]
        public DateTime dateOfSignIn { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public byte[] playerSerialized { get; set; }

        public PlayerDatePair(Player currPlayer, DateTime currDate)
        {
            playerOfPair = currPlayer;
            dateOfSignIn = currDate;
            name = playerOfPair.name;
        }


        public static void Serialize(Stream streamObject, object objForSerialization)
        {
            if (objForSerialization == null || streamObject == null)
                return;

            DataContractJsonSerializer ser = new DataContractJsonSerializer(objForSerialization.GetType());
            ser.WriteObject(streamObject, objForSerialization);
        }

        public static object Deserialize(Stream streamObject, Type serializedObjectType)
        {
            if (serializedObjectType == null || streamObject == null)
                return null;

            DataContractJsonSerializer ser = new DataContractJsonSerializer(serializedObjectType);
            return ser.ReadObject(streamObject);
        }
    }
}
