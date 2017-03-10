using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace serial
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private StringBuilder builder = new StringBuilder();
        SerialPort port1 = new SerialPort();
        int lines = 6000;
        int ll = 0;
        //数据发送次数
        //int sts = 0;
        //数据接收次数
        //int rts = 0;
        //总数据发送长度
        int sdl = 0;
        //总数据接收长度
        int rdl = 0;
        //命令发送次数
        int s0 = 0;
        int s1 = 0;
        int s2 = 0;
        int s3 = 0;
        int s4 = 0;
        int s5 = 0;
        //命令接收次数
        int r0 = 0;
        int r1 = 0;
        int r2 = 0;
        int r3 = 0;
        int r4 = 0;
        int r5 = 0;
        private List<byte> buffer = new List<byte>(1024);//默认分配1页内存，并始终限制不允许超过  
        //private List<byte> img_Data = new List<byte>();//缓存串口返回的照片数据
        private bool Listening = false;//是否没有执行完invoke相关操作  
        private bool Closing = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke 
        StringBuilder sb = new StringBuilder();
        private void Form1_Load(object sender, EventArgs e)
        {
            Port_Select();
            this.comboBox1.SelectedIndex = 0;
            //this.comboBox2.SelectedIndex = 0;
            //port1.NewLine = "\r\n";
        }
        private void Port_Select()
        {//获取机器中的串口地址
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "关闭")  //当要关闭串口的时候
            {
                Closing = true;
                port1.DiscardOutBuffer();
                port1.DiscardInBuffer();
                try
                {
                    port1.Close();
                    button1.Text = "打开";
                    string s = "关闭串口：" + comboBox1.SelectedItem.ToString() + "\r\n";
                    textBox2.AppendText(string.Format("{0:MM-dd HH:mm:ss ffff}", DateTime.Now) + "\tT:" + s);
                    comboBox1.Enabled = true;
                }
                catch
                {
                    Closing = false;
                    button1.Text = "关闭";
                    string s = "关闭串口：" + comboBox1.SelectedItem.ToString() + "失败\r\n";
                    textBox2.AppendText(string.Format("{0:MM-dd HH:mm:ss ffff}", DateTime.Now) + "\tT:" + s);
                    comboBox1.Enabled = false;
                }
                //button2.Enabled = false;
            }
            else if (button1.Text == "打开") //当要打开串口的时候
            {
                int bd = int.Parse(comboBox2.SelectedItem.ToString());
                int dbs = int.Parse(comboBox3.SelectedItem.ToString());
                string py = comboBox4.SelectedItem.ToString();
                Parity pys=new Parity();
                StopBits sbs = new StopBits();
                switch (py)
                {
                    case "None":
                        pys = Parity.None;
                        py = "N";
                        break;
                    case "Odd":
                        pys = Parity.Odd;
                        py = "O";
                        break;
                    case "Even":
                        pys = Parity.Even;
                        py = "E";
                        break;
                }
                string sb = comboBox5.SelectedItem.ToString();
                switch(sb)
                {
                    case "1":
                        sbs = StopBits.One;
                        break;
                    case "1.5":
                        sbs = StopBits.OnePointFive;
                        break;
                    case "2":
                        sbs = StopBits.Two;
                        break;
                }
                try
                {
                    port1.PortName = comboBox1.SelectedItem.ToString();
                    port1.BaudRate = bd;
                    port1.DataBits = dbs;                    
                    port1.Parity = pys;                    
                    port1.StopBits = sbs;
                    //port1.RtsEnable = true;
                    port1.ReadBufferSize = 1024;
                    port1.Encoding = Encoding.BigEndianUnicode;
                    port1.Open();
                    button1.Text = "关闭";
                    comboBox1.Enabled = false;
                    //button2.Enabled = true;
                    Closing = false;
                    string s = "打开串口：" + comboBox1.SelectedItem.ToString() +","+ bd.ToString() + ","+ py+ "," + sb+"\r\n";
                    textBox2.AppendText(string.Format("{0:MM-dd HH:mm:ss ffff}", DateTime.Now)+ "\tT:" + s);
                    port1.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
                }
                catch
                {
                    button1.Text = "打开";
                    //label3.Text = "串口：" + comboBox1.SelectedItem.ToString() + "打开失败";
                    MessageBox.Show("该串口无法打开");
                }
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            //Thread.Sleep(100);
            if(port1.IsOpen==false)
            { return; }
            int n = port1.BytesToRead;
            rdl += n;
            this.Invoke((EventHandler)(delegate
            {
                //this.label13.ForeColor = Color.Red;
                this.label15.Text = rdl.ToString();
                this.textBox2.AppendText(string.Format("{0:MM-dd HH:mm:ss ffff}", DateTime.Now) + "\tR:" + n.ToString()+"字节数据\r\n");
            }));
            byte[] buf = new byte[n];
            if (Closing) return;
            try
            {                
                port1.Read(buf, 0, n);
                // sb.Append(ASCIIEncoding.ASCII.GetString(buf));
                buffer.AddRange(buf);
                sb.Append(Encoding.GetEncoding("gb2312").GetString(buf));                
            }
            catch
            {

            }
            if (buffer.Count > 0)
            {
                string rev = "\tHex:" + buf2hex(buf);
                //判断是否为乱码
                if (sb.ToString() == null)
                {
                    buffer.RemoveRange(0, buffer.Count);
                    rev += buf2hex(buf) + Environment.NewLine;
                    //return;
                }
                else
                {
                    rev += ("\tGBK:" + sb.ToString());
                }
                rev= ("L："+ll.ToString()+"\tT:"+string.Format("{0:MM-dd HH:mm:ss ffff}", DateTime.Now)+":"+rev+ Environment.NewLine);                
                if(checkBox1.Checked==true)//自动回包
                {
                    int r = tbreg0.Text.Trim().Length + tbreg1.Text.Trim().Length + tbreg2.Text.Trim().Length + tbreg3.Text.Trim().Length + tbreg4.Text.Trim().Length + tbreg5.Text.Trim().Length;
                    int s = bkcmd0.Text.Trim().Length + bkcmd1.Text.Trim().Length + bkcmd2.Text.Trim().Length + bkcmd3.Text.Trim().Length + bkcmd4.Text.Trim().Length + bkcmd5.Text.Trim().Length;
                    if (checkBox2.Checked == true||r==0)//收发相同or未填接收命令
                    {
                        port1.Write(buf, 0, n);
                        sdl += n;
                        this.Invoke((EventHandler)(delegate
                        {
                            //this.label13.ForeColor = Color.Red;
                            this.label17.Text = sdl.ToString();
                            this.textBox2.AppendText(string.Format("{0:MM-dd HH:mm:ss ffff}", DateTime.Now) + "\tRepeat:" + n.ToString() + "字节数据\r\n");
                        }));
                    }
                    else
                    {
                        checkBack(rev,s);
                    }
                }
                /*Regex crgsn = new Regex(@"\d{11}[D][8][4][0][F]");
                Regex c2 = new Regex(@"\d{11}[C][4][5][8][B]");//01 03 03 00 00 0C 45 8B
                MatchCollection mcrgsn = crgsn.Matches(rev);
                MatchCollection m2 = c2.Matches(rev);
                if (mcrgsn.Count != 0)
                {
                    string str = "01 03 1A 00 03 03 13 4D 44 34 34 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 C9 F6";
                    byte[] str2hex = strToToHexByte(str);
                    port1.Write(str2hex, 0, 31);
                    rev += ("/r/n发送1:" + str);
                }
                if (m2.Count != 0)
                {
                    string str = "01 03 18 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 7C 34";
                    byte[] str2hex = strToToHexByte(str);
                    port1.Write(str2hex, 0, 29);
                    rev += ("/r/n发送2:" + str);
                }*/
                buffer.RemoveRange(0, buffer.Count);

                //buffer.RemoveRange(0, buffer.Count);
                this.Invoke((EventHandler)(delegate
                {
                    this.textBox1.AppendText(rev);
                    ll++;
                    if (ll>=lines)
                    {
                        string dt = string.Format("{0:yyyyMMddHHmmssffff}", DateTime.Now);
                        saveTxt(dt);
                        //ll = 0;
                       // this.textBox1.Text = "";
                        buffer.RemoveRange(0, buffer.Count);
                        //this.port1.Close();
                        cleanMsg();
                    }
                    sb.Length = 0;
                    
                }));               
            }
            else
            {
                return;
            }

        }

        public void checkBack(string rev,int s)
        {
            
            if (s==0)
            {
                this.Invoke((EventHandler)(delegate
                {
                    this.label13.ForeColor = Color.Red;
                    this.label13.Text = "回发命令为空！";
                }));                
            }
            else if(cbreg.Checked == true)
            {
                if (tbreg0.Text.Trim().Length != 0 && rev.Contains(tbreg0.Text.Trim().Replace(" ", "")) || tbreg0.Text.Trim().Replace(" ", "") == "*" || tbreg0.Text.Trim().Replace(" ", "") == "?")
                {
                    r0++;
                    sendMsg(bkcmd0.Text.Trim().Replace(" ", ""), 0);
                }
                if (tbreg1.Text.Trim().Length != 0 && rev.Contains(tbreg1.Text.Trim().Replace(" ", "")) || tbreg1.Text.Trim().Replace(" ", "") == "*" || tbreg1.Text.Trim().Replace(" ", "") == "?")
                {
                    r1++;
                    sendMsg(bkcmd1.Text.Trim().Replace(" ", ""), 1);
                }
                if (tbreg2.Text.Trim().Length != 0 && rev.Contains(tbreg2.Text.Trim().Replace(" ", "")) || tbreg2.Text.Trim().Replace(" ", "") == "*" || tbreg2.Text.Trim().Replace(" ", "") == "?")
                {
                    r2++;
                    sendMsg(bkcmd2.Text.Trim().Replace(" ", ""), 2);
                }
                if (tbreg3.Text.Trim().Length != 0 && rev.Contains(tbreg3.Text.Trim().Replace(" ", "")) || tbreg3.Text.Trim().Replace(" ", "") == "*" || tbreg3.Text.Trim().Replace(" ", "") == "?")
                {
                    r3++;
                    sendMsg(bkcmd3.Text.Trim().Replace(" ", ""), 3);
                }
                if (tbreg4.Text.Trim().Length != 0 && rev.Contains(tbreg4.Text.Trim().Replace(" ", "")) || tbreg4.Text.Trim().Replace(" ", "") == "*" || tbreg4.Text.Trim().Replace(" ", "") == "?")
                {
                    r4++;
                    sendMsg(bkcmd4.Text.Trim().Replace(" ", ""), 4);
                }
                if (tbreg5.Text.Trim().Length != 0 && rev.Contains(tbreg5.Text.Trim().Replace(" ", "")) || tbreg5.Text.Trim().Replace(" ", "") == "*" || tbreg5.Text.Trim().Replace(" ", "") == "?")
                {
                    r5++;
                    sendMsg(bkcmd5.Text.Trim().Replace(" ", ""), 5);
                }
            }
            else
            {
                if (tbreg0.Text.Trim().Length != 0 && rev.Contains(tbreg0.Text.Trim())|| tbreg0.Text.Trim()=="*"|| tbreg0.Text.Trim()=="?")
                {
                    r0++;
                    sendMsg(bkcmd0.Text.Trim(),0);                                                            
                }
                if (tbreg1.Text.Trim().Length != 0 && rev.Contains(tbreg1.Text.Trim()) || tbreg1.Text.Trim() == "*" || tbreg1.Text.Trim() == "?")
                {
                    r1++;
                    sendMsg(bkcmd1.Text.Trim(),1);                    
                }
                if (tbreg2.Text.Trim().Length != 0 && rev.Contains(tbreg2.Text.Trim()) || tbreg2.Text.Trim() == "*" || tbreg2.Text.Trim() == "?")
                {
                    r2++;
                    sendMsg(bkcmd2.Text.Trim(),2);                    
                }
                if (tbreg3.Text.Trim().Length != 0 && rev.Contains(tbreg3.Text.Trim()) || tbreg3.Text.Trim() == "*" || tbreg3.Text.Trim() == "?")
                {
                    r3++;
                    sendMsg(bkcmd3.Text.Trim(),3);                    
                }
                if (tbreg4.Text.Trim().Length != 0 && rev.Contains(tbreg4.Text.Trim()) || tbreg4.Text.Trim() == "*" || tbreg4.Text.Trim() == "?")
                {
                    r4++;
                    sendMsg(bkcmd4.Text.Trim(),4);                    
                }
                if (tbreg5.Text.Trim().Length != 0&&rev.Contains(tbreg5.Text.Trim()) || tbreg5.Text.Trim() == "*" || tbreg5.Text.Trim() == "?")
                {
                    r5++;
                    sendMsg(bkcmd5.Text.Trim(),5);                    
                }
            }
        }
        public void sendMsg(string str,int n)
        {
           if(str.Length==0)
            {
                this.Invoke((EventHandler)(delegate
                {
                    this.label13.ForeColor = Color.Red;
                    this.label13.Text = "回发命令"+(n+1).ToString()+"为空！";
                }));
                return;
            }
           else
            {                
                byte[] str2hex;
                if(cbreg.Checked==true)
                {
                    str2hex = strToToHexByte(str);
                    port1.Write(str2hex, 0, str2hex.Length);                    
                }
                else
                {
                    str2hex = cmd(str);
                    port1.Write(str2hex, 0, str2hex.Length);
                }
                sdl += str2hex.Length;
                switch(n)
                {
                    case 0:
                        s0++;
                        break;
                    case 1:
                        s1++;
                        break;
                    case 2:
                        s2++;
                        break;
                    case 3:
                        s3++;
                        break;
                    case 4:
                        s4++;
                        break;
                    case 5:
                        s5++;
                        break;
                }
                this.Invoke((EventHandler)(delegate
                {
                    //this.label13.ForeColor = Color.Red;
                    this.label17.Text = sdl.ToString();
                    this.label26.Text = r0.ToString();
                    this.label27.Text = r1.ToString();
                    this.label28.Text = r2.ToString();
                    this.label29.Text = r3.ToString();
                    this.label30.Text = r4.ToString();
                    this.label31.Text = r5.ToString();
                    this.label32.Text = s0.ToString();
                    this.label33.Text = s1.ToString();
                    this.label34.Text = s2.ToString();
                    this.label35.Text = s3.ToString();
                    this.label36.Text = s4.ToString();
                    this.label37.Text = s5.ToString();
                    this.textBox2.AppendText(string.Format("{0:MM-dd HH:mm:ss ffff}", DateTime.Now) + "\tS:" + str2hex.Length.ToString() + "字节数据\t回馈命令"+(n++).ToString()+"\r\n");
                }));
            }
        }
        public byte[] cmd(string cmd)
        {
            byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(cmd);
            return byteArray;
        }
        public void saveTxt(string dt)
        {
            string fp = Application.StartupPath + @"\" + dt + ".txt";
            FileStream fs = new FileStream(fp, FileMode.Create, FileAccess.Write);//创建写入文件 
            string txt = textBox1.Text;
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(txt);
            sw.Close();//写入
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.ScrollToCaret();
        }

        public string buf2hex(byte[] buf)
        {
            string str = "";
            for(int i=0;i<buf.Length;i++)
            {
                str += buf[i].ToString("X2");
            }
            return str;
        }

        public static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
            {
                hexString += " ";
            }
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
            {
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return returnBytes;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cleanMsg();
        }
        public void cleanMsg()
        {
            label13.ForeColor = Color.Green;
            if (checkBox3.Checked == true)
            {
                string dt = "cl" + string.Format("{0:yyyyMMddHHmmssffff}", DateTime.Now);
                saveTxt(dt);
                label13.Text = "清除成功！共" + ll.ToString() + "行，记录保存文件：" + dt.ToString() + ".txt";
            }
            else
            {
                label13.Text = "清除成功！共" + ll.ToString() + "行，记录未保存";
            }
            this.textBox1.Text = "";
            this.textBox2.Text = "";
            ll = 0;
            sdl = 0;
            //总数据接收长度
            rdl = 0;
            //命令发送次数
            s0 = 0;
            s1 = 0;
            s2 = 0;
            s3 = 0;
            s4 = 0;
            s5 = 0;
            //命令接收次数
            r0 = 0;
            r1 = 0;
            r2 = 0;
            r3 = 0;
            r4 = 0;
            r5 = 0;
            label15.Text = "0";
            label17.Text = "0";
            label26.Text = r0.ToString();
            label27.Text = r1.ToString();
            label28.Text = r2.ToString();
            label29.Text = r3.ToString();
            label30.Text = r4.ToString();
            label31.Text = r5.ToString();
            label32.Text = s0.ToString();
            label33.Text = s1.ToString();
            label34.Text = s2.ToString();
            label35.Text = s3.ToString();
            label36.Text = s4.ToString();
            label37.Text = s5.ToString();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            string dt = "sv" + string.Format("{0:yyyyMMddHHmmssffff}", DateTime.Now);
            saveTxt(dt);
            label13.ForeColor = Color.Green;
            label13.Text = "保存成功！共" + ll.ToString() + "行，文件："+dt.ToString()+".txt";
            //ll = 0;
            //this.textBox1.Text = "";
        }      

        private void checkBox2_Changed(object sender, EventArgs e)
        {           
                checkBox1.Checked = checkBox2.Checked;           
        }
    }
}
