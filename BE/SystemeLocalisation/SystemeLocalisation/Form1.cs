using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SystemeLocalisation
{
    public partial class Form1 : Form
    {
        MqttClient mqttClient;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mqttClient = new MqttClient("test.mosquitto.org");
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            string clientId = Guid.NewGuid().ToString();
            mqttClient.Connect(clientId);
            mqttClient.Subscribe(new string[] { "tags" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            if (mqttClient.IsConnected)
            {
                Console.WriteLine("nique zebi");
                label1.Text = "nique zebi";
            }
            else
            {
                label1.Text = "nique pas zebi";
            }


        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received
            var message = Encoding.UTF8.GetString(e.Message);
            //var todo = JsonConvert.DeserializeObject<JObject>(message);
            //Console.WriteLine(todo);
            //Console.WriteLine(todo.GetType());
            //Console.WriteLine(message);
            List<Result> result = JsonConvert.DeserializeObject<List<Result>>(message);
            foreach (var item in result)
            {
                Console.WriteLine("-----------------------");
                Console.WriteLine(item.tagId);
                Console.WriteLine(item.timestamp);
                Console.WriteLine(item.data.coordinates.x);
                Console.WriteLine(item.data.coordinates.y);
                Console.WriteLine("-----------------------");
            }
        }


        public class Coordinates
        {
            public int x { get; set; }
            public int y { get; set; }
        }


        public class Data
        {
            public Coordinates coordinates;
        }
        public class Result
        {
            public Data data { get; set; }
            public int tagId { get; set; }
            public float timestamp { get; set; }
        }
    }

}
