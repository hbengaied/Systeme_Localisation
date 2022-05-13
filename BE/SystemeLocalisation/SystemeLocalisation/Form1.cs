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
        GpsData gpsDataArchive = new GpsData();

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

        int CurrentTagId;
        int DrawCoordX;
        int DrawCoordY;
        int DrawCoordArchiveX;
        int DrawCoordArchiveY;

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
            mqttClient = new MqttClient("test.mosquitto.org");
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

        //private BaliseData archive_data(BaliseData nouveau)
        //{
        //    return nouveau;
        //}

        private void affichage_struct(Dictionary<int, BaliseData> d)
        {
            foreach (KeyValuePair<int, BaliseData> kvp in d)
            {
                Console.WriteLine("Key {0}\nTagId : {1} \nX : {2}\nY : {3}\nTimeStamp : {4} \nSpeedX : {5} \nSpeedY : {6} ",
                    kvp.Key, kvp.Value.tagId, kvp.Value.x, kvp.Value.y, kvp.Value.timestamp, kvp.Value.SpeedX, kvp.Value.SpeedY);
            }
        }

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
                            Console.WriteLine(item.data.coordinates.y);
                            gpsData.tagId = item.tagId;
                            gpsData.x = item.data.coordinates.x;
                            gpsData.y = item.data.coordinates.y;
                            gpsData.timestamp = item.timestamp;

                            gpsDataArchive.tagId = item.tagId;
                            gpsDataArchive.x = item.data.coordinates.x;
                            gpsDataArchive.y = item.data.coordinates.y;
                            gpsDataArchive.timestamp = item.timestamp;

                            CurrentTagId = item.tagId;
                            ConvertX(stockage, gpsData, item.tagId);
                            ConvertY(stockage, gpsData, item.tagId);
                            //Console.WriteLine("X a dessiner {0}", DrawCoordX);
                            //Console.WriteLine("Y a dessiner {0}", DrawCoordY);
                            Console.WriteLine("Affichage Stockgps Premiere fois");
                            Console.WriteLine(gpsData.tagId);
                            Console.WriteLine(gpsData.x);
                            Console.WriteLine(gpsData.y);
                            Console.WriteLine("---------------------------------");
                            EraseArchiveX(stockage_archive, gpsDataArchive, item.tagId);
                            EraseArchiveY(stockage_archive, gpsDataArchive, item.tagId);

                        }
                        else
                        {
                            //Console.WriteLine("Ensuite ici on a modifie juste les valeur de la balise gps");
                            gpsData.tagId = item.tagId;
                            gpsData.x = item.data.coordinates.x;
                            gpsData.y = item.data.coordinates.y;
                            gpsData.timestamp = item.timestamp;
                            CurrentTagId = item.tagId;
                            //affichage_struct(stockage);
                            ConvertX(stockage,gpsData,item.tagId);
                            ConvertY(stockage,gpsData,item.tagId);
                            EraseArchiveX(stockage_archive, gpsDataArchive, item.tagId);
                            EraseArchiveY(stockage_archive, gpsDataArchive, item.tagId);
                            //Console.WriteLine("X a dessiner {0}", DrawCoordX);
                            //Console.WriteLine("Y a dessiner {0}", DrawCoordY);
                            //Console.WriteLine("X a effacer {0}", DrawCoordArchiveX);
                            //Console.WriteLine("Y a effacer {0}", DrawCoordArchiveY);
                            Console.WriteLine("Affichage Stockgps");
                            Console.WriteLine(gpsData.tagId);
                            Console.WriteLine(gpsData.x);
                            Console.WriteLine(gpsData.y);
                            Console.WriteLine("Affichage gps archve");
                            Console.WriteLine(gpsDataArchive.tagId);
                            Console.WriteLine(gpsDataArchive.x);
                            Console.WriteLine(gpsDataArchive.y);
                            gpsDataArchive.tagId = item.tagId;
                            gpsDataArchive.x = item.data.coordinates.x;
                            gpsDataArchive.y = item.data.coordinates.y;
                            gpsDataArchive.timestamp = item.timestamp;
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
                            //Console.WriteLine("---------------------------------------------------------------------------------");
                            //Console.WriteLine("Affichage contenu stockage");
                            //affichage_struct(stockage);
                            //Console.WriteLine("\n");
                            //Console.WriteLine("Affichage contenu archive");
                            //Console.WriteLine("\n");
                            //affichage_struct(stockage_archive);
                            //Console.WriteLine("Numero du tag {0}", item.tagId);
                            decimal soustractionx = Math.Abs(stockage[item.tagId].x - stockage_archive[item.tagId].x);
                            decimal soustractiony = Math.Abs(stockage[item.tagId].y - stockage_archive[item.tagId].y);
                            decimal TimeStamp_Colis = Math.Abs(stockage[item.tagId].timestamp - stockage_archive[item.tagId].timestamp);
                            //Console.WriteLine("Valeur en x : {0}", soustractionx);
                            //Console.WriteLine("Valeur en y : {0}", soustractiony);
                            //Console.WriteLine("Valeur time : {0}", TimeStamp_Colis);
                            decimal speedx = (soustractionx / 1000) / TimeStamp_Colis;
                            decimal speedy = (soustractiony / 1000) / TimeStamp_Colis;
                            //Console.WriteLine("Valeur de la vitesse apres le calcul en x {0} et y {1} dans le stockage : ",speedx, speedy);
                            stockage[item.tagId].SpeedX = speedx;
                            stockage[item.tagId].SpeedY = speedy;
                            CurrentTagId = item.tagId;
                            ConvertX(stockage, gpsData, item.tagId);
                            ConvertY(stockage, gpsData, item.tagId);
                            EraseArchiveX(stockage_archive, gpsDataArchive, item.tagId);
                            EraseArchiveY(stockage_archive, gpsDataArchive, item.tagId);
                            //Console.WriteLine("Vitesse en x {0}  Vitesse en y {1} dans l'archive pour voir si on a bien mis les valeurs", stockage[item.tagId].SpeedX, stockage[item.tagId].SpeedY);
                            PackageIsCollected(item.tagId);
                            PackageIsDropped(item.tagId);
                        }
                        else
                        {
                            BaliseData ajout = new BaliseData();
                            BaliseData ajout_archive = new BaliseData();
                            //Console.WriteLine("First ici car au debut notre id existe pas");
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
                            ConvertX(stockage, gpsData, item.tagId);
                            ConvertY(stockage, gpsData, item.tagId);
                            EraseArchiveX(stockage_archive, gpsDataArchive, item.tagId);
                            EraseArchiveY(stockage_archive, gpsDataArchive, item.tagId);
                        }
                    }
                }
            } catch(Exception ex)
            {
                //Console.WriteLine("NIQUEEEEE ZEBIIIIIIIIIIIIIII");
                //Console.WriteLine(ex.Message);
            }
        }

        private void PackageIsCollected(int tagId)
        {
            decimal TimeStampGps = gpsData.timestamp - gpsDataArchive.timestamp;
            decimal TimeStamp_Colis = stockage[tagId].timestamp - stockage_archive[tagId].timestamp;
            //Si capteur se deplace en x ou en y plus que la marge d'erreur alors le capteur de deplace
            if(Math.Abs(stockage_archive[tagId].x - stockage[tagId].x)> 50 ||
                Math.Abs(stockage_archive[tagId].y - stockage[tagId].y) > 50)
            {
                //Si la vitesse de deplacement est > 0.35m/s c'est pas un outlier mais bel est bien qmq qui deplace le colis
                if((float)stockage[tagId].SpeedX > 3 || (float)stockage[tagId].SpeedY > 3)
                {
                    //Console.WriteLine("Mouvement !!!!!!!!!!");
                    //Si le colis se trouve à moins de 40 cm de moi alors c'est moi qui l'est recup
                    if(Math.Abs(gpsData.x - stockage[tagId].x) < 400 &&
                        Math.Abs(gpsData.y - stockage[tagId].y) < 400 && 
                        TimeStamp_Colis < 2 && TimeStampGps < 2 && stockage[tagId].isGet == false)
                    {
                        stockage[tagId].isGet = true;
                        stockage[tagId].isLost = false;
                        Console.WriteLine("---------------------------------------------------------------------------------");
                        Console.WriteLine("Le colis : {0} a été recup par vous !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!", tagId);
                        Console.WriteLine("---------------------------------------------------------------------------------");
                    }
                }
                //stockage_archive[tagId] = stockage[tagId];
            }
                stockage_archive[tagId].tagId = stockage[tagId].tagId;
                stockage_archive[tagId].timestamp = stockage[tagId].timestamp;
                stockage_archive[tagId].x = stockage[tagId].x;
                stockage_archive[tagId].y = stockage[tagId].y;
                stockage_archive[tagId].isGet = stockage[tagId].isGet;
                stockage_archive[tagId].isLost = stockage[tagId].isLost;
            //Je sais pas si la ligne d'en dessous est correct :  tester et remplacer par celle de haut dessus
            //stockage_archive = stockage;
            //gpsDataArchive = gpsData;
            gpsDataArchive.tagId = gpsData.tagId;
            gpsDataArchive.x = gpsData.x;
            gpsDataArchive.y = gpsData.y;
            gpsDataArchive.timestamp = gpsData.timestamp;
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
                        //stockage_archive[tagId] = stockage[tagId];
                        stockage_archive[tagId].tagId = stockage[tagId].tagId;
                        stockage_archive[tagId].timestamp = stockage[tagId].timestamp;
                        stockage_archive[tagId].x = stockage[tagId].x;
                        stockage_archive[tagId].y = stockage[tagId].y;
                        stockage_archive[tagId].isGet = stockage[tagId].isGet;
                        stockage_archive[tagId].isLost = stockage[tagId].isLost;
                        //Je sais pas si la ligne d'en dessous est correct :  tester et remplacer par celle de haut dessus
                        //stockage_archive = stockage;
                    }
                }
            }
        }

        private void TimerEvent(object sender, EventArgs e)
        {
            //faire une boucle pour draw le nb de colis dispo dans stockage
            //pas oublier d'effacer les coord precedente avant de redessiner les new

            //egalement un if si gpsData pas vide draw le gps
            if(CurrentTagId == mainId)
            {
                if(gpsData.x != 0 && gpsData.y != 0)
                {
                    //Console.WriteLine("On est ici");
                    graphics.FillRectangle(Brushes.Cyan, DrawCoordArchiveX, DrawCoordArchiveY, 30, 30);
                    graphics.FillRectangle(Brushes.Black, DrawCoordX, DrawCoordY, 30, 30);
                    pictureBox1.Refresh();

                }
            } else if ( CurrentTagId == secondId || CurrentTagId == thirdId)
            {
                if (stockage.ContainsKey(CurrentTagId))
                {
                    if(stockage[CurrentTagId].x != 0 && stockage[CurrentTagId].y != 0)
                    {
                        graphics.FillRectangle(Brushes.Cyan, DrawCoordArchiveX, DrawCoordArchiveY, 30, 30);
                        if (CurrentTagId == secondId)
                        {
                            graphics.FillRectangle(Brushes.Red, DrawCoordX, DrawCoordY, 30, 30);
                            pictureBox1.Refresh();
                        } else if(CurrentTagId == thirdId)
                        {
                            graphics.FillRectangle(Brushes.Yellow, DrawCoordX, DrawCoordY, 30, 30);
                            pictureBox1.Refresh();
                        }
                    }
                }
            }

        }
        private void OrdreRecuperation(GpsData gpsData, Dictionary<int, BaliseData> stockage)
        {
            //Trouver formule math pour voir quel point est le plus proche de moi en x et y et retourner un tableau avec l'ordre par tagid
        }

        private void ConvertX(Dictionary<int, BaliseData> stockage, GpsData gpsData, int tagId)
        {
            //280 + 26614
            int AverageRealLifeX = 26894;
            int CoeffX = AverageRealLifeX / 1250;
            if(tagId == mainId)
            {
                DrawCoordX = gpsData.x / CoeffX;
            }else if(tagId == secondId || tagId == thirdId)
            {
                DrawCoordX = stockage[tagId].x / CoeffX;
            }
        }

        private void ConvertY(Dictionary<int, BaliseData> stockage, GpsData gpsData, int tagId)
        {
            //510+7014
            int AverageRealLifeY = 7524;
            int CoeffY = AverageRealLifeY / 650;
            if (tagId == mainId)
            {
                DrawCoordY = gpsData.y / CoeffY;
            }
            else if (tagId == secondId || tagId == thirdId)
            {
                DrawCoordY = stockage[tagId].y / CoeffY;
            }
        }

        private void EraseArchiveX(Dictionary<int, BaliseData> stockage, GpsData gpsDataArchive, int tagId)
        {
            int AverageRealLifeX = 26894;
            int CoeffX = AverageRealLifeX / 1250;
            if (tagId == mainId)
            {
                DrawCoordArchiveX = gpsDataArchive.x / CoeffX;
            }
            else if (tagId == secondId || tagId == thirdId)
            {
                DrawCoordArchiveX = stockage_archive[tagId].x / CoeffX;
            }
        }

        private void EraseArchiveY(Dictionary<int, BaliseData> stockage_archive, GpsData gpsDataArchive, int tagId)
        {
            int AverageRealLifeY = 7524;
            int CoeffY = AverageRealLifeY / 650;
            if (tagId == mainId)
            {
                DrawCoordArchiveY = gpsDataArchive.y / CoeffY;
            }
            else if (tagId == secondId || tagId == thirdId)
            {
                DrawCoordArchiveY = stockage_archive[tagId].y / CoeffY;
            }
        }


    }
}