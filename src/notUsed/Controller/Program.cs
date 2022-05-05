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
        static readonly string PIR_BEDROOM = "DIPS8/0x842e14fffe065bbc";
        static readonly string PIR_KITCHEN = "DIPS8/0x842e14fffe47a4f3";
        static readonly string BEDROOM_SCREEN = "DIPS8/BEDROOM_SCREEN";
        static readonly string KITCHEN_SCREEN = "DIPS8/KITCHEN_SCREEN";
        static readonly TimeSpan startMorning = new TimeSpan(12, 0, 0); // Modified while debugging
        static readonly TimeSpan endMorning = new TimeSpan(17, 0, 0); // Modified while debugging
        private static readonly TimeSpan pillPlaceThreshold = new TimeSpan(0, 0, 20); // time to put pillbox back to its place
        static readonly int MIN_PILL_DURATION = 5; // Spend at least 5 seconds taking pills 
        static readonly int DEMO_TIME = 30; // Spend at least 5 seconds taking pills 
        static bool pillsTaken = false;
        static bool isAwake = false;
        static bool pillboxOff;
        static long pillsOffTime = -1;
        private static Timer timer;
        private static Timer pillsOffReminder;
        static DateTimeOffset breakfastTime;

        private static void breakfastTimer(TimeSpan breakfastTime)
        {
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
            var currentTimeString = currentTime.ToString().Substring(0, 5);
            Console.WriteLine("Time is now " + currentTimeString + ". You can eat breakfast");
            client.Publish(BEDROOM_SCREEN, Encoding.ASCII.GetBytes("BREAKFAST_TIME: " + currentTimeString));
            client.Publish(KITCHEN_SCREEN, Encoding.ASCII.GetBytes("BREAKFAST_TIME: " + currentTimeString));
        }

        private static void SetPillboxReminder(int timeOut)
        {
            pillsOffReminder = new Timer(x =>
            {
                if (pillboxOff)
                {
                    Console.WriteLine("Remember to set your pill box back to where it belongs.");
                    client.Publish(KITCHEN_SCREEN, Encoding.ASCII.GetBytes("PUT_PILLS_BACK"));
                    client.Publish(BEDROOM_SCREEN, Encoding.ASCII.GetBytes("PUT_PILLS_BACK"));
                    //SetPillboxReminder(60);
                    SetPillboxReminder(6);
                }
            }, null, timeOut * 1000, int.MaxValue);
        }


        private static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string received = Encoding.UTF8.GetString(e.Message);
            if (e.Topic == PIR_KITCHEN)
            {
                TimeSpan currentTime = new DateTimeOffset(DateTime.Now).TimeOfDay;
                if (pillsTaken && currentTime < breakfastTime.TimeOfDay)
                {
                    Console.WriteLine("Remember to wait before eating");
                    client.Publish(KITCHEN_SCREEN, Encoding.ASCII.GetBytes("WAIT: " + breakfastTime.TimeOfDay));
                }
            }
            if (e.Topic == PIR_BEDROOM)
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
                        client.Publish(BEDROOM_SCREEN, Encoding.ASCII.GetBytes("GOOD_MORNING"));
                        isAwake = true;
                    }
                }
            }
            //Console.WriteLine("Received from broker: " + received);
            String[] receivedProps = received.Split(':');
            if ((receivedProps[0] == "Tag on" || receivedProps[0] == "Tag off") && isAwake)
            {
                String tag = receivedProps[1].Trim(' ');
                //Console.WriteLine("Tag: " + tag);
                if (receivedProps[0] == "Tag off" && tag == PILL_TAG)
                {
                    Console.WriteLine("Pills off");
                    if (pillsTaken)
                    {
                        Console.WriteLine("DON'T YOU DARE TAKE MORE PILLS!");
                        client.Publish(KITCHEN_SCREEN, Encoding.ASCII.GetBytes("PILLS_TAKEN_AGAIN"));
                        client.Publish(BEDROOM_SCREEN, Encoding.ASCII.GetBytes("PILLS_TAKEN_AGAIN"));
                    }
                    else
                    {
                        pillsOffTime = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                    }
                    pillboxOff = true;
                    //SetPillboxReminder(60);
                    SetPillboxReminder(6);
                }
                if (receivedProps[0] == "Tag on" && tag == PILL_TAG)
                {
                    pillboxOff = false;
                    if (pillsOffTime != -1 && !pillsTaken)
                    {
                        var endTime = new DateTimeOffset(DateTime.Now);
                        var timeSpent = endTime.ToUnixTimeSeconds() - pillsOffTime;

                        if (timeSpent >= MIN_PILL_DURATION) // Spend at least 5 seconds taking pills
                        {
                            pillsTaken = true;
                            //var breakfastTime = endTime.AddHours(1);
                            breakfastTime = endTime.AddSeconds(DEMO_TIME);
                            string takenAt = endTime.TimeOfDay.ToString().Substring(0, 5);
                            string breakfastTimeString = breakfastTime.TimeOfDay.ToString().Substring(0, 5);
                            client.Publish(BEDROOM_SCREEN, Encoding.ASCII.GetBytes("PILLS_TAKEN: " + takenAt));
                            client.Publish(KITCHEN_SCREEN, Encoding.ASCII.GetBytes("PILLS_TAKEN: " + takenAt));
                            Console.WriteLine("Good job! You took the pills at: " + takenAt);
                            Console.WriteLine("Remember to wait an hour before eating! \nI will remind you to eat breakfast at: " + breakfastTimeString);
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
                client.Subscribe(new string[] { "DIPS8/pills" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                client.Subscribe(new string[] { PIR_BEDROOM }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                client.Subscribe(new string[] { PIR_KITCHEN }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            }
            //Console.WriteLine("Hello World!");
            //Console.WriteLine("BreakfastTime: " + breakfastTime.TimeOfDay);
        }
    }
}
