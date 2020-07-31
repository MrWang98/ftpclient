using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

namespace FTPClient
{
    public partial class Client : Form
    {
        public Client()
        {
            InitializeComponent();
            IPAddress = ipTextBox.Text;
            Port = portTextBox.Text;
            UserName = userNameTextBox.Text;
            Password = passwordTextBox.Text;
        }

        #region Text Variable
        private string IPAddress = "";
        private string Port = "";
        private string UserName = "";
        private string Password = "";
        private string localDir = "";
        private string remoteDir = "";
        private string localFileStr = "";
        private string remoteFIleStr = "";
        private const string CRLF = "\r\n";
        #endregion

        #region TCP Variable
        private TcpClient commandClient = null;
        private TcpClient dataClient = null;
        private bool connectStatus = false;
        private bool commandClientStatus = false;
        private bool dataClientStatus = false;
        private StreamReader commandStreamReader = null;
        private StreamReader dataStreamReader = null;
        private NetworkStream commandStreamWriter = null;
        private NetworkStream dataStreamWriter = null;
        #endregion

        #region Input Function
        private void ipTextBox_TextChanged(object sender, EventArgs e)
        {
            IPAddress = ipTextBox.Text;
        }

        private void portTextBox_TextChanged(object sender, EventArgs e)
        {
            Port = portTextBox.Text;
        }

        private void userNameTextBox_TextChanged(object sender, EventArgs e)
        {
            UserName = userNameTextBox.Text;
        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {
            Password = passwordTextBox.Text;
        }
        #endregion

        #region Click Function
        private void connectButton_Click(object sender, EventArgs e)
        {
            if (!commandClientStatus)
            {
                if (isEmpty(IPAddress) || isEmpty(Port) || isEmpty(UserName) || isEmpty(Password))
                {
                    string text = (isEmpty(IPAddress) ? "IP Addr " : "")
                        + (isEmpty(Port) ? "Port " : "")
                        + (isEmpty(UserName) ? "UserName " : "")
                        + (isEmpty(Password) ? "Password " : "");
                    MessageBox.Show(text + "should not be empty");
                    return;
                }
                connect();
            }
            else
            {
                disconnect();
            }

        }

        private void openButton_Click(object sender, EventArgs e)
        {
            string path = "";
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Choose Folder";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                path = dialog.SelectedPath;
                logListBox.Items.Add("Local Directory:" + path);
                refreshLocalDir(path);
            }
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            //string filePath = localDir+
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {

        }

        private void downloadButton_Click(object sender, EventArgs e)
        {

        }

        private void localListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = localListView.HitTest(e.X, e.Y);
            if(info.Item.Text == "...")
            {
                string parent = localDir.Replace(
                    "\\" + Regex.Split(localDir, @"\\").Last(), string.Empty);
                parent += parent.Contains("\\") ? "" : "\\";
                if (!Directory.Exists(parent))
                    return;
                refreshLocalDir(parent);
                return;
            }
            if (info.Item == null || info.Item.Tag.ToString() == "files")
                return;
            string newPath = localDir
                + ((Regex.Split(localDir, @"\\").Last() == "") ? "" : "\\")
                + info.Item.Text;
            if (!Directory.Exists(newPath))
                return;
            refreshLocalDir(newPath);
        }

        private void remoteListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = remoteListView.HitTest(e.X, e.Y);
            if (info.Item.Text == "...")
            {
                string parent = remoteDir.Replace(
                    "/" + Regex.Split(remoteDir, @"/").Last(), string.Empty);
                parent += parent.Contains("/") ? "" : "/";
                changeRemoteFilePath(parent);
                getRemoteFileList();
                return;
            }
            if (info.Item == null || info.Item.Tag.ToString() == "files")
                return;
            string newPath = remoteDir
                + ((remoteDir == "/") ? "" : "/")
                + info.Item.Text;
            changeRemoteFilePath(newPath);
            getRemoteFileList();
        }

        private void RemoteListView_Click(object sender, EventArgs e)
        {
            if (remoteListView.SelectedItems.Count == 0)
            {
                downloadButton.Enabled = false;
                deleteButton.Enabled = false;
                return;
            }
            downloadButton.Enabled = deleteButton.Enabled =
                (remoteListView.SelectedItems[0].Tag.ToString() == "file") && connectStatus;
            remoteFIleStr = remoteListView.SelectedItems[0].Text;
        }

        private void localListView_Click(object sender, EventArgs e)
        {
            if (localListView.SelectedItems.Count == 0)
            {
                uploadButton.Enabled = false;
                return;
            }
            uploadButton.Enabled =
                (localListView.SelectedItems[0].Tag.ToString() == "file") && connectStatus;
            localFileStr = localListView.SelectedItems[0].Text;
        }
        #endregion

        public void refreshLocalDir(string path)
        {
            localDirTextBox.Text = localDir = path;
            localListView.Items.Clear();
            string[] files = Directory.GetFiles(path);
            string[] directories = Directory.GetDirectories(path);
            if (Regex.Split(path, @"\\")[1] != "")
            {
                ListViewItem item = new ListViewItem("...");
                item.ForeColor = Color.BlueViolet;
                item.Tag = "directory";
                localListView.Items.Add(item);
            }
            foreach (string directory in directories)
            {
                string name = Regex.Split(directory, @"\\").Last();
                if (name.Contains('$') && name.IndexOf('$') == 0)
                    continue;
                ListViewItem item = new ListViewItem(name);
                item.ForeColor = Color.BlueViolet;
                item.Tag = "directory";
                localListView.Items.Add(item);
            }
            foreach (string file in files)
            {
                string name = Regex.Split(file, @"\\").Last();
                if (name.Contains('$') && name.IndexOf('$') == 0)
                    continue;
                else if (name.Contains("~$") && name.IndexOf("~$") == 0)
                    continue;
                else if (name.Contains(".ini") || name.Contains(".sys"))
                    continue;
                ListViewItem item = new ListViewItem(name);
                item.Tag = "file";
                localListView.Items.Add(item);
            }
        }

        private bool isEmpty(string s)
        {
            return s == string.Empty;
        }

        private void connect()
        {
            Cursor.Current = Cursors.WaitCursor;
            disableInput();
            try
            {
                logListBox.Items.Add("Trying to create Command Client ...");
                commandClient = new TcpClient(IPAddress, Convert.ToInt32(Port));
                commandClientStatus = true;
            }
            catch (Exception e)
            {
                commandClientStatus = false;
                Console.WriteLine(e.Message);
                logListBox.Items.Add("Command Client Connect Failed");
                logListBox.Items.Add("Error: " + e.Message);
            }
            if (!commandClientStatus)
            {
                MessageBox.Show("Connect Failed");
                enableInput();
            }
            else
            {
                logListBox.Items.Add("Connect to Command Client success!");
                commandStreamReader = new StreamReader(commandClient.GetStream());
                commandStreamWriter = commandClient.GetStream();
                read(commandStreamReader);
                login();
            }
        }

        private void disconnect()
        {
            if (dataClientStatus)
            {
                remoteDirTextBox.Clear();
                remoteListView.Clear();
                write(commandStreamWriter, "ABOR" + CRLF);
                read(commandStreamReader);
                dataStreamReader.Close();
                dataStreamWriter.Close();
                dataClient.Close();
                dataClient.Dispose();
                logListBox.Items.Add("Data Connection Closed");
                dataClientStatus = false;
            }
            if (commandClientStatus)
            {
                connectButton.Text = "Connect";
                write(commandStreamWriter, "QUIT" + CRLF);
                read(commandStreamReader);
                commandStreamReader.Close();
                commandStreamWriter.Close();
                commandClient.Close();
                commandClient.Dispose();
                logListBox.Items.Add("Command Connection Closed");
                commandClientStatus = false;
            }
            connectStatus = false;
            enableInput();
        }

        private string read(StreamReader reader)
        {
            string res = reader.ReadLine();
            logListBox.Items.Add("Receive:" + res);
            return res;
        }

        private void write(NetworkStream writer,string command)
        {
            logListBox.Items.Add("Send:" + command);
            byte[] data = Encoding.ASCII.GetBytes(command.ToCharArray());
            writer.Write(data, 0, data.Length);
        }

        private void login()
        {
            string res;
            write(commandStreamWriter, "USER " + UserName + CRLF);
            res = read(commandStreamReader);

            write(commandStreamWriter, "PASS " + Password + CRLF);
            res = read(commandStreamReader);
            if (Convert.ToInt32(res) == 530)
            {
                MessageBox.Show("Invalid Password");
                logListBox.Items.Add("Invalid Password");
                disconnect();
            }
            else
            {
                createDataConnection();
            }
        }

        private void createDataConnection()
        {
            string res;
            string[] resArray;
            int dataPort;

            write(commandStreamWriter, "PASV" + CRLF);
            res = read(commandStreamReader);
            resArray = Regex.Split(res, ",");
            dataPort = (Convert.ToInt32(resArray[4]) << 8)
                + Convert.ToInt32(resArray[5].Substring(0, resArray[5].Length - 2));
            logListBox.Items.Add("DataPort:" + dataPort);

            try
            {
                logListBox.Items.Add("Trying to create Data Client ...");
                dataClient = new TcpClient(IPAddress, dataPort);
                dataClientStatus = true;
            }
            catch (Exception e)
            {
                dataClientStatus = false;
                Console.WriteLine(e.Message);
                logListBox.Items.Add("Data Client Connect Failed");
                logListBox.Items.Add("Error: " + e.Message);
            }
            if (!dataClientStatus)
            {
                MessageBox.Show("Port "+dataPort+" Connect Failed");
                disconnect();
            }
            else
            {
                connectStatus = true;
                connectButton.Text = "Disconnect";
                logListBox.Items.Add("Connect to Data Client success!");
                dataStreamReader = new StreamReader(dataClient.GetStream());
                dataStreamWriter = dataClient.GetStream();
                getRemoteFilePath();
                getRemoteFileList();
            }
        }

        public void getRemoteFilePath()
        {
            write(commandStreamWriter, "PWD" + CRLF);
            string res = read(commandStreamReader);
            if (res.Contains("257"))
            {
                remoteDir = remoteDirTextBox.Text = Regex.Split(res, "\"")[1];
                logListBox.Items.Add("Current Remote Directory:" + remoteDir);
            }
        }

        public void changeRemoteFilePath(string path)
        {
            string res;
            write(commandStreamWriter, "CWD " + path + CRLF);
            res = read(commandStreamReader);
            if (res.Contains("250"))
            {
                remoteDir = remoteDirTextBox.Text = path;
                logListBox.Items.Add("Current Remote Directory:" + path);
            }
        }

        public void getRemoteFileList()
        {
            string res;
            write(commandStreamWriter, "LIST" + CRLF);
            read(commandStreamReader);

            remoteListView.Clear();
            if(remoteDir != "/")
            {
                ListViewItem item = new ListViewItem("...");
                item.Tag = "directory";
                remoteListView.Items.Add(item);
            }
            int directoryNum, fileNum;
            directoryNum = fileNum = 0;
            while ((res = read(dataStreamReader)) != null)
            {
                string pattern = @"([0-1]?[0-9]|2[0-3]):([0-5][0-9])";
                Match match = Regex.Matches(res, pattern)[0];
                string[] msgStrings = Regex.Split(res, match.Value + " ");
                string[] collection = new string[msgStrings.Length - 1];
                for (int i = 1; i < msgStrings.Length; i++)
                {
                    collection[i - 1] = msgStrings[i];
                }
                ListViewItem[] directories;
                if (res[0] != 'd')
                {
                    ListViewItem item = new ListViewItem(String.Join(match.Value + " ", collection));
                    item.Tag = "file";
                    remoteListView.Items.Insert(directoryNum + fileNum + 1, item);
                    fileNum++;
                }
                else
                {
                    ListViewItem item = new ListViewItem(String.Join(match.Value + " ", collection));
                    item.Tag = "directory";
                    item.ForeColor = Color.BlueViolet;
                    remoteListView.Items.Insert(directoryNum + 1, item);
                    directoryNum++;
                }
            }
        }

        private void disableInput()
        {
            ipTextBox.Enabled = false;
            portTextBox.Enabled = false;
            userNameTextBox.Enabled = false;
            passwordTextBox.Enabled = false;
        }

        private void enableInput()
        {
            ipTextBox.Enabled = true;
            portTextBox.Enabled = true;
            userNameTextBox.Enabled = true;
            passwordTextBox.Enabled = true;
        }
    }
}
