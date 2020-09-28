using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using EasyModbus;
using System.IO.Ports;
using System.Threading;
using System.Timers;
using System.Text.RegularExpressions;

namespace WindowsFormsApp5
{
    

    public partial class Form1 : Form
    {
        SerialPort port;
        string lineReadIn;
        


        // this will prevent cross-threading between the serial port
        // received data thread & the display of that data on the central thread
        private delegate void preventCrossThreading(string x);
        private preventCrossThreading accessControlFromCentralThread;
        private static System.Timers.Timer aTimer;



        public Form1()
        {
            InitializeComponent();
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            SetTimer();




            // create and open the serial port (configured to my machine)
            // this is a Down-n-Dirty mechanism devoid of try-catch blocks and
            // other niceties associated with polite programming
            const string com = "COM5";
            port = new SerialPort(com, 9600, Parity.None, 8, StopBits.One);

            //   port.ErrorReceived += new SerialErrorReceivedEventHandler();
            try
            {
                port.Open();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Error: Port " + com + " jest zajęty");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uart exception: " + ex);
            }


 

            if (port.IsOpen)
            {
                // set the 'invoke' delegate and attach the 'receive-data' function
                // to the serial port 'receive' event.

                accessControlFromCentralThread = displayTextReadIn;
                port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
                
            }

        }
        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {
                aTimer.Stop();
                Thread.Sleep(100);
                port.Write("LOFF\r");
            }
        //    Thread.Sleep(10000);
            //port.Close();
            System.Windows.Forms.Application.Exit();
            //Application.
        }

        // Application.Exit();


        private void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(5);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
       // bool flaga; 

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (port.IsOpen)
            {
                port.Write("LON\r");

            }
        }

        string wydruk;
//        string[] result = new string[100];

        // this is called when the serial port has receive-data for us.
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs rcvdData)
        {

            while(port.BytesToRead > 0)
            {

                //   lineReadIn += port.ReadExisting();

                lineReadIn += port.ReadExisting();
                //   lineReadIn += Environment.NewLine;

                // lineReadIn += "\r\n";
                //   lineReadIn += lineReadIn;

           //     flaga = false;
           //     aTimer.Stop();

                Thread.Sleep(25);
            }

         //   flaga = true;


            // display what we've acquired.

               

            lineReadIn = Regex.Replace(lineReadIn, @"\s+", string.Empty);
            if (lineReadIn.Length > 8)
            lineReadIn = lineReadIn.Remove(8);
            if (wydruk != lineReadIn)
                tworzeniepliku(lineReadIn);
            else
                Thread.Sleep(500);

            displayTextReadIn(lineReadIn);

            wydruk = lineReadIn;
            lineReadIn = string.Empty;
            aTimer.Start();

        }// end function 'port_dataReceived'



        // this, hopefully, will prevent cross threading.
        private void displayTextReadIn(string ToBeDisplayed)          //wyswietlanie sygnalu na drugim texboxie
        {
            if (textBox1.InvokeRequired)
                textBox1.BeginInvoke(accessControlFromCentralThread, ToBeDisplayed);
            else
                textBox1.Text = ToBeDisplayed;
            
        }


        DateTime stop, start;
        //---------------------------------------------------------------------------------------------

        

        const int M_BRAK_POLACZENIA_Z_MES = 4;




        public int Test1(string SerialTxt)
        {
            using (MESwebservice.BoardsSoapClient wsMES = new MESwebservice.BoardsSoapClient("BoardsSoap"))
            {
                DataSet Result;
                try
                {
                    Result = wsMES.GetBoardHistoryDS(@"itron", SerialTxt);
                }
                catch
                {
                    return M_BRAK_POLACZENIA_Z_MES;
                }

                
            }
                return 1;
        }




        private int sprawdzeniekrok(string sn)
        {
            int Result;

            Result = Test1(sn); //przykladowy numer seryjny 9100000668
            switch (Result)
            {
                case M_BRAK_POLACZENIA_Z_MES:
                   // MessageBox.Show("Brak połączenia z MES.", "Info", MessageBoxButtons.OK);
                    break;

                default:
                   // MessageBox.Show("Wszystko jest OK", "Info", MessageBoxButtons.OK);                   
                    return 1;
            }
            MessageBox.Show("Brak połączenia z MES.", "Info", MessageBoxButtons.OK);
            return 0;
        }


        private int tworzeniepliku(string sn)
        {
            sn = Regex.Replace(sn, @"\s+", string.Empty);

            if(sn.Length > 8)
            sn = sn.Remove(8);

            if (sn=="ERROR")
            {
                return 0;
            }

           // textBox1.Text = sn;

            string sciezka = ("C:/tars/");      //definiowanieścieżki do której zapisywane logi
            stop = DateTime.Now;
            if (Directory.Exists(sciezka))       //sprawdzanie czy sciezka istnieje
            {
                ;
            }
            else
                System.IO.Directory.CreateDirectory(sciezka); //jeśli nie to ją tworzy

            try
            {
                using (StreamWriter sw = new StreamWriter("C:/tars/" + sn + "(" + stop.ToString("yyyy-MM-dd HH-mm-ss") + ")" + ".Tars"))
                {


                    sw.WriteLine("S{0}", sn);
                    sw.WriteLine("CITRON");
                    sw.WriteLine("BGAZPAR");
                    sw.WriteLine("NPLKWIM0T20B1P01");
                    sw.WriteLine("PHEAT_STAKE");
                    sw.WriteLine("s1");
                    sw.WriteLine("DTarsSoftwareDoc");
                    sw.WriteLine("RTarsSoftwareRevision");
                    sw.WriteLine("nTarsAssemblyNumber");
                    sw.WriteLine("rTarsAssemblyRevision");
                    sw.WriteLine("WTarsFirmwareRevision");
                    sw.WriteLine("TP");
                    sw.WriteLine("OTarsOperatorId");
                    sw.WriteLine("L1");
                    sw.WriteLine("p1");


                    // sw.WriteLine("[" + start.Year + "-" + stop.Month + "-" + stop.Day + " " + stop.Hour + ":" + stop.Minute + ":" + stop.Second);
                    sw.WriteLine("[" + start.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("]" + stop.ToString("yyyy-MM-dd HH:mm:ss"));


                    sw.WriteLine("MMES_StepsVerificationResults");
                    sw.WriteLine("dAll_OK");
                    sw.WriteLine(">-");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                port.Write("LOFF\r");
            }


            return 1;


        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {
                aTimer.Enabled = true;
                port.Write("LON\r");
            }
          //  port.WriteLine("LON");
            //port.Write("4C4F4E");
            //port.WriteLine("4C4F4E");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {
                aTimer.Enabled = false;
                port.Write("LOFF\r");
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
            start = DateTime.Now;
            
        }
    }
}
