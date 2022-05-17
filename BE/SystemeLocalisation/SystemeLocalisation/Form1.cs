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
        public class BaliseData
        {
            public int x;
            public int y;
            public decimal timestamp;
            public int tagId;
            public decimal SpeedX;
            public decimal SpeedY;
            public bool isGet;
            public bool isLost;

            public BaliseData()
            {
                x = 0;
                y = 0;
                timestamp = 0;
                tagId = 0;
                SpeedX = 0;
                SpeedY = 0;
                isGet = false;
                isLost = false;
            }
        }

        public class GpsData
        {
            public int x;
            public int y;
            public decimal timestamp;
            public int tagId;

            public GpsData()
            {
                x = 0;
                y = 0;
                timestamp = 0;
                tagId = 0;
            }

        }

        GpsData gpsData = new GpsData();

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
            public decimal timestamp { get; set; }
        }

        Dictionary<int, BaliseData> stockage = new Dictionary<int, BaliseData>();
        Dictionary<int, BaliseData> stockage_archive = new Dictionary<int, BaliseData>();

        int mainId = 26884;
        int secondId = 26921;
        int thirdId = 26963;

        int CurrentTagId = 0;
        int NbColisGet = 0;
        int firstToGet = 0;

        Graphics graphics;

        MqttClient mqttClient;
        public Form1()
        {
            WindowState = FormWindowState.Maximized;
            InitializeComponent();

            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = new Bitmap(1250, 650);
            graphics = Graphics.FromImage(pictureBox1.Image);
            mqttClient = new MqttClient("172.30.4.44");
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            string clientId = Guid.NewGuid().ToString();
            mqttClient.Connect(clientId);
            mqttClient.Subscribe(new string[] { "tags" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            if (mqttClient.IsConnected)
            {
                Console.WriteLine("Connected ! ");
            }
            else
            {
                Console.WriteLine("Not Connected !");
            }
        }

        //private void affichage_struct(Dictionary<int, BaliseData> d)
        //{
        //    foreach (KeyValuePair<int, BaliseData> kvp in d)
        //    {
        //        Console.WriteLine("Key {0}\nTagId : {1} \nX : {2}\nY : {3}\nTimeStamp : {4} \nSpeedX : {5} \nSpeedY : {6} ",
        //            kvp.Key, kvp.Value.tagId, kvp.Value.x, kvp.Value.y, kvp.Value.timestamp, kvp.Value.SpeedX, kvp.Value.SpeedY);
        //    }
        //}

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                // handle message received
                var message = Encoding.UTF8.GetString(e.Message);
                List<Result> result = JsonConvert.DeserializeObject<List<Result>>(message);

                foreach (var item in result)
                {
                    if (item.tagId == mainId)
                    {
                        if(gpsData.x == 0)
                        {
                            //Premire fois qu'on voit cet id tu maintag
                            NewDataGps(gpsData, item.data.coordinates.x, item.data.coordinates.y, item.timestamp, item.tagId);
                            CurrentTagId = item.tagId;
                        }
                        else
                        {
                            //Console.WriteLine("Ensuite ici on a modifie juste les valeur de la balise gps");
                            NewDataGps(gpsData, item.data.coordinates.x, item.data.coordinates.y, item.timestamp, item.tagId);
                            CurrentTagId = item.tagId;
                        }
                    }
                    else if(item.tagId == secondId ||item.tagId == thirdId)
                    {
                        if (stockage.ContainsKey(item.tagId))
                        {
                            //Console.WriteLine("2eme fois qu'on recoit des data sur ce capteur");
                            stockage[item.tagId].tagId = stockage_archive[item.tagId].tagId;
                            stockage[item.tagId].isGet = stockage_archive[item.tagId].isGet;
                            stockage[item.tagId].isLost = stockage_archive[item.tagId].isLost;
                            stockage[item.tagId].x = item.data.coordinates.x;
                            stockage[item.tagId].y = item.data.coordinates.y;
                            stockage[item.tagId].timestamp = item.timestamp;
                            decimal soustractionx = Math.Abs(stockage[item.tagId].x - stockage_archive[item.tagId].x);
                            decimal soustractiony = Math.Abs(stockage[item.tagId].y - stockage_archive[item.tagId].y);
                            decimal TimeStamp_Colis = Math.Abs(stockage[item.tagId].timestamp - stockage_archive[item.tagId].timestamp);
                            decimal speedx = (soustractionx / 1000) / TimeStamp_Colis;
                            decimal speedy = (soustractiony / 1000) / TimeStamp_Colis;
                            //Console.WriteLine("Valeur de la vitesse apres le calcul en x {0} et y {1} dans le stockage : ",speedx, speedy);
                            stockage[item.tagId].SpeedX = speedx;
                            stockage[item.tagId].SpeedY = speedy;
                            CurrentTagId = item.tagId;
                            //Console.WriteLine("Vitesse en x {0}  Vitesse en y {1} dans l'archive pour voir si on a bien mis les valeurs", stockage[item.tagId].SpeedX, stockage[item.tagId].SpeedY);
                            PackageIsCollected(item.tagId);
                            PackageIsDropped(item.tagId);
                            bool lost = ColisLost(stockage);
                        }
                        else
                        {
                            BaliseData ajout = new BaliseData();
                            BaliseData ajout_archive = new BaliseData();
                            ajout.tagId = item.tagId;
                            ajout.x = item.data.coordinates.x;
                            ajout.y = item.data.coordinates.y;
                            ajout.timestamp = item.timestamp;
                            ajout.SpeedX = 0;
                            ajout.SpeedY = 0;
                            ajout.isGet = false;
                            ajout.isLost = false;

                            ajout_archive.tagId = item.tagId;
                            ajout_archive.x = item.data.coordinates.x;
                            ajout_archive.y = item.data.coordinates.y;
                            ajout_archive.timestamp = item.timestamp;
                            ajout_archive.SpeedX = 0;
                            ajout_archive.SpeedY = 0;
                            ajout_archive.isGet = false;
                            ajout_archive.isLost = false;
                            stockage.Add(item.tagId, ajout);
                            stockage_archive.Add(item.tagId, ajout_archive);
                            CurrentTagId = item.tagId;
                        }
                    }
                }
            } catch(Exception ex)
            {

            }
        }

        private void PackageIsCollected(int tagId)
        {
            decimal TimeStamp_Colis = stockage[tagId].timestamp - stockage_archive[tagId].timestamp;
            //Si capteur se deplace en x ou en y plus que la marge d'erreur alors le capteur de deplace
            if(Math.Abs(stockage_archive[tagId].x - stockage[tagId].x)> 50 ||
                Math.Abs(stockage_archive[tagId].y - stockage[tagId].y) > 50)
            {
                //Si la vitesse de deplacement est > 0.35m/s c'est pas un outlier mais bel est bien qmq qui deplace le colis
                if((float)stockage[tagId].SpeedX > 3 || (float)stockage[tagId].SpeedY > 3)
                {
                    //Si le colis se trouve à moins de 40 cm de moi alors c'est moi qui l'est recup
                    if(Math.Abs(gpsData.x - stockage[tagId].x) < 400 &&
                        Math.Abs(gpsData.y - stockage[tagId].y) < 400 && 
                        TimeStamp_Colis < 2 && stockage[tagId].isGet == false)
                    {
                        stockage[tagId].isGet = true;
                        stockage[tagId].isLost = false;
                        Console.WriteLine("---------------------------------------------------------------------------------");
                        Console.WriteLine("Le colis : {0} a été recup par vous !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", tagId);
                        Console.WriteLine("---------------------------------------------------------------------------------");

                    }
                }
            }
            ArchiveStockage(stockage,stockage_archive,tagId);
        }

        private void PackageIsDropped(int tagId)
        {
            //Si la distance entre moi et un colis est > 100 cm 
            if(Math.Abs(gpsData.x - stockage[tagId].x) > 1000 ||
                Math.Abs(gpsData.y - stockage[tagId].y) > 1000)
            {
                if(stockage[tagId].isGet == true)
                {
                    stockage[tagId].isGet = false;
                    stockage[tagId].isLost = true;

                    if(stockage[tagId].isLost==true)
                    {
                        Console.WriteLine("DEMI TOUR COLIS PERDU");
                        MessageBox.Show("T'as fait tomber un colis !!! fais demi tour");
                        ArchiveStockage(stockage, stockage_archive, tagId);
                        NbColisGet--;
                    }
                }
            }
        }

        private void TimerEvent(object sender, EventArgs e)
        {
            //faire une boucle pour draw le nb de colis dispo dans stockage
            //pas oublier d'effacer les coord precedente avant de redessiner les new

            //egalement un if si gpsData pas vide draw le gps

            int nbget = Nb_colis_get(stockage);
            if(nbget == 0)
            {
                label1.Text = "Nombre de colis récupérés : 0";
            } else if(nbget == 1) {
                label1.Text = "Nombre de colis récupérés : 1";
            } else if( nbget == 2)
            {
                label1.Text = "Nombre de colis récupérés : 2";
            }

            if(stockage.Count > 1)
            {
                firstToGet = OrdreRecuperation(gpsData, stockage);
            }
            Console.WriteLine(firstToGet);

            if(gpsData.x != 0 && gpsData.y != 0 && CurrentTagId != 0)
            {
                //Console.WriteLine("On est ici");
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(imageList.Images[2], ConvertXGps(gpsData,mainId), ConvertYGps(gpsData,mainId), 30, 30);
                if(stockage.Count > 1)
                {
                    if(firstToGet == secondId)
                    {
                        int MonX = ConvertXBalise(stockage, secondId);
                        int MonY = ConvertYBalise(stockage, secondId);
                        int MonXBis = ConvertXBalise(stockage, thirdId);
                        int MonYBis = ConvertYBalise(stockage, thirdId);
                        graphics.DrawImage(imageList.Images[0], MonX, MonY, 30, 30);
                        graphics.DrawImage(imageList.Images[1], MonXBis, MonYBis, 30, 30);
                    }
                    else if(firstToGet == thirdId)
                    {
                        int MonX = ConvertXBalise(stockage, secondId);
                        int MonY = ConvertYBalise(stockage, secondId);
                        int MonXBis = ConvertXBalise(stockage, thirdId);
                        int MonYBis = ConvertYBalise(stockage, thirdId);
                        graphics.DrawImage(imageList.Images[1], MonX, MonY, 30, 30);
                        graphics.DrawImage(imageList.Images[0], MonXBis, MonYBis, 30, 30);
                    }
                }
                else if (stockage.Count == 1)
                {
                    int MonX = ConvertXBalise(stockage, stockage.ElementAt(0).Key);
                    int MonY = ConvertYBalise(stockage, stockage.ElementAt(0).Key);
                    graphics.DrawImage(imageList.Images[0], MonX, MonY, 30, 30);

                }
                pictureBox1.Refresh();
            }
        }

        private int OrdreRecuperation(GpsData gpsData, Dictionary<int, BaliseData> stockage)
        {
            //Trouver formule math pour voir quel point est le plus proche de moi en x et y et retourner un tableau avec l'ordre par tagid
            if (stockage.Count == 1)
            {
                return stockage.ElementAt(0).Value.tagId;
            }else if(stockage.Count == 2) { 
                double distance1 = Math.Sqrt((Math.Pow(gpsData.x - stockage.ElementAt(0).Value.x, 2) + Math.Pow(gpsData.y - stockage.ElementAt(0).Value.y, 2)));
                double distance2 = Math.Sqrt((Math.Pow(gpsData.x - stockage.ElementAt(1).Value.x, 2) + Math.Pow(gpsData.y - stockage.ElementAt(1).Value.y, 2)));
                if(distance1 < distance2)
                {
                    return stockage.ElementAt(0).Key;
                }
                else
                {
                    return stockage.ElementAt(1).Key;
                }
            }
            return 0;
        }

        private int ConvertXGps(GpsData gpsData, int tagId)
        {
            int AverageRealLifeX = 26894;
            int CoeffX = AverageRealLifeX / 1250;
            int res = gpsData.x / CoeffX;
            return res ;
        }


        private int ConvertYGps(GpsData gpsData, int tagId)
        {
            int AverageRealLifeY = 7524;
            int CoeffY = AverageRealLifeY / 650;
            int res = gpsData.y / CoeffY;
            return res;
        }

        private int ConvertXBalise(Dictionary<int, BaliseData> stockage, int tagId)
        {
            //280 + 26614
            int AverageRealLifeX = 26894;
            int CoeffX = AverageRealLifeX / 1250;
            int res = stockage[tagId].x / CoeffX;
            return res;
        }

        private int ConvertYBalise(Dictionary<int, BaliseData> stockage, int tagId)
        {
            //280 + 26614
            int AverageRealLifeY = 7524;
            int CoeffY = AverageRealLifeY / 650;
            int res = stockage[tagId].y / CoeffY;
            return res;
        }

        private int Nb_colis_get(Dictionary<int, BaliseData> stockage)
        {
            int res = 0;
            for (int i = 0; i < stockage.Count; i++)
            {
                if (stockage.ElementAt(i).Value.isGet == true)
                {
                    res++;
                }
            }

            return res;
        }

        private bool ColisLost(Dictionary<int, BaliseData> stockage)
        {
            for(int i = 0; i < stockage.Count; i++)
            {
                if(stockage.ElementAt(i).Value.isLost == true)
                {
                    return true;
                }
            }
            return false;
        }

        private void NewDataGps(GpsData gpsData, int CoordX, int CoordY, decimal TimeStamp, int TagId)
        {
            gpsData.x = CoordX;
            gpsData.y = CoordY;
            gpsData.timestamp = TimeStamp; ;
            gpsData.tagId = TagId;
        }

        public void ArchiveStockage(Dictionary<int, BaliseData> stockage, Dictionary<int, BaliseData> stockageArchive, int TAG)
        {
            stockageArchive[TAG].tagId = stockage[TAG].tagId;
            stockageArchive[TAG].timestamp = stockage[TAG].timestamp;
            stockageArchive[TAG].x = stockage[TAG].x;
            stockageArchive[TAG].y = stockage[TAG].y;
            stockageArchive[TAG].isGet = stockage[TAG].isGet;
            stockageArchive[TAG].isLost = stockage[TAG].isLost;
        }

    }
}