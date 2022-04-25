using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SubscriberService
{
    class Program
    {
        static MqttClient client;
        static readonly string PILL_TAG = "0107ee832e";
        static readonly string PIR_ID = "DIPS8/0x842e14fffe065bbc";
        static readonly TimeSpan startMorning = new TimeSpan(12, 0, 0); // Modified while debugging
        static readonly TimeSpan endMorning = new TimeSpan(15, 0, 0); // Modified while debugging
        private static readonly TimeSpan pillPlaceThreshold = new TimeSpan(0, 0, 20); // time to put pillbox back to its place
        static readonly int MIN_PILL_DURATION = 5; // Spend at least 5 seconds taking pills 
        static bool pillsTaken = false;
        static bool isAwake = false;
        static bool pillboxOff;
        static long pillsOffTime = -1;
        private static Timer timer;

        private static void breakfastTimer(TimeSpan breakfastTime) { 
            DateTime current = DateTime.Now;
            TimeSpan timeToGo = breakfastTime - current.TimeOfDay;
            if (timeToGo < TimeSpan.Zero)
            {
                return;//time already passed
            }
            timer = new Timer(x =>
            {
                breakfastReminder();
            }, null, timeToGo, Timeout.InfiniteTimeSpan);
        }

        private static void breakfastReminder()
        {
            TimeSpan currentTime = new DateTimeOffset(DateTime.Now).TimeOfDay;
            Console.WriteLine("Time is now "+ currentTime +". You can eat breakfast");
        }

        private static void SetPillboxReminder(long pillsOffTime)
        {
            var pillsOffDateTime = DateTime.FromBinary(pillsOffTime);
            do
            {
                var currentTime = DateTime.Now;
                var timeDifference = currentTime.Subtract(pillsOffDateTime);
                if (timeDifference <= pillPlaceThreshold) continue;
                Console.WriteLine("Remember to set your pill box back to where it belongs.");
                return;
            } while (pillboxOff);
        }


        private static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string received = Encoding.UTF8.GetString(e.Message);
            if(e.Topic == PIR_ID)
            {
                TimeSpan currentTime = new DateTimeOffset(DateTime.Now).TimeOfDay;
                if (currentTime > startMorning && currentTime < endMorning && !isAwake)
                {
                    //Console.WriteLine("Received PIR-data");
                    JObject jsondata = JObject.Parse(received);
                    JToken occupied = jsondata.GetValue("occupancy");
                    if (occupied.ToString() == "True")
                    {
                        Console.WriteLine("Good morning! Remember your pill");
                        isAwake = true;
                    }
                }
            }
            //Console.WriteLine("Received from broker: " + received);
            String[] receivedProps = received.Split(':');
            if ((receivedProps[0] == "Tag on" || receivedProps[0] == "Tag off") && !pillsTaken && isAwake)
            {
                String tag = receivedProps[1].Trim(' ');
                //Console.WriteLine("Tag: " + tag);
                if (receivedProps[0] == "Tag off" && tag == PILL_TAG)
                {
                    Console.WriteLine("Pills off");
                    pillsOffTime = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                    pillboxOff = true;
                    SetPillboxReminder(pillsOffTime);
                }
                if (receivedProps[0] == "Tag on" && tag == PILL_TAG)
                {
                    if(pillsOffTime != -1)
                    {
                        var endTime = new DateTimeOffset(DateTime.Now);
                        var timeSpent = endTime.ToUnixTimeSeconds() - pillsOffTime;
                        pillboxOff = false;
                        if(timeSpent >= MIN_PILL_DURATION) // Spend at least 5 seconds taking pills
                        {
                            pillsTaken = true;
                            //var breakfastTime = endTime.AddHours(1);
                            var breakfastTime = endTime.AddSeconds(10);
                            Console.WriteLine("Good job! You took the pills at: " + endTime.TimeOfDay);
                            Console.WriteLine("Remember to wait an hour before eating! \nI will remind you to eat breakfast at: " + breakfastTime.TimeOfDay);
                            breakfastTimer(breakfastTime.TimeOfDay);
                        }
                    }
                }

            }
        }

        static void Main(string[] args)
        {
            string brokerAddress = "test.mosquitto.org";
            client = new MqttClient(brokerAddress);
            var clientId = Guid.NewGuid().ToString();
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Connect(clientId, "test", "test");

            if (client.IsConnected)
            {
                client.Subscribe(new string[] { "DIPS8" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                client.Subscribe(new string[] { PIR_ID }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            //Console.WriteLine("Hello World!");
        }
    }
}
