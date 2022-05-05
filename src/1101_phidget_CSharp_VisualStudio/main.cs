using System;
using System.Text;
using Phidget22;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ConsoleApplication
{
	class Program
	{
		static MqttClient client;

		private static void VoltageRatioInput0_VoltageRatioChange(object sender, Phidget22.Events.VoltageRatioInputVoltageRatioChangeEventArgs e)
		{
			var dist = 4.8 / (e.VoltageRatio - 0.02);
			Console.WriteLine("Distance: " + dist + " cm");
			client.Publish("test", Encoding.UTF8.GetBytes("Dist: " + dist));
		}

		static void Main(string[] args)
		{
			string brokerAddress = "localhost";
			client = new MqttClient(brokerAddress);
			var clientId = Guid.NewGuid().ToString();
			client.Connect(clientId, "test", "test");

			VoltageRatioInput voltageRatioInput0 = new VoltageRatioInput();

			voltageRatioInput0.VoltageRatioChange += VoltageRatioInput0_VoltageRatioChange;

			voltageRatioInput0.Open(5000);

			//Wait until Enter has been pressed before exiting
			Console.ReadLine();

			voltageRatioInput0.Close();
		}
	}
}
