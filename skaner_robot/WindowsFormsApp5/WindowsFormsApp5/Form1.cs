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



        
        public Form1()
        {
            InitializeComponent();
            Application.ApplicationExit += new EventHandler(OnApplicationExit);





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
                port.Write("LON\r");
            }

        }

        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {
                port.Write("LOFF\r");
            }
            Thread.Sleep(250);
            port.Close();
        }

        private void zerowanieRS()
        {

            // clear the RCVbox text string and write the VER command
      //      s1.Text = lineReadIn = string.Empty;
            port.Write("VER\r");
        }

        string wydruk;
//        string[] result = new string[100];

        // this is called when the serial port has receive-data for us.
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs rcvdData)
        {

            while (port.BytesToRead > 0)
            {
                //   lineReadIn += port.ReadExisting();

                lineReadIn += port.ReadExisting();
             //   lineReadIn += Environment.NewLine;

                // lineReadIn += "\r\n";
                //   lineReadIn += lineReadIn;
                Thread.Sleep(25);
            }




            // display what we've acquired.

               

            lineReadIn = Regex.Replace(lineReadIn, @"\s+", string.Empty);
            if(wydruk != lineReadIn)
            tworzeniepliku(lineReadIn);
            displayTextReadIn(lineReadIn);

            wydruk = lineReadIn;
            lineReadIn = string.Empty;


        }// end function 'port_dataReceived'



        // this, hopefully, will prevent cross threading.
        private void displayTextReadIn(string ToBeDisplayed)          //wyswietlanie sygnalu na drugim texboxie
        {
            if (textBox1.InvokeRequired)
                textBox1.Invoke(accessControlFromCentralThread, ToBeDisplayed);
            else
                textBox1.Text = ToBeDisplayed;
            
        }

        private void operacje_na_danych(string ToBeDisplayed)          //wyswietlanie sygnalu na drugim texboxie
        {
            if (textBox1.InvokeRequired)
                textBox1.Invoke(accessControlFromCentralThread, ToBeDisplayed);
            else
            {
                //               Dane(ToBeDisplayed);
                textBox1.Text = ToBeDisplayed;
            }

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
            if(sn=="ERROR")
            {
                return 0;
            }
            string sciezka = ("C:/tars/");      //definiowanieścieżki do której zapisywane logi
            stop = DateTime.Now;
            if (Directory.Exists(sciezka))       //sprawdzanie czy sciezka istnieje
            {
                ;
            }
            else
                System.IO.Directory.CreateDirectory(sciezka); //jeśli nie to ją tworzy

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

            return 1;


        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            port.Write("LON\r");
          //  port.WriteLine("LON");
            //port.Write("4C4F4E");
            //port.WriteLine("4C4F4E");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            port.Write("LOFF\r");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            start = DateTime.Now;
        }
    }
}
