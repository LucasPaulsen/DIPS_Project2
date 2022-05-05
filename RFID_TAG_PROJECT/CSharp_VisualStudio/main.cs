using System;
using System.Text;
using Phidget22;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ConsoleApplication
{
	class Program
	{
		public static MqttClient client { get; private set; }
		public static String topic = "DIPS8/pills";


		private static void Rfid0_Tag(object sender, Phidget22.Events.RFIDTagEventArgs e)
		{
			Console.WriteLine("Tag: " + e.Tag);
			Console.WriteLine("Protocol: " + e.Protocol);
			Console.WriteLine("----------");
			client.Publish(topic, Encoding.UTF8.GetBytes(("Tag on: " + e.Tag)));

		}

		private static void Rfid0_TagLost(object sender, Phidget22.Events.RFIDTagLostEventArgs e)
		{
			Console.WriteLine("Tag lost: " + e.Tag);
			Console.WriteLine("Protocol: " + e.Protocol);
			Console.WriteLine("----------");
			client.Publish(topic, Encoding.UTF8.GetBytes(("Tag off: " + e.Tag)));

		}

		static void Main(string[] args)
		{

			string BrokerAddress = "test.mosquitto.org";

			client = new MqttClient(BrokerAddress, 1883, false, null, null, MqttSslProtocols.None);
			var clientID = Guid.NewGuid().ToString();

			client.Connect(clientID, "test", "test");


			RFID rfid0 = new RFID();

			rfid0.Tag += Rfid0_Tag;
			rfid0.TagLost += Rfid0_TagLost;

			rfid0.Open(5000);

			//Wait until Enter has been pressed before exiting
			Console.ReadLine();

			rfid0.Close();
		}
	}
}
