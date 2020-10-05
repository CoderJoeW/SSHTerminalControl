using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace terminalcontrol
{
    public class SSH : RichTextBox
    {
        #region Public Variables
        string hostname, username, password;
        int port = 22;
        private SshClient sshclient;
        

        #endregion
        StreamReader reader;

        private ShellStream shellStream;

        #region Constructor
        public SSH()
        {
            
            this.BackColor = Color.Black;
            this.ForeColor = Color.Silver;
            this.Multiline = true;
            this.ReadOnly = true;

            FontFamily fontFamily = new FontFamily("Lucida Sans Typewriter");
            Font font = new Font(
               fontFamily,
               12,
               FontStyle.Regular,
               GraphicsUnit.Pixel);
            this.Font = font;

            //this.ScrollBars = ScrollBars.Vertical;
            //this.AcceptsTab = true;
            //event handlers
            
            this.KeyDown += SSH_KeyDown;
            
        }


        private string localcommand = "";
        private void SSH_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {

                if(e.Control && e.KeyCode == Keys.V)
                {
                    foreach (char Char in Clipboard.GetText(TextDataFormat.Text))
                    {
                        shellStream.Write(Char.ToString());
                        localcommand += Char.ToString();
                    }
                    
                    return;
                }
                //remaining is key tab, and nano
                //if (e.KeyCode == Keys.Tab)
                //{
                //    shellStream.DataReceived -= ShellStream_DataReceived;
                //    shellStream.WriteLine("ls");
                //    Thread.Sleep(500);
                //    string s = reader.ReadToEnd();
                //    this.AppendText(s);
                //    //to do tab work
                //}

                //if (e.KeyCode == Keys.Up)
                //{
                //    //shellStream.Write("\x24");
                //    //return;
                //}

                if (e.Control && e.KeyCode == Keys.C)
                {
                    shellStream.Write("\x03");
                    return;
                }
                if (e.Control && e.KeyCode == Keys.X)
                {
                    shellStream.Write("\x18");
                    return;
                }

                if (e.KeyCode == Keys.Back)
                {
                    //local command handled in data received method
                    shellStream.Write("\x08"); 
                    return;
                }

                if (e.KeyCode == Keys.Return)
                {
                    TraverseCommand_forLocal(localcommand);
                    localcommand = "";
                    shellStream.WriteLine("");
                    return;
                }



                if (e.KeyCode == Keys.Space)
                {
                    shellStream.Write(" ");
                    localcommand += " ";
                    return;
                }

                if (e.KeyCode == Keys.OemPeriod)
                {
                    shellStream.Write(".");
                    localcommand += ".";
                    return;
                }

                if (e.KeyCode == Keys.OemMinus)
                {
                    shellStream.Write("-");
                    localcommand += "-";
                    return;
                }

                
                if (e.KeyCode == Keys.OemQuestion)
                {
                    shellStream.Write("/");
                    localcommand += "/";
                    return;
                }

                if (e.KeyCode == Keys.Oem5)
                {
                    shellStream.Write("\\");
                    localcommand += "\\";
                    return;
                }




                if (e.Shift && ((e.KeyValue >= 0x30 && e.KeyValue <= 0x39) // numbers
                    || (e.KeyValue >= 0x41 && e.KeyValue <= 0x5A) // letters
                                                                    //|| (e.KeyValue >= 0x60 && e.KeyValue <= 0x69) // numpad
                    ))
                {
                    shellStream.Write(char.ConvertFromUtf32(e.KeyValue));
                    localcommand += char.ConvertFromUtf32(e.KeyValue);
                    //shellStream.Write(e.KeyCode.ToString());
                    //localcommand += e.KeyCode.ToString();
                    return;
                }

                //MessageBox.Show(e.KeyValue.ToString());
                //write alpha numberic values in small
                if ((e.KeyValue >= 0x30 && e.KeyValue <= 0x39) // numbers
                     || (e.KeyValue >= 0x41 && e.KeyValue <= 0x5A) // letters
                                                                   //|| (e.KeyValue >= 0x60 && e.KeyValue <= 0x69)// numpad
                     )
                {
                    shellStream.Write(char.ConvertFromUtf32(e.KeyValue).ToLower());
                    localcommand += char.ConvertFromUtf32(e.KeyValue).ToLower();
                    //shellStream.Write(e.KeyCode.ToString().ToLower());
                    //localcommand += e.KeyCode.ToString().ToLower();
                    return;
                }

            }
            catch (ObjectDisposedException ex)
            {
                MessageBox.Show("Connection failed");
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        private void TraverseCommand_forLocal(string command)
        {
            
            //string command = this.Lines[this.Lines.Length - 1].Substring(this.Lines[Lines.Length - 1].IndexOf("#") + 1);
            if (command.ToLower() == "clear")
            {
                this.Text = "";
            }

            if (command.ToLower().StartsWith("nano") || command.ToLower().StartsWith("sudo nano"))
            {
                this.Text = "reading file";
            }
        }



        private delegate void SafeCallDelegate(string text);

     






        #endregion

        public void connect()
        {
            try
            {

                ConnectionInfo ConnNfo = new ConnectionInfo(this.hostname, this.port, this.username,
                new AuthenticationMethod[]{

                // Pasword based Authentication
                new PasswordAuthenticationMethod(this.username,this.password),

                    // Key Based Authentication (using keys in OpenSSH Format)
                    //new PrivateKeyAuthenticationMethod("username",new PrivateKeyFile[]{
                    //    new PrivateKeyFile(@"..\openssh.key","passphrase")
                    //}),
                }
                );

                sshclient = new SshClient(ConnNfo);
                
                this.Select(this.Text.Length, 0);
                if (sshclient.IsConnected)
                {
                    throw new Exception("Client already connected");
                }
                else
                {
                    sshclient.Connect();

                    //var modes = new Dictionary<Renci.SshNet.Common.TerminalModes, uint>();
                    //shellStream = sshclient.CreateShellStream("xterm", 255, 50, 800, 600, 1024, modes);
                    
                    shellStream = sshclient.CreateShellStream("dumb", 80, 24, 800, 600, 1024);

                    reader = new StreamReader(shellStream);
                  
                    shellStream.DataReceived += ShellStream_DataReceived;
                  
                }

            }
            catch (Exception ex)
            {
                this.ReadOnly = true;
                throw ex;
            }
        }


        private void WriteTextSafe(string text)
        {
            if (this.InvokeRequired)
            {
                var d = new SafeCallDelegate(WriteTextSafe);
                this.Invoke(d, new object[] { text });
            }
            else
            {
               
                    string byte_string = "";
                    byte[] byte_array = Encoding.ASCII.GetBytes(text);
                    foreach (byte Byte in byte_array)
                    {
                        byte_string += Byte.ToString();

                    }
                if (byte_string == "8328")
                {
                    localcommand = localcommand.Substring(0, localcommand.Length - 1);
                    this.Text = this.Text.Substring(0, this.Text.Length - 1);
                    this.Select(this.Text.Length, 1);
                }
                else if (byte_string == "7")
                {
                    return;
                }
                else
                {
                    this.AppendText(text);
                }
                
                    
            }
        }

        private void ShellStream_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
        {
            
            WriteTextSafe(reader.ReadToEnd());
        }

        public bool getConnectionStatus()
        {
            return sshclient.IsConnected;
        }
        public void disconnect()
        {
            if (sshclient != null && sshclient.IsConnected)
            {
                sshclient.Disconnect();
                sshclient.Dispose();
            }
            else
            {
                throw new Exception("SSH Client is null or is not disconnected");
            }
        }

        #region User Properties
        [Browsable(true)]
        [Category("SSH Properties")]
        [Description("sets username")]
        [DisplayName("User Name")]
        public String UserName
        {
            get
            {
                return this.username;
            }
            set
            {
                this.username = value;
            }
        }


        [Browsable(true)]
        [Category("SSH Properties")]
        [Description("sets hostname")]
        [DisplayName("Host Name")]
        public String HostName
        {
            get
            {
                return this.hostname;
            }
            set
            {
                this.hostname = value;
            }
        }


        [Browsable(true)]
        [Category("SSH Properties")]
        [Description("sets password")]
        [DisplayName("Password")]
        public String Password
        {
            get
            {
                return this.password;
            }
            set
            {
                this.password = value;
            }
        }



        [Browsable(true)]
        [Category("SSH Properties")]
        [Description("sets port")]
        [DisplayName("Port")]
        public int Port
        {
            get
            {
                return this.port;
            }
            set
            {
                this.port = value;
            }
        }

        #endregion

    }

}


//custom control reference link : https://www.c-sharpcorner.com/UploadFile/ehtesham.dotnet/how-to-create-a-custom-control/
//ssh reference link : https://gist.github.com/piccaso/d963331dcbf20611b094