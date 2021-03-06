﻿using System;
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

namespace ftpclient
{
    public partial class Form1 : Form
    {

        #region  Private variable
        private TcpClient cmdServer;
        private TcpClient dataServer;
        private NetworkStream cmdStrmWtr;
        private StreamReader cmdStrmRdr;
        private NetworkStream dataStrmWtr;
        private StreamReader dataStrmRdr;
        private String cmdData;
        private byte[] szData;
        private const String CRLF = "\r\n";
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        //连接
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "连接")
            {
                
                Cursor cr = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                cmdServer = new TcpClient(textBox1.Text, Convert.ToInt32(textBox2.Text));
                listBox1.Items.Clear();
                try
                {
                    cmdStrmRdr = new StreamReader(cmdServer.GetStream());
                    cmdStrmWtr = cmdServer.GetStream();
                    this.getSatus();

                    string retstr;

                    //Login
                    cmdData = "USER " + textBox3.Text + CRLF;
                    szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                    cmdStrmWtr.Write(szData, 0, szData.Length);
                    this.getSatus();

                    cmdData = "PASS " + textBox4.Text + CRLF;
                    szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                    cmdStrmWtr.Write(szData, 0, szData.Length);
                    retstr = this.getSatus().Substring(0, 3);
                    if (Convert.ToInt32(retstr) == 530) throw new InvalidOperationException("帐号密码错误");

                    /*cmdData = "PWD"  + CRLF;
                    szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                    cmdStrmWtr.Write(szData, 0, szData.Length);
                    string message=this.getSatus();
                    string remoteDir;
                    if (message.Contains("257"))
                    {
                        remoteDir = Regex.Split(message, "\"")[1];
                    }

                    cmdData = "CWD " + "/home" + CRLF;
                    szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                    cmdStrmWtr.Write(szData, 0, szData.Length);
                    string messageCWD = this.getSatus();

                    cmdData = "PWD" + CRLF;
                    szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                    cmdStrmWtr.Write(szData, 0, szData.Length);
                    string message2 = this.getSatus();
                    string remoteDirCWD;
                    if (message2.Contains("257"))
                    {
                        remoteDirCWD = Regex.Split(message2, "\"")[1];
                    }

                    cmdData = "CDUP" + CRLF;
                    szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                    cmdStrmWtr.Write(szData, 0, szData.Length);
                    string messageCDUP=this.getSatus();

                    cmdData = "PWD" + CRLF;
                    szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                    cmdStrmWtr.Write(szData, 0, szData.Length);
                    string message3 = this.getSatus();
                    string remoteDirCDUP;
                    if (message.Contains("257"))
                    {
                        remoteDirCDUP = Regex.Split(message3, "\"")[1];
                    }*/


                    this.freshFileBox_Right();

                    label1.Text = textBox1.Text + ":";
                    button1.Text = "断开";
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button5.Enabled = true;
                }
                catch (InvalidOperationException err)
                {
                    listBox1.Items.Add("ERROR: " + err.Message.ToString());
                }
                finally
                {
                    Cursor.Current = cr;
                }
            }
            else
            {
                Cursor cr = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;

                //Logout

                cmdData = "QUIT" + CRLF;
                szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
                cmdStrmWtr.Write(szData, 0, szData.Length);
                this.getSatus();


                cmdStrmWtr.Close();
                cmdStrmRdr.Close();

                label1.Text = "";
                button1.Text = "连接";
                button2.Enabled = false;
                button3.Enabled = false;
                button5.Enabled = false;
                listBox3.Items.Clear();

                Cursor.Current = cr;
            }
        }

        #region  Private Functions

        /// <summary>
        /// 获取命令端口返回结果，并记录在listBox1上
        /// </summary>
        private String getSatus()
        {

            String ret = cmdStrmRdr.ReadLine();
            listBox1.Items.Add(ret);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            return ret;
        }

        /// <summary>
        /// 进入被动模式，并初始化数据端口的输入输出流
        /// </summary>
        private void openDataPort()
        {
            string retstr;
            string[] retArray;
            int dataPort;

            // Start Passive Mode 
            cmdData = "PASV" + CRLF;
            szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
            cmdStrmWtr.Write(szData, 0, szData.Length);
            retstr = this.getSatus();
            
            // Calculate data's port
            retArray = Regex.Split(retstr, ",");
            retstr = retArray[5].Substring(0, retArray[5].Length-2);
            dataPort = Convert.ToInt32(retArray[4]) * 256 + Convert.ToInt32(retstr);
            listBox1.Items.Add("Get dataPort=" + dataPort);


            //Connect to the dataPort
            dataServer = new TcpClient(textBox1.Text, dataPort);
            dataStrmRdr = new StreamReader(dataServer.GetStream());
            dataStrmWtr = dataServer.GetStream();
        }

        /// <summary>
        /// 断开数据端口的连接
        /// </summary>
        private void closeDataPort()
        {
            dataStrmRdr.Close();
            dataStrmWtr.Close();
            this.getSatus();

            cmdData = "ABOR" + CRLF;
            szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
            cmdStrmWtr.Write(szData, 0, szData.Length);
            this.getSatus();

        }

        /// <summary>
        /// 获得/刷新 右侧的服务器文件列表
        /// </summary>
        private void freshFileBox_Right()
        {

            openDataPort();

            string absFilePath;

            //List
            cmdData = "LIST" + CRLF;
            szData = System.Text.Encoding.ASCII.GetBytes(cmdData.ToCharArray());
            cmdStrmWtr.Write(szData, 0, szData.Length);
            this.getSatus();

            listBox3.Items.Clear();
            while ((absFilePath = dataStrmRdr.ReadLine()) != null)
            {
                //
                string pattern = @"([0-1]?[0-9]|2[0-3]):([0-5][0-9])";
                Match match = Regex.Matches(absFilePath, pattern)[0];//匹配文件信息中的“时间”，例如00：00
                string[] msgStrings = Regex.Split(absFilePath, match.Value + " ");//从 “时间”+空格 处分割文件信息字符串成前后两个，因为防止文件名中有同样的时间+空格，所以要再设个字符串数组
                                                                            //将文件名的字符串收集起来，假如文件名里有时间且也被分割的话，就用第二个字符串数组把文件名再拼接起来
                string[] collection = new string[msgStrings.Length - 1];//收集文件名的字符串
                for (int i = 1; i < msgStrings.Length; i++)
                {
                    collection[i - 1] = msgStrings[i];
                }
                if (absFilePath.IndexOf("d", StringComparison.OrdinalIgnoreCase) != 0)//显示非文件夹文件
                {
                    listBox3.Items.Add(String.Join(match.Value + " ", collection));//拼接并输出
                }
            
                


                //判断是否是文件
                //absFilePath.IndexOf("d", StringComparison.OrdinalIgnoreCase) == 0

            }

            closeDataPort();
        }

        /// <summary>
        /// 获得/刷新 左侧的本地文件列表
        /// </summary>
        private void freshFileBox_Left()
        {
            listBox2.Items.Clear();
            if (textBox5.Text == "") return;
            var files = Directory.GetFiles(textBox5.Text, "*.*");
            foreach (var file in files)
            {
                Console.WriteLine(file);
                string[] temp = Regex.Split(file, @"\\");
                listBox2.Items.Add(temp[temp.Length - 1]);
            }
        }

        #endregion

        private void button4_Click(object sender, EventArgs e)
        {
            string path = string.Empty;
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                path = fbd.SelectedPath;
                listBox1.Items.Add("选中本地路径:" + path);
            }

            textBox5.Text = path;
            freshFileBox_Left();
        }

        //上传文件
        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox5.Text == "" || listBox2.SelectedIndex < 0)
            {
                MessageBox.Show("请选择上传的文件", "ERROR");
                return;
            }

            Cursor cr = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            string fileName = listBox2.Items[listBox2.SelectedIndex].ToString();
            string filePath = textBox5.Text + "\\" + fileName;

            this.openDataPort();

            
            cmdData = "STOR " + fileName + CRLF;
            char[] a = cmdData.ToCharArray();
            byte[] b = System.Text.Encoding.UTF8.GetBytes(a);
            szData = b;
            cmdStrmWtr.Write(szData, 0, szData.Length);
            this.getSatus();

            FileStream fstrm = new FileStream(filePath, FileMode.Open);
            byte[] fbytes = new byte[1030];
            int cnt = 0;
            while ((cnt = fstrm.Read(fbytes, 0, 1024)) > 0)
            {
                dataStrmWtr.Write(fbytes, 0, cnt);
            }
            fstrm.Close();

            this.closeDataPort();

            this.freshFileBox_Right();

            Cursor.Current = cr;
        }

        //下载文件
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox5.Text == "" || listBox3.SelectedIndex < 0)
            {
                MessageBox.Show("请选择目标文件和下载路径", "ERROR");
                return;
            }

            Cursor cr = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            string fileName = listBox3.Items[listBox3.SelectedIndex].ToString();
            string filePath = textBox5.Text + "\\" + fileName;

            this.openDataPort();



            cmdData = "RETR " + fileName + CRLF;
            szData = System.Text.Encoding.UTF8.GetBytes(cmdData.ToCharArray());
            cmdStrmWtr.Write(szData, 0, szData.Length);
            this.getSatus();


            char[] fchars = new char[1030];
            byte[] fbytes = new byte[1030];
            int cnt = 0;
            FileStream fstrm;


            long size_t;
            int offset;
            if (File.Exists(filePath))//断点续传，如果本地文件已存在且不覆盖，则进行断点续传
            {
                if ((int)MessageBox.Show("文件已存在，是否覆盖","提示",MessageBoxButtons.OKCancel,MessageBoxIcon.Exclamation)==2)
                {
                    FileInfo fi = new FileInfo(filePath);
                    offset = int.Parse(fi.Length.ToString().Split('.')[0]);
                    fstrm = new FileStream(filePath, FileMode.Append);
                    dataStrmWtr.Read(fbytes, 0, offset);
                    while ((cnt = dataStrmWtr.Read(fbytes, 0, 1024)) > 0)
                    {
                        fstrm.Write(fbytes, 0, cnt);
                    }
                }
                else
                {
                    fstrm = new FileStream(filePath, FileMode.Open);
                    while ((cnt = dataStrmWtr.Read(fbytes, 0, 1024)) > 0)
                    {
                        fstrm.Write(fbytes, 0, cnt);
                    }
                }
            }
            else
            {
                fstrm = new FileStream(filePath, FileMode.Create);
                while ((cnt = dataStrmWtr.Read(fbytes, 0, 1024)) > 0)
                {
                    fstrm.Write(fbytes, 0, cnt);
                }
            }
            
            fstrm.Close();

            this.closeDataPort();

            this.freshFileBox_Left();

            Cursor.Current = cr;
        }

        //删除文件
        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex < 0)
            {
                MessageBox.Show("请选择要删除的目标文件", "ERROR");
                return;
            }

            Cursor cr = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            string fileName = listBox3.Items[listBox3.SelectedIndex].ToString();
            string filePath = textBox5.Text + "\\" + fileName;

            cmdData = "DELE " + fileName + CRLF;
            szData = System.Text.Encoding.UTF8.GetBytes(cmdData.ToCharArray());
            cmdStrmWtr.Write(szData, 0, szData.Length);
            this.getSatus();
            
            this.freshFileBox_Right();

            Cursor.Current = cr;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (button6.Text == "显示密码")
            {
                textBox4.PasswordChar = '\0';
                button6.Text = "隐藏密码";
            }
            else
            {
                textBox4.PasswordChar = '*';
                button6.Text = "显示密码";
            }
        }
    }
}
