using System;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using PaintchatChatter.Properties;
using System.Collections;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace PaintchatChatter
{
    

    public partial class ChatWindow : Form
    {

        

        TcpClient client;
        IPEndPoint serverEndPoint;
        NetworkStream clientStream;
        private Thread listenThread;

        public bool closeRequested = false;

        private bool sendHex = false;

        bool connected = false;
        int numThreads = 0;
        List<string> UserName = new List<string>();
        List<string> UserID = new List<string>();

        //System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
        //byte[] buffer;
        //ArrayList ByteArray = new ArrayList();


        delegate void SetTextCallback(string text);
       delegate void SetListAddCallback(string text,string ID);
       delegate void SetListRemoveCallback(string text, string ID);
      //  delegate void SetListRemoveCallback(string text);

        private void CloseMe(object o, EventArgs e)
        {
            // Decrease number of threads and shutdown
            this.numThreads--;
            this.Close();
           // textBox1.Text += "MAN";
        }

        static void HighlightPhrase(RichTextBox box, string phrase, Color color)
        {
            int pos = box.SelectionStart;
            string s = box.Text;
            for (int ix = 0; ; )
            {
                int jx = s.IndexOf(phrase, ix, StringComparison.CurrentCultureIgnoreCase);
                if (jx < 0) break;
                box.SelectionStart = jx;
                box.SelectionLength = phrase.Length;
                box.SelectionColor = color;
                ix = jx + 1;
            }
            box.SelectionStart = pos;
            box.SelectionLength = 0;
        }

        private void Settext(string text)
        {

            this.richTextBox1.Text += text;
            this.richTextBox1.Text += "\n";
            this.richTextBox1.SelectionStart = richTextBox1.Text.Length;
            this.richTextBox1.ScrollToCaret();
         
            
        }

        private void AddList(string text,string ID)
        { 
            UserName.Add(text);
            UserID.Add(ID);
            listBox1.Items.Add(text);
            this.richTextBox1.Text += text + " HAS CONNECTED \n";
            this.richTextBox1.SelectionStart = richTextBox1.Text.Length;
            this.richTextBox1.ScrollToCaret();
    
        }

        private void RemoveList(string text, string ID)
        {
            UserName.Remove(text);
            UserID.Remove(ID);
            listBox1.Items.Remove(text);
            this.richTextBox1.Text += text + " HAS DISCONNECTED \n";
            this.richTextBox1.SelectionStart = richTextBox1.Text.Length;
            this.richTextBox1.ScrollToCaret();
          
        }

        private void ListenForServer(object client)
        {

            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            SetTextCallback d = new SetTextCallback(Settext);
            SetListAddCallback listAdd = new SetListAddCallback(AddList);
            SetListRemoveCallback listRemove = new SetListRemoveCallback(RemoveList);
            //buffer = encoding.GetBytes("");

            byte[] message = new byte[65535];
            byte[] message2 = new byte[2];
            int bytesRead = 0;
          //  I realize this is a mess, I dont care
            string text = "";
            int length = 0;
            int length2 = 0;
            int offset = 0;
            int i = 0;
            int ID = 0;
            int LOGGEDIN = 0;
            int value = 0;

            bool firstlogin = true;
            while (true) 
            {
                
                try
                {
                    Array.Clear(message, 0, message.Length);
                   // firstlogin = false;
                 
                    if (firstlogin == true)
                    {
                        bytesRead = clientStream.Read(message, 0, 1);
                        bytesRead = clientStream.Read(message2, 0, 1);
                        length = ((int)message[0] << 8) | (int)message2[0];//LENGTH OF DATA
                      //  bytesRead = clientStream.Read(message, 0, 2);
                       // length = message[0];
                        bytesRead = clientStream.Read(message, 0, length);
                        
                            for (i = 0; i <= length + 1; i++)
                            {
                                text += (char)message[i];
                            }

                            this.Invoke(d, new object[] { text });
                            firstlogin = false;
                            text = "";
                    }
                    else
                    {
                        //AFTER INITIAL LOGIN SERVER WILL SEND 2 BYTES (LENGTH), then the message!
                       
                        bytesRead = clientStream.Read(message, 0, 1);
                        bytesRead = clientStream.Read(message2, 0, 1);
                        length = ((int)message[0]<<8) | (int)message2[0];//LENGTH OF DATA
                        //string text2 = encoding.GetString(message, 0, bytesRead);
                        Thread.Sleep(100);
                        if (length == 0)
                        {//PING?
                            //this.Invoke(d, new object[] {DateTime.Now.ToString("HH:mm:ss tt") +  " : --PING!-- "}); 
                        }
                        else
                        { 
                            bytesRead = clientStream.Read(message, 0, 1);
                            bytesRead = clientStream.Read(message2, 0, 1);
                            length2 = ((int)message[0] << 8) | (int)message2[0];//LENGTH OF MESSAGE
                            bytesRead = clientStream.Read(message, 0, length-2);//full message
                            //text2 = encoding.GetString(message, 0, bytesRead);
                            offset = 4;
                            while (offset <= bytesRead)
                            {
                                ID = message[offset - 2];//ID OF USER, need to be used to create a list of users
                                LOGGEDIN = message[offset - 4];//IF LOGGED IN OR NOT (ONLY USEFUL WHEN GRABBING ID LIST ON FIRST LOG, OR IF SOMEONE LOGS IN OR OUT)
                                //0 on message, 1 on login, 2 on logout
                                value = message[offset-1];//LENGTH OF NAME BEFORE TEXT, ONLY USEFUL ON FIRST LOGIN.
                               
                               // else
                               // {
                                    //TEXT
                                    for (i = offset; i <= length2 - 1; i++)
                                    {
                                    text += (char)message[i];
                                    }
                                    if (value > 0)
                                    {text = text.Insert(value, ">");}

                                    if (LOGGEDIN == 1)
                                    {//LOGGED IN
                                        this.Invoke(listAdd, new object[] { text, ID.ToString() });
                                    }
                                    else if (LOGGEDIN == 2)
                                    {//LOGGED OUT
                                        this.Invoke(listRemove, new object[] { text, ID.ToString() });
                                    }
                                    else
                                    {//LOGGED OUT

                                      //  this.Invoke(d, new object[] { text });
                                        int ndx = UserID.FindIndex(s => s == ID.ToString());
                                        
                                        //int Name = UserName.IndexOf(ndx);
                                        //  UserName.Find(b => b.Property == value);
                                        if (ndx == -1)
                                        { 
                                            this.Invoke(d, new object[] { text }); 
                                        }
                                        else
                                        {
                                            string Name = UserName[ndx]; 
                                             text = text.Insert(0, Name + ">");
                                             this.Invoke(d, new object[] { text });
                                        }
                                    }
                                  
                                text = "";
                                offset = (i + 6);
                                length2 = message[i + 1] + (offset - 4);
                               // }
                            }
                        }

                    }
 
                }
                catch
                {
                  //  richTextBox1.Text += "Some Error";
                    break;
                    
                }
                if (bytesRead == 0)
                {
                   // this.closeRequested = true;
                    
                   this.Invoke(d, new object[] { "***SERVER HAS DROPPED*** \r\n" });
                    MessageBox.Show("***SERVER HAS DROPPED***", "No Connection!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
                  //  return;
                }
               
            }
            Disconnect();
          //  message = encoding.GetBytes("");
          //  clientStream.Flush();
            clientStream.Close();
            tcpClient.Close(); 
            //System.Console.Write("---the client has disconnected from the server---");
        }

        public ChatWindow()
        { 
            InitializeComponent();
           // textBox1.Text = " \r\n";  
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
        }

        private void ChatWindow_Load(object sender, EventArgs e)
        {

          //  richTextBox1.Text.Insert(0, "{\\colortbl ;\\red128\\green0\\blue0;\\red0\\green128\\blue0;\\green0\\blue255;}");
            // Console.WriteLine("Now Receiving messages..");
            //buffer = encoding.GetBytes(""); 
          
            try
            {
                
                string[] text = System.IO.File.ReadAllLines(@"Config.txt");
                textBox5.Text = text[0];
                textBox4.Text = text[1];
                textBox2.Text = text[2];
                textBox3.Text = text[3];
            }
            catch
            {
                MessageBox.Show("Cannot find Config.txt!", "No Configuration!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
               
            }
        }

        
        private void timer1_Tick(object sender, EventArgs e)
        {    
        }
        
        private void mKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {         
        }

        private void hexCheckBox_CheckedChangedHandler(object sender, EventArgs e)
        {
            this.sendHex = ((System.Windows.Forms.CheckBox)sender).Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] data5 = Encoding.ASCII.GetBytes(textBox1.Text);
                byte[] data6 = { 0x00, (byte)((textBox1.TextLength + 6)-2), 0x00, 0x00, 0x00, 0x00 };//message   
                //byte[] data4 = { 0x00, (byte)((textBox1.TextLength)/2+6) };//length of message

                byte[] buffer = {0x00};
                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();

                if (sendHex) {

                    string hexString = Regex.Replace(textBox1.Text, @"[^0-9A-Fa-f]", "");
                    byte[] data = StringToByteArray(hexString);

                    buffer = new byte[] { (byte)data.Length };
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();

                    clientStream.Write(data, 0, data.Length);
                    clientStream.Flush();

                    richTextBox1.Text += "#HEX# " + hexString + "\n";
                } else {
                    buffer = new byte[] { (byte)(textBox1.TextLength + 6)};
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();

                    byte[] data = new byte[data6.Length + data5.Length];
                    System.Buffer.BlockCopy(data6, 0, data, 0, data6.Length);
                    System.Buffer.BlockCopy(data5, 0, data, data6.Length, data5.Length);
                    clientStream.Write(data, 0, data.Length);
                    clientStream.Flush();
                    richTextBox1.Text += textBox2.Text + "> " + textBox1.Text + "\n";

                }

                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                textBox1.Text = "";
            }
            catch
            {
                richTextBox1.Text += "Connection Error \r\n";
            }     
        }

        private void Connect()
        {
                try
                {   
                     client = new TcpClient();
                     serverEndPoint = new IPEndPoint(IPAddress.Parse(textBox5.Text), Convert.ToInt32(textBox4.Text));
                    //byte[] message = new byte[4096];
              
                    client.Connect(serverEndPoint);
                    clientStream = client.GetStream();

                    string pass = textBox3.Text;
                    string user_name = textBox2.Text;
                    listBox1.Items.Add(user_name);
                    byte[] data = Encoding.ASCII.GetBytes("name=" + user_name + "\npassword=" + pass + "\nprotocol=paintchat.text");
                   
                    byte[] data2 = { 0x62, 0x00, (byte)data.Length };
                    clientStream.Write(data2, 0, data2.Length);

                    clientStream.Write(data, 0, data.Length);

                    connected = true;
                    //create a thread to listen for messages FROM the server
                    this.numThreads++;
                    this.listenThread = new Thread(new ParameterizedThreadStart(this.ListenForServer));
                    this.listenThread.Start(client);
                    button3.Text = "Disconnect";
                    textBox2.Enabled = false;
                    textBox3.Enabled = false;
                }
                catch
                {
                    richTextBox1.Text += "--CANNOT REACH SERVER-- \r\n";
                    textBox2.Enabled = true;
                    textBox3.Enabled = true;
                }
           }

        private void Disconnect()
        {
            try
            {
                //ADD CODE TO TELL SERVER YOU ARE DISCONNECTING
                byte[] data4 = { 0x00, 0x06, 0x00, 0x04, 0x02, 0x00, 0x00, 0x00 };//disconnect message
                clientStream.Write(data4, 0, data4.Length);

                listBox1.Items.Clear();
                UserID.Clear();
                UserName.Clear();
             //   Thread.Sleep(200);
                clientStream.Close();
               // Thread.Sleep(200);
                listenThread.Abort(client);
                client.Close();
                connected = false;
                textBox2.Enabled = true;
                textBox3.Enabled = true;
                button3.Text = "Connect";
               
            }
            catch
            {
            }
           
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
          
        }

        private void textBox1_ModifiedChanged(object sender, EventArgs e)
        {
           
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;//STOPS ANNOYING BEEPS (checks to see if the keypress was handled, if false than beep)
                button1_Click(sender,e);       
            }
        }

        private void ChatWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
           // Random random = new Random();
            
            //this.richTextBox1.SelectionStart = richTextBox1.Text.Length;
          //  this.richTextBox1.ScrollToCaret(); 
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void fontDialog1_Apply(object sender, EventArgs e)
        {
            //richTextBox1.Font = fontDialog1.Font;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
           //fontDialog1.Font =  fontDialog1.ShowDialog();
            fontDialog1.ShowColor = true;

            if (fontDialog1.ShowDialog() != DialogResult.Cancel)
            {
                richTextBox1.Font = fontDialog1.Font;
                richTextBox1.ForeColor = fontDialog1.Color;
            }

        }

        private void textBox3_TextChanged_1(object sender, EventArgs e)
        {

        }


        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = false;
           this.closeRequested = true;
           try
           {
               byte[] data4 = { 0x00, 0x06, 0x00, 0x04, 0x02, 0x00, 0x00, 0x00 };//disconnect message
               clientStream.Write(data4, 0, data4.Length);
               clientStream.Close();
               Thread.Sleep(200);
              listenThread.Abort(client);
           }
           catch
           {
           }

        }
       



        private void ChatWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
  
        }

        private void button3_Click(object sender, EventArgs e)
        {
       
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (connected == true)
            {

                Disconnect();
            }
            else
            {
                Connect();
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        public static byte[] StringToByteArray(string hex) {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            string str = listBox1.Items[e.Index] as string; // Get the current item and cast it to MyListBoxItem
                e.Graphics.DrawString( // Draw the appropriate text in the ListBox
                        str, // The message linked to the item
                        listBox1.Font, // Take the font from the listbox
                        new SolidBrush(Color.Black), // Set the color 
                        e.Bounds
                        );
        }

    }   

}
