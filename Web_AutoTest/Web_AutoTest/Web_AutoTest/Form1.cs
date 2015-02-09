using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using SE190X;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        // Boolean flag used to determine when a character other than a number is entered.
        private bool nonNumberEntered = false;

        private bool STOP_WHEN_FAIL = false;
        private bool WAIT = false;
        private bool Run_Stop;

        TcpClient telnet = new TcpClient();
        NetworkStream telentStream; //宣告網路資料流變數
        byte[] bytWrite_telnet;
        byte[] bytRead_telnet;
        public Thread rcvThread;

        TcpListener Web_listener;
        public Thread listenThread;
        TcpClient Web_client;
        string LCdata;

        UdpClient udpClient;
        Thread udpRcvThread;
        public const int UDPport = 55954;
        public static int Device_Max = 199; // 599
        public static int DevCount;
        public static int DevSetCount1;
        public static int DevSetCount2;
        public static int DevDefaultCount;
        public static int DevUpgradeCount;
        public static string[,] DA2 = new string[Device_Max, 24];
        public static string[,] DA2_compare = new string[Device_Max, 24];
        public static int localendport;
        public static string[] stringData = new string[Device_Max];

        // Ping
        System.Net.NetworkInformation.Ping objping = new System.Net.NetworkInformation.Ping();

        // C#取得主程式路徑(Application Path)
        string appPATH = Application.StartupPath;

        string fnameTmp;
        string MODEL_NAME, TARGET_IP;   // MODEL_NAME 程式測試&判斷使用，由文字檔檔名決定，強制大寫
        string model_name;              // model_name 出廠設定使用，由文字檔內文第一行決定，強制大寫
        static uint dev_num = 50;

        // new:建構ArrayList物件
        ArrayList TEST_STATUS = new ArrayList(50); // 0:未測試,1:PASS,2:fail,3:error

        ArrayList TEST_FunLog = new ArrayList(50);
        int idx_funlog;
        string[] TEST_RESULT;

        public Label[] lblFunction = new Label[dev_num];

        string COM_function, CAN_functiom, CAN_loopback, WaitKey, USR, PWD;
        string data;
        int TestFun_MaxIdx;
        int row_num;
        int MOUSE_Idx, Test_Idx;
        DateTime time;
        Process proc;
        int[] COM_PID = new int[2];
        int secretX;
        bool chooseStart = false;
        string tester_forExcel, productNum_forExcel, coreSN_forExcel, lanSN_forExcel, uartSN_forExcel, serial1SN_forExcel, serial2SN_forExcel, serial3SN_forExcel, serial4SN_forExcel;
        string startTime, endTime;
        string tmpIP, tmpNetmask, tmpMAC, tmpModel, tmpKernel, tmpAP, tmphostname;

        string rxContents;
        string rxContents_EUT;

        public Form1()
        {
            InitializeComponent();

            // 表單中的焦點永遠在某個控制項上
            //this.Activated += new EventHandler(delegate(object o, EventArgs e)
            //{
            //    this.txt_Tx.Focus();
            //});
            //this.txt_Tx.Leave += new EventHandler(delegate(object o, EventArgs e)
            //{
            //    this.txt_Tx.Focus();
            //});
        }

        public delegate void myUICallBack(string myStr, TextBox txt); // delegate 委派；Invoke 調用

        /// <summary>
        /// 更新主線程的UI (txt_Rx.text) = Display
        /// </summary>
        /// <param name="myStr">字串</param>
        /// <param name="txt">指定的控制項，限定有Text屬性</param>
        public void myUI(string myStr, TextBox txt)
        {
            if (txt.InvokeRequired)    // if (this.InvokeRequired)
            {
                myUICallBack myUpdate = new myUICallBack(myUI);
                this.Invoke(myUpdate, myStr, txt);
            }
            else
            {
                int i;
                string[] line;
                int ptr = myStr.IndexOf("\r\n", 0); // vb6: ptr = InStr(1, keyword, vbCrLf, vbTextCompare)
                //Debug.Print(ptr.ToString());
                if (ptr == -1)  // Instr與IndexOf的起始位置不同，結果的表達也不同(參見MSDN)
                {
                    ptr = myStr.IndexOf(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString(), 0); // ←[J
                    if (ptr != -1)
                        ptr = ptr + 2;
                }
                // 判斷 txt_Rx.Text 中的字串是否超出最大長度
                if (txt.Text.Length + myStr.Length >= txt.MaxLength)
                {
                    if (myStr.Length >= txt.MaxLength)
                        //txt.Text = myStr.Substring(myStr.Length - 1 - txt.MaxLength, txt.MaxLength); // 右邊(S.Length-1-指定長度，指定長度)
                        txt.Text = myStr.Substring((myStr.Length - txt.MaxLength));
                    else if (txt.Text.Length >= myStr.Length)
                        //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.Text.Length - myStr.Length), (txt.Text.Length - myStr.Length));
                        txt.Text = txt.Text.Substring((txt.Text.Length - (txt.Text.Length - myStr.Length)));
                    else
                        //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.MaxLength - myStr.Length), (txt.MaxLength - myStr.Length));
                        txt.Text = txt.Text.Substring((txt.Text.Length - (txt.MaxLength - myStr.Length)));
                }
                txt.Text = txt.Text + myStr;

                // 處理((char)8)，例如開機倒數321訊息
                //int ptr1 = myStr.IndexOf(((char)8).ToString(), 0);
                //if (ptr1 != -1)
                //{
                //    while (((txt_Rx.Text.IndexOf(((char)8).ToString(), 0) + 1) > 0))
                //    {
                //        ptr1 = (txt_Rx.Text.IndexOf(((char)8).ToString(), 0) + 1);
                //        if ((ptr1 > 1))
                //        {
                //            txt_Rx.Text = (txt_Rx.Text.Substring(0, (ptr1 - 2)) + txt_Rx.Text.Substring((txt_Rx.Text.Length - (txt_Rx.Text.Length - ptr1))));
                //        }
                //        else
                //        {
                //            txt_Rx.Text = (txt_Rx.Text.Substring(0, (ptr1 - 1)) + txt_Rx.Text.Substring((txt_Rx.Text.Length - (txt_Rx.Text.Length - ptr1))));
                //        }
                //    }
                //}

                data = data + myStr;
                //Console.WriteLine(data);
                if (ptr == -1 || ptr == 0)
                {
                    return;
                }

                // 處理終端機上下鍵的動作(顯示上一個指令)
                if (myStr.IndexOf(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString()) != -1)
                {
                    line = txt_Rx.Text.Split('\r');
                    txt_Rx.Text = string.Empty;     // 文字會重複的問題
                    string Rx_tmp = string.Empty;   // Rx_tmp 解決卷軸滾動視覺效果
                    for (i = 1; i < line.GetUpperBound(0) - 1; i++)
                    {
                        Rx_tmp = Rx_tmp + "\r\n" + line[i];
                    }
                    txt_Rx.Text = Rx_tmp + "\r\n" + line[i + 1].Replace(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString(), "");
                }

                // 開機完，自動輸入USR、PWD
                if (WAIT)
                {
                    if (WaitKey == null)
                    {
                        WaitKey = string.Empty;
                    }
                    else if (WaitKey != string.Empty)
                    {
                        if (data.Contains(WaitKey))
                        {
                            if (WaitKey.Equals("login", StringComparison.OrdinalIgnoreCase))
                            {
                                if (serialPort1.IsOpen)
                                {
                                    //serialPort1.DiscardOutBuffer(); // 捨棄序列驅動程式傳輸緩衝區的資料
                                    if (!String.IsNullOrEmpty(USR)) //USR!=null || USR!=string.empty
                                    {
                                        serialPort1.Write(USR + ((char)13).ToString());
                                        System.Threading.Thread.Sleep(100);
                                        serialPort1.Write(PWD + ((char)13).ToString());
                                    }
                                    else
                                    {
                                        serialPort1.Write("root" + ((char)13).ToString());
                                        System.Threading.Thread.Sleep(100);
                                        serialPort1.Write("root" + ((char)13).ToString());
                                    }
                                }
                            }
                            WaitKey = string.Empty;
                            WAIT = false;
                        }
                    }
                }
                data = myStr.Substring((myStr.Length - (myStr.Length - ptr)));
                //Debug.Print(data);
            }
        }

        private void serialPort1_Display(object sender, EventArgs e)
        {
            int i;
            string[] line;
            int ptr = rxContents.IndexOf("\r\n", 0); // vb6: ptr = InStr(1, keyword, vbCrLf, vbTextCompare)
            //Debug.Print(ptr.ToString());
            if (ptr == -1)  // Instr與IndexOf的起始位置不同，結果的表達也不同(參見MSDN)
            {
                ptr = rxContents.IndexOf(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString(), 0); // ←[J
                if (ptr != -1)
                    ptr = ptr + 2;
            }
            // 判斷 txt_Rx.Text 中的字串是否超出最大長度
            if (txt_Rx.Text.Length + rxContents.Length >= txt_Rx.MaxLength)
            {
                if (rxContents.Length >= txt_Rx.MaxLength)
                    //txt.Text = myStr.Substring(myStr.Length - 1 - txt.MaxLength, txt.MaxLength); // 右邊(S.Length-1-指定長度，指定長度)
                    txt_Rx.Text = rxContents.Substring((rxContents.Length - txt_Rx.MaxLength));
                else if (txt_Rx.Text.Length >= rxContents.Length)
                    //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.Text.Length - myStr.Length), (txt.Text.Length - myStr.Length));
                    txt_Rx.Text = txt_Rx.Text.Substring((txt_Rx.Text.Length - (txt_Rx.Text.Length - rxContents.Length)));
                else
                    //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.MaxLength - myStr.Length), (txt.MaxLength - myStr.Length));
                    txt_Rx.Text = txt_Rx.Text.Substring((txt_Rx.Text.Length - (txt_Rx.MaxLength - rxContents.Length)));
            }
            txt_Rx.Text = txt_Rx.Text + rxContents;

            // 處理((char)8)，例如開機倒數321訊息
            int ptr1 = rxContents.IndexOf(((char)8).ToString(), 0);
            if (ptr1 != -1)
            {
                while (((txt_Rx.Text.IndexOf(((char)8).ToString(), 0) + 1) > 0))
                {
                    ptr1 = (txt_Rx.Text.IndexOf(((char)8).ToString(), 0) + 1);
                    if ((ptr1 > 1))
                    {
                        txt_Rx.Text = (txt_Rx.Text.Substring(0, (ptr1 - 2)) + txt_Rx.Text.Substring((txt_Rx.Text.Length - (txt_Rx.Text.Length - ptr1))));
                        //Debug.Print(txt_Rx.Text);
                    }
                    else
                    {
                        txt_Rx.Text = (txt_Rx.Text.Substring(0, (ptr1 - 1)) + txt_Rx.Text.Substring((txt_Rx.Text.Length - (txt_Rx.Text.Length - ptr1))));
                        //Debug.Print(txt_Rx.Text);
                    }
                }
            }

            data = data + rxContents;
            //Console.WriteLine(data);
            if (ptr == -1 || ptr == 0)
            {
                return;
            }

            // 處理終端機上下鍵的動作(顯示上一個指令)
            if (rxContents.IndexOf(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString()) != -1)
            {
                line = txt_Rx.Text.Split('\r');
                txt_Rx.Text = string.Empty;     // 文字會重複的問題
                string Rx_tmp = string.Empty;   // Rx_tmp 解決卷軸滾動視覺效果
                for (i = 1; i < line.GetUpperBound(0) - 1; i++)
                {
                    Rx_tmp = Rx_tmp + "\r\n" + line[i];
                }
                txt_Rx.Text = Rx_tmp + "\r\n" + line[i + 1].Replace(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString(), "");
            }

            // 開機完，自動輸入USR、PWD
            if (WAIT)
            {
                if (WaitKey == null)
                {
                    WaitKey = string.Empty;
                }
                else if (WaitKey != string.Empty)
                {
                    if (data.Contains(WaitKey))
                    {
                        if (WaitKey.Equals("login", StringComparison.OrdinalIgnoreCase))
                        {
                            if (serialPort1.IsOpen)
                            {
                                //serialPort1.DiscardOutBuffer(); // 捨棄序列驅動程式發送的緩衝區的資料
                                if (!String.IsNullOrEmpty(USR)) //USR!=null || USR!=string.empty
                                {
                                    serialPort1.Write(USR + ((char)13).ToString());
                                    System.Threading.Thread.Sleep(100);
                                    serialPort1.Write(PWD + ((char)13).ToString());
                                }
                                else
                                {
                                    serialPort1.Write("root" + ((char)13).ToString());
                                    System.Threading.Thread.Sleep(100);
                                    serialPort1.Write("root" + ((char)13).ToString());
                                }
                            }
                        }
                        WaitKey = string.Empty;
                        WAIT = false;       // debug: check "data"
                    }
                }
            }
            data = rxContents.Substring((rxContents.Length - (rxContents.Length - ptr)));
            //Debug.Print(data);
        }

        private void serialPort2_Display(object sender, EventArgs e)
        {
            int i;
            string[] line;
            int ptr = rxContents_EUT.IndexOf("\r\n", 0); // vb6: ptr = InStr(1, keyword, vbCrLf, vbTextCompare)
            //Debug.Print(ptr.ToString());
            if (ptr == -1)  // Instr與IndexOf的起始位置不同，結果的表達也不同(參見MSDN)
            {
                ptr = rxContents_EUT.IndexOf(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString(), 0); // ←[J
                if (ptr != -1)
                    ptr = ptr + 2;
            }
            // 判斷 txt_Rx_EUT.Text 中的字串是否超出最大長度
            if (txt_Rx_EUT.Text.Length + rxContents_EUT.Length >= txt_Rx_EUT.MaxLength)
            {
                if (rxContents_EUT.Length >= txt_Rx_EUT.MaxLength)
                    //txt.Text = myStr.Substring(myStr.Length - 1 - txt.MaxLength, txt.MaxLength); // 右邊(S.Length-1-指定長度，指定長度)
                    txt_Rx_EUT.Text = rxContents_EUT.Substring((rxContents_EUT.Length - txt_Rx_EUT.MaxLength));
                else if (txt_Rx_EUT.Text.Length >= rxContents_EUT.Length)
                    //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.Text.Length - myStr.Length), (txt.Text.Length - myStr.Length));
                    txt_Rx_EUT.Text = txt_Rx_EUT.Text.Substring((txt_Rx_EUT.Text.Length - (txt_Rx_EUT.Text.Length - rxContents_EUT.Length)));
                else
                    //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.MaxLength - myStr.Length), (txt.MaxLength - myStr.Length));
                    txt_Rx_EUT.Text = txt_Rx_EUT.Text.Substring((txt_Rx_EUT.Text.Length - (txt_Rx_EUT.MaxLength - rxContents_EUT.Length)));
            }
            txt_Rx_EUT.Text = txt_Rx_EUT.Text + rxContents_EUT;

            // 處理((char)8)，例如開機倒數321訊息
            int ptr1 = rxContents_EUT.IndexOf(((char)8).ToString(), 0);
            if (ptr1 != -1)
            {
                while (((txt_Rx_EUT.Text.IndexOf(((char)8).ToString(), 0) + 1) > 0))
                {
                    ptr1 = (txt_Rx_EUT.Text.IndexOf(((char)8).ToString(), 0) + 1);
                    if ((ptr1 > 1))
                    {
                        txt_Rx_EUT.Text = (txt_Rx_EUT.Text.Substring(0, (ptr1 - 2)) + txt_Rx_EUT.Text.Substring((txt_Rx_EUT.Text.Length - (txt_Rx_EUT.Text.Length - ptr1))));
                        //Debug.Print(txt_Rx_EUT.Text);
                    }
                    else
                    {
                        txt_Rx_EUT.Text = (txt_Rx_EUT.Text.Substring(0, (ptr1 - 1)) + txt_Rx_EUT.Text.Substring((txt_Rx_EUT.Text.Length - (txt_Rx_EUT.Text.Length - ptr1))));
                        //Debug.Print(txt_Rx_EUT.Text);
                    }
                }
            }

            data = data + rxContents_EUT;
            //Console.WriteLine(data);
            if (ptr == -1 || ptr == 0)
            {
                return;
            }

            // 處理終端機上下鍵的動作(顯示上一個指令)
            if (rxContents_EUT.IndexOf(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString()) != -1)
            {
                line = txt_Rx_EUT.Text.Split('\r');
                txt_Rx_EUT.Text = string.Empty;     // 文字會重複的問題
                string Rx_tmp = string.Empty;   // Rx_tmp 解決卷軸滾動視覺效果
                for (i = 1; i < line.GetUpperBound(0) - 1; i++)
                {
                    Rx_tmp = Rx_tmp + "\r\n" + line[i];
                }
                txt_Rx_EUT.Text = Rx_tmp + "\r\n" + line[i + 1].Replace(((char)27).ToString() + ((char)91).ToString() + ((char)74).ToString(), "");
            }

            // 開機完，自動輸入USR、PWD
            if (WAIT)
            {
                if (WaitKey == null)
                {
                    WaitKey = string.Empty;
                }
                else if (WaitKey != string.Empty)
                {
                    if (data.Contains(WaitKey))
                    {
                        if (WaitKey.Equals("login", StringComparison.OrdinalIgnoreCase))
                        {
                            if (serialPort1.IsOpen)
                            {
                                //serialPort1.DiscardOutBuffer(); // 捨棄序列驅動程式發送的緩衝區的資料
                                if (!String.IsNullOrEmpty(USR)) //USR!=null || USR!=string.empty
                                {
                                    serialPort1.Write(USR + ((char)13).ToString());
                                    System.Threading.Thread.Sleep(100);
                                    serialPort1.Write(PWD + ((char)13).ToString());
                                }
                                else
                                {
                                    serialPort1.Write("root" + ((char)13).ToString());
                                    System.Threading.Thread.Sleep(100);
                                    serialPort1.Write("root" + ((char)13).ToString());
                                }
                            }
                        }
                        WaitKey = string.Empty;
                        WAIT = false;       // debug: check "data"
                    }
                }
            }
            data = rxContents_EUT.Substring((rxContents_EUT.Length - (rxContents_EUT.Length - ptr)));
            //Debug.Print(data);
        }

        public void RecNote(int idx, string note)
        {
            string tmpNote = string.Empty;
            DateTime time = DateTime.Now;
            tmpNote = String.Format("{0:00}:{1:00}:{2:00}", time.Hour, time.Minute, time.Second) + " [" + lblFunction[idx].Tag + "]" + ": " + note + "\r\n";
            noteUI(tmpNote, txt_Note);
        }

        public delegate void noteUICallBack(string myStr, TextBox txt); // delegate 委派；Invoke 調用

        /// <summary>
        /// 更新主線程的UI (txt_Note.text)
        /// </summary>
        /// <param name="myStr">字串</param>
        /// <param name="txt">指定的控制項，限定有Text屬性</param>
        public void noteUI(string myStr, TextBox txt)
        {
            if (txt.InvokeRequired)    // if (this.InvokeRequired)
            {
                noteUICallBack myUpdate = new noteUICallBack(noteUI);
                this.Invoke(myUpdate, myStr, txt);
            }
            else
            {
                // 判斷 txt.Text 中的字串是否超出最大長度
                if (txt.Text.Length + myStr.Length >= txt.MaxLength)
                {
                    if (myStr.Length >= txt.MaxLength)
                        //txt.Text = myStr.Substring(myStr.Length - 1 - txt.MaxLength, txt.MaxLength); // 右邊(S.Length-1-指定長度，指定長度)
                        txt.Text = myStr.Substring((myStr.Length - txt.MaxLength));
                    else if (txt.Text.Length >= myStr.Length)
                        //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.Text.Length - myStr.Length), (txt.Text.Length - myStr.Length));
                        txt.Text = txt.Text.Substring((txt.Text.Length - (txt.Text.Length - myStr.Length)));
                    else
                        //txt.Text = txt.Text.Substring(txt.Text.Length - 1 - (txt.MaxLength - myStr.Length), (txt.MaxLength - myStr.Length));
                        txt.Text = txt.Text.Substring((txt.Text.Length - (txt.MaxLength - myStr.Length)));
                }
                txt.Text = txt.Text + myStr;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;   // 漏斗指標
            consoleToolStripMenuItem_CheckStateChanged(null, null);

            // 獲取電腦的有效串口
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cmbDutCom.Items.Add(port);
                cmbEutCom.Items.Add(port);
            }
            cmbDutCom.Sorted = true;
            cmbDutCom.SelectedIndex = 0;
            cmbEutCom.Sorted = true;
            cmbEutCom.SelectedIndex = 1;

            if (IsIP(txtDutIP.Text))
            {
                TARGET_IP = txtDutIP.Text;
            }
            //if (IsIP(txtEutIP.Text))
            //{
            //    TARGET_eutIP = txtEutIP.Text;
            //}

            this.Cursor = Cursors.Default;      // 還原預設指標
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            serialPort1_Close();
            //serialPort2_Close();
            if (telnet.Connected) { telnet.Close(); }
            Application.Exit();
        }

        #region Shell

        private int Shell(string FilePath, string FileName)
        {
            try
            {
                ////////////////////// like VB 【shell】 ///////////////////////
                //System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc = new Process();
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                proc.EnableRaisingEvents = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.FileName = FilePath + "\\" + FileName;
                proc.Start();
                return proc.Id;
                ////////////////////////////////////////////////////////////////
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " ' " + FileName + " ' ", "Shell error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Run python(.py)
        /// </summary>
        /// <param name="Command">Python file(.py)</param>
        /// <returns></returns>
        //public String run(String Command)
        public void runpy(String Command)
        {
            //String Output = null;
            tabControl1.SelectedTab = tabPage3;
            Hold(10);
            if (Command != null && !Command.Equals(""))
            {
                Process process = new Process();
                //process.EnableRaisingEvents = false;
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "cmd.exe";
                // no create the cmd windows
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardInput = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.UseShellExecute = false;
                //processStartInfo.WorkingDirectory =  ;

                process.StartInfo = processStartInfo;

                try
                {
                    process.Start();
                    process.StandardInput.WriteLine(Command);
                    process.StandardInput.WriteLine("exit");
                    //process.WaitForExit(15 * 1000);
                    //Output = process.StandardOutput.ReadToEnd();
                }
                catch (Exception)
                {
                    process.Close();
                    //return e.ToString();
                }
                finally
                {
                    process.Close();
                }
            }
            //return ContextFilter(Output);
            //return Output;
        }

        public String ContextFilter(String Output)
        {
            Regex regex_end = new Regex("^[^^]*#end");
            Match match = regex_end.Match(Output);
            Regex regex_begin = new Regex("^[^^]*?#begin\r\n");
            String result = regex_begin.Replace(match.Value, "");
            Regex regex_tar = new Regex("\r\n#end$");
            result = regex_tar.Replace(result, "");
            return result;
        }

        private void CloseShell(int pid)
        {
            //if (!Process.GetProcessById(pid).HasExited)
            //{
            //    // Close process by sending a close message to its main window.
            //    Process.GetProcessById(pid).CloseMainWindow();
            //    Process.GetProcessById(pid).WaitForExit(3000);
            //}
            if (!Process.GetProcessById(pid).HasExited)
            {
                Process.GetProcessById(pid).Kill();
                Process.GetProcessById(pid).WaitForExit(1000);
            }
        }

        #endregion Shell

        private void cmdOpeFile_Click(object sender, EventArgs e)
        {
            string[] cmd;
            int n = 0;
            String line;
            STOP_WHEN_FAIL = false;

            openFileDialog1.FileName = string.Empty;
            openFileDialog1.Multiselect = false;
            openFileDialog1.InitialDirectory = appPATH;
            openFileDialog1.Filter = "純文字檔(*.txt)|*.txt|All(*.*)|*.*";
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    fnameTmp = openFileDialog1.SafeFileName;
                    //fnameTmp = openFileDialog1.FileName.Replace(appPATH + "\\", string.Empty);
                    cmdOpeFile.Text = fnameTmp;

                    // Pass the file path and file name to the StreamReader constructor
                    using (StreamReader sr = new StreamReader(fnameTmp, Encoding.ASCII))
                    {
                        // 1. Read the first line of text
                        line = sr.ReadLine();
                        cmd = line.Split(' ');
                        if (cmd.GetUpperBound(0) < 1)
                        {
                            MessageBox.Show("檔案第一行錯誤，格式應該為 Model IP User Password ", "Error Message");
                            sr.Close();
                            return;
                        }
                        else
                            if (!IsIP(cmd[1]))
                            {
                                MessageBox.Show("檔案第一行錯誤，請檢查 IP 是否輸入正確 ", "Error Message");
                                sr.Close();
                                return;
                            }
                        Shell(appPATH, "arp-d.bat");
                        // model_name 出廠設定使用，由文字檔內文第一行決定，強制大寫
                        model_name = cmd[0].ToUpper();
                        // MODEL_NAME 程式測試&判斷使用，由文字檔檔名決定，強制大寫
                        MODEL_NAME = (fnameTmp.Replace(".txt", string.Empty)).ToUpper();
                        TARGET_IP = cmd[1];
                        USR = cmd[2];
                        if (cmd.GetUpperBound(0) > 2) { PWD = cmd[3]; }
                        else { PWD = string.Empty; }

                        this.Text = MODEL_NAME + "   Web Auto-test";
                        chkLoop.Checked = false;
                        Test_Idx = 0;
                        Run_Stop = true;
                        //SYSTEM = 0;
                        serialPort1_Close();
                        //serialPort2_Close();
                        if (telnet.Connected) { telnet.Close(); }

                        MappingFunction();

                        RemoveControl(TestFun_MaxIdx);   // Initial Label
                        txt_Note.Text = string.Empty;
                        txt_Rx.Text = string.Empty;
                        //txt_WebRx.Text = string.Empty;
                        TEST_STATUS.Clear();    // 將所有元素移除(Initial)
                        TEST_FunLog.Clear();

                        // 2. Continue to read until you reach end of file
                        line = sr.ReadLine();
                        while (line != null)
                        {
                            if (line != string.Empty)
                            {
                                cmd = line.Split(' ');
                                switch (cmd[0].ToUpper())
                                {
                                    case "BUZZER":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("BUZZER");
                                        break;
                                    case "CONSOLE-DUT":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0); // 0:將TEST_STATUS狀態設定為未測試
                                        break;
                                    case "CONSOLE-EUT":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0); // 0:將TEST_STATUS狀態設定為未測試
                                        break;
                                    case "COM":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("COM");
                                        break;
                                    case "COMTOCOM":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("COMTOCOM");
                                        break;
                                    case "DELETE":  // delete files in jffs2
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        break;
                                    case "INVITE":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("INVITE");
                                        break;
                                    case "LOADTOOLS":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        break;
                                    case "EMAIL":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("EMAIL");
                                        break;
                                    case "RESTART":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("RESTART");
                                        break;
                                    case "RTC":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("RTC");
                                        break;
                                    case "SLEEP":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        break;
                                    case "SYSTEM":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        break;
                                    case "STOP":
                                        if (cmd[1].ToUpper() == "WHEN" && cmd[2].ToUpper() == "FAIL")
                                            STOP_WHEN_FAIL = true;
                                        break;
                                    case "TELNET":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("TELNET");
                                        break;
                                    case "WATCHDOG":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        TEST_FunLog.Add("WATCHDOG");
                                        break;
                                    case "WEB":
                                        tabControl1.SelectedTab = tabPage3;
                                        if (listenThread == null)
                                        {
                                            listenThread = new Thread(new ThreadStart(ListenForClients));
                                            listenThread.IsBackground = true;
                                            listenThread.Start();
                                        }
                                        break;
                                    case "RUN":
                                        AddFunction(line, cmd[0], n);
                                        n = n + 1;
                                        TEST_STATUS.Add(0);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            // 3. Read the next line
                            line = sr.ReadLine();
                        }
                        // 4. close the file
                        sr.Close();
                    }
                    if (n == 0)
                        return;
                    else
                        TestFun_MaxIdx = n;
                    composingTmr.Enabled = true;
                    TEST_STATUS.TrimToSize();   // TrimToSize():將容量設為實際元素數目
                    TEST_FunLog.TrimToSize();
                    TEST_RESULT = new string[TEST_FunLog.Count];

                    cmdOpeFile.Text = fnameTmp.Replace(".txt", string.Empty);

                    web_ip_text(TARGET_IP, USR, PWD);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "");
            }
            finally
            {
                //Debug.Print("STOP_WHEN_FAIL = " + STOP_WHEN_FAIL.ToString());
                //Debug.Print("測試陣列的大小 : " + lblFun_MaxIdx.ToString());
            }
        }

        private void web_ip_text(string IP, string user, string password)
        {
            // 建立檔案
            FileStream fs = File.Open(appPATH + "\\web_ip.txt", FileMode.OpenOrCreate, FileAccess.Write);
            // 建構StreamWriter物件
            StreamWriter sw = new StreamWriter(fs);

            // 寫入
            sw.WriteLine(IP);
            sw.WriteLine(user);
            sw.WriteLine(password);

            // 清除目前寫入器(Writer)的所有緩衝區，並且造成任何緩衝資料都寫入基礎資料流
            sw.Flush();

            // 關閉目前的StreamWriter物件和基礎資料流
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// 新增控制項 lblFunction
        /// </summary>
        /// <param name="dat">檔案名稱的設定內容，給Tag屬性</param>
        /// <param name="item_name">測試項名稱，給Text屬性</param>
        /// <param name="n">控制項陣列的索引標籤，給TabIndex屬性</param>
        public void AddFunction(string dat, string item_name, int n)
        {
            lblFunction[n] = new Label();
            lblFunction[n].AutoSize = true;
            lblFunction[n].TextAlign = ContentAlignment.MiddleCenter;
            lblFunction[n].Font = new Font("Arial", 12, FontStyle.Bold); // new Font(字型, 大小, 樣式);
            lblFunction[n].BorderStyle = BorderStyle.FixedSingle;
            lblFunction[n].Enabled = true;
            lblFunction[n].Location = new Point(12, 48);
            lblFunction[n].Visible = false;
            lblFunction[n].Tag = dat;
            lblFunction[n].BackColor = Color.FromArgb(255, 255, 255);
            lblFunction[n].Text = item_name.Substring(0, 1).ToUpper() + item_name.Substring(1, item_name.Length - 1);
            // TabIndex => ((Label)sender).TabIndex
            lblFunction[n].TabIndex = n;
            //splitContainer1.Panel1.Controls.Add(lblFunction[n]);
            tabPage5.Controls.Add(lblFunction[n]);
            // 註冊事件
            lblFunction[n].MouseMove += new MouseEventHandler(lblFunction_MouseMove);
            lblFunction[n].MouseLeave += new EventHandler(lblFunction_MouseLeave);
            lblFunction[n].MouseDown += new MouseEventHandler(lblFunction_MouseDown);

            // 連結 contextMenuStrip (右鍵選單)
            lblFunction[n].ContextMenuStrip = contextMenuStrip1;
        }

        /// <summary>
        /// 移除控制項 lblFunction
        /// </summary>
        /// <param name="MaxIdx">控制項陣列的上限值</param>
        public void RemoveControl(int MaxIdx)
        {
            int idx;
            // NOTE: The code below uses the instance of the Label from the previous example.
            for (idx = 0; idx <= MaxIdx; idx++)
            {
                //if (splitContainer1.Panel1.Controls.Contains(lblFunction[idx]))
                if (tabPage5.Controls.Contains(lblFunction[idx]))
                {
                    // 移除事件
                    this.lblFunction[idx].MouseMove -= new MouseEventHandler(lblFunction_MouseMove);
                    lblFunction[idx].MouseLeave -= new EventHandler(lblFunction_MouseLeave);
                    lblFunction[idx].MouseDown -= new MouseEventHandler(lblFunction_MouseDown);
                    splitContainer1.Panel1.Controls.Remove(lblFunction[idx]);
                    lblFunction[idx].Dispose();
                }
            }
        }

        private void lblFunction_MouseMove(object sender, MouseEventArgs e)
        {
            string dat = System.Convert.ToString(((Label)sender).Tag);
            lbl_cmdTag.Text = dat;
        }

        private void lblFunction_MouseLeave(object sender, EventArgs e)
        {
            lbl_cmdTag.Text = string.Empty;
        }

        // 單擊測試 & 右鍵選單
        private void lblFunction_MouseDown(object sender, MouseEventArgs e)
        {
            string dat = System.Convert.ToString(((Label)sender).Text);
            int idx = ((Label)sender).TabIndex;
            if (cmdStart.Enabled == false) { return; }
            if (e.Button == MouseButtons.Left)
            {
                cmdOpeFile.Enabled = false;
                cmdStart.Enabled = false;
                cmdStop.Enabled = true;
                cmdNext.Enabled = false;
                TEST_STATUS[idx] = RunTest(idx);
                cmdOpeFile.Enabled = true;
                cmdStart.Enabled = true;
                cmdStop.Enabled = true;
                cmdNext.Enabled = true;
                Run_Stop = false;
            }
            else if (e.Button == MouseButtons.Right)
            {
                MOUSE_Idx = idx;
                if (dat == "Console-DUT" || dat == "Console-EUT" || dat == "Telnet" || dat == "Power")
                {
                    用Putty開啟ToolStripMenuItem.Visible = true;
                }
                else
                {
                    用Putty開啟ToolStripMenuItem.Visible = false;
                }
            }
        }

        public void MappingFunction()
        {
            switch ((MODEL_NAME.Substring(0, 4)).ToUpper())
            {
                default:
                    COM_function = "atop_tcp_server";
                    CAN_functiom = "dcan_tcpsvr";
                    CAN_loopback = "dcan_loopback";
                    break;
            }

            txtDutIP.Text = TARGET_IP;
            //string[] ip_split = new string[3];
            //ip_split = TARGET_IP.Split('.');
            //ip_split[3] = (Convert.ToInt32(ip_split[3]) + 2).ToString();
            //txtEutIP.Text = ip_split[0] + "." + ip_split[1] + "." + ip_split[2] + "." + ip_split[3];
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //string rxContents;
            if (serialPort1.BytesToRead > 0)
            {
                //int bytes = serialPort1.BytesToRead;
                //byte[] comBuffer = new byte[bytes];
                byte[] comBuffer = new byte[serialPort1.BytesToRead];
                serialPort1.Read(comBuffer, 0, comBuffer.Length);
                rxContents = Encoding.ASCII.GetString(comBuffer);

                //myUI(rxContents, txt_Rx);
                this.Invoke(new EventHandler(serialPort1_Display));
            }
        }

        private void serialPort1_Close()
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.DataReceived -= new SerialDataReceivedEventHandler(serialPort1_DataReceived);
                Hold(100);
                serialPort1.Close();
            }
        }

        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //string rxContents_EUT;
            if (serialPort2.BytesToRead > 0)
            {
                byte[] comBuffer = new byte[serialPort2.BytesToRead];
                serialPort2.Read(comBuffer, 0, comBuffer.Length);
                rxContents_EUT = Encoding.ASCII.GetString(comBuffer);

                //myUI(rxContents_EUT, txt_Rx_EUT);
                this.Invoke(new EventHandler(serialPort2_Display));
            }
        }

        private void serialPort2_Close()
        {
            if (serialPort2.IsOpen)
            {
                serialPort2.DataReceived -= new SerialDataReceivedEventHandler(serialPort2_DataReceived);
                Hold(100);
                serialPort2.Close();
            }
        }

        private void telnet_Receive()
        {
            string rdData = string.Empty;
            while (true)
            {
                try
                {
                    Array.Resize(ref bytRead_telnet, telnet.ReceiveBufferSize); // Array.Resize等於vb的ReDim
                    telentStream.Read(bytRead_telnet, 0, telnet.ReceiveBufferSize);
                    rdData = (System.Text.Encoding.Default.GetString(bytRead_telnet));
                    myUI(rdData, txt_Rx);
                    Array.Clear(bytRead_telnet, 0, telnet.ReceiveBufferSize);
                    Thread.Sleep(100);
                }
                catch (Exception)
                {
                    //throw;
                }
            }
        }

        #region Web TcpListener

        private void ListenForClients()
        {
            string t;
            try
            {
                int port = 12345;
                Web_listener = new TcpListener(IPAddress.Any, port);
                Web_listener.Start(1);
                myUI(">> " + "Server Started" + "\r\n", txt_WebRx);

                while (true)
                {
                    //myUI(">> " + "Listening" + "\r\n", txt_WebRx);

                    // blocks until a client has connected to the server
                    TcpClient client = Web_listener.AcceptTcpClient();
                    time = DateTime.Now;
                    t = String.Format("{0:00}:{1:00}:{2:00}", time.Hour, time.Minute, time.Second);
                    myUI(">> " + t + " Connect" + "\r\n", txt_WebRx);

                    // create a thread to handle communication
                    // with connected client
                    //Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    //clientThread.Start(client);

                    Web_client = (TcpClient)client;
                    NetworkStream clientStream = Web_client.GetStream();

                    byte[] message = new byte[4096];
                    int bytesRead;
                    LCdata = string.Empty;

                    //while (true)
                    while (Web_client.Connected)
                    {
                        bytesRead = 0;

                        try
                        {
                            // blocks until a client sends a message
                            bytesRead = clientStream.Read(message, 0, message.Length);
                        }
                        catch
                        {
                            // a socket error has occured
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            // the client has disconnected from the server
                            break;
                        }

                        // message has successfully been received
                        ASCIIEncoding encoder = new ASCIIEncoding();
                        LCdata = encoder.GetString(message, 0, bytesRead);
                        myUI(LCdata + "\r\n", txt_WebRx);
                    }

                    Web_client.Close();
                    time = DateTime.Now;
                    t = String.Format("{0:00}:{1:00}:{2:00}", time.Hour, time.Minute, time.Second);
                    myUI(">> " + t + " Close" + "\r\n", txt_WebRx);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Bind failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private void HandleClient(object client)
        //{
        //    Web_client = (TcpClient)client;
        //    NetworkStream clientStream = Web_client.GetStream();

        //    byte[] message = new byte[4096];
        //    int bytesRead;
        //    string LCdata = string.Empty;

        //    //while (true)
        //    while (Web_client.Connected)
        //    {
        //        bytesRead = 0;

        //        try
        //        {
        //            // blocks until a client sends a message
        //            bytesRead = clientStream.Read(message, 0, message.Length);
        //        }
        //        catch
        //        {
        //            // a socket error has occured
        //            break;
        //        }

        //        if (bytesRead == 0)
        //        {
        //            // the client has disconnected from the server
        //            break;
        //        }

        //        // message has successfully been received
        //        ASCIIEncoding encoder = new ASCIIEncoding();
        //        LCdata = encoder.GetString(message, 0, bytesRead);
        //        myUI(LCdata + "\r\n", txt_WebRx);
        //    }

        //    Web_client.Close();
        //    myUI(">> " + "Close" + "\r\n", txt_WebRx);
        //}

        #endregion Web TcpListener

        /// <summary>
        /// 發送指令
        /// </summary>
        /// <param name="cmd">Command</param>
        public void SendCmd(string cmd)
        {
            if (serialPort1.IsOpen)
            {
                //serialPort1.DiscardOutBuffer(); // 捨棄序列驅動程式傳輸緩衝區的資料
                if (cmd.StartsWith(((char)27).ToString()))
                {
                    serialPort1.Write(cmd);
                }
                else
                {
                    serialPort1.Write(cmd);
                    serialPort1.Write(((char)13).ToString());
                }
            }
            else if (telnet != null && telnet.Connected)
            {
                if (cmd.StartsWith(((char)27).ToString()))
                {
                    bytWrite_telnet = System.Text.Encoding.Default.GetBytes(cmd);
                    telentStream.Write(bytWrite_telnet, 0, bytWrite_telnet.Length);
                }
                else
                {
                    bytWrite_telnet = System.Text.Encoding.Default.GetBytes(cmd + ((char)13).ToString());
                    telentStream.Write(bytWrite_telnet, 0, bytWrite_telnet.Length);
                }
            }
        }

        private void SendWeb(string cmd)
        {
            if (Web_client != null && Web_client.Connected)
            {
                NetworkStream clientStream = Web_client.GetStream();
                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] buffer = encoder.GetBytes(cmd);

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
            }
        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            int idx;
            cmdOpeFile.Enabled = false;
            cmdStart.Enabled = false;
            cmdStop.Enabled = true;
            cmdNext.Enabled = false;
            Run_Stop = false;
            try
            {
                tabControl2.SelectedTab = tabPage5;
                Hold(10);
                time = DateTime.Now;
                startTime = String.Format("{0:00}/{1:00}" + ((char)10).ToString() + "{2:00}:{3:00}:{4:00}", time.Month, time.Day, time.Hour, time.Minute, time.Second);
                if (!chooseStart)
                {
                    for (idx = 0; idx < TestFun_MaxIdx; idx++)
                    {
                        if (!lblFunction[idx].Text.ToUpper().Contains("CONSOLE") || !lblFunction[idx].Text.ToUpper().Contains("TELNET"))
                        {
                            lblFunction[idx].BackColor = Color.FromArgb(255, 255, 255);
                        }
                    }
                    Hold(1);
                }
                retest:
                for (idx = Test_Idx; idx < TestFun_MaxIdx; idx++)
                {
                    TEST_STATUS[idx] = RunTest(idx);
                    if (Run_Stop)
                    {
                        return;
                    }
                    if (STOP_WHEN_FAIL && Convert.ToInt32(TEST_STATUS[idx]) == 2)
                    {
                        break;
                    }
                    Hold(1000);
                }
                if (chkLoop.CheckState == CheckState.Checked && Run_Stop == false)
                {
                    for (idx = 0; idx < TestFun_MaxIdx; idx++)
                    {
                        if (!lblFunction[idx].Text.ToUpper().Contains("CONSOLE") || !lblFunction[idx].Text.ToUpper().Contains("TELNET"))
                        {
                            lblFunction[idx].BackColor = Color.FromArgb(255, 255, 255);
                            Hold(1);
                        }
                    }
                    Test_Idx = 0;
                    goto retest;
                }
            }
            finally
            {
                cmdOpeFile.Enabled = true;
                cmdStart.Enabled = true;
                cmdStop.Enabled = true;
                cmdNext.Enabled = true;
                if (telnet.Connected) { telnet.Close(); }
                Test_Idx = 0;
                chooseStart = false;
            }
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            try
            {
                Run_Stop = true;
                WAIT = false;
                SendCmd(((char)3).ToString()); // ((char)3):Ctrl+c
                Shell(appPATH, "arp-d.bat");
                if (udpClient != null)
                {
                    udpClient.Close(); // 釋放Invite port=55954
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Stop error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                cmdOpeFile.Enabled = true;
                cmdStart.Enabled = true;
                cmdStop.Enabled = true;
                cmdNext.Enabled = true;
            }
        }

        /// <summary>
        /// 功能測試
        /// </summary>
        /// <param name="idx">控制項(lblFunction)陣列的索引標籤</param>
        /// <returns>回傳測試完的結果 0:未測試, 1:PASS, 2:fail, 3:error </returns>
        public int RunTest(int idx)
        {
            lblStatus.Text = string.Empty;
            int RunTest_result = 0; // 0:未測試,1:PASS,2:fail,3:error
            try
            {
                int i;
                string[] line;
                DialogResult dr;
                string[] cmd;
                string time1, time2, timeTmp;
                int j;
                FileStream fs;
                StreamWriter sw;
                double duration;
                int secs;
                string fileDirectory;
                string filePath;
                string keyString = string.Empty;
                string ip_3G = string.Empty; // ip_3G used in "3G" and "NAT" test function.
                //telnet = new TcpClient();

                lblFunction[idx].BackColor = Color.FromArgb(0, 255, 255);   // 測試中

                cmd = Convert.ToString(lblFunction[idx].Tag).Split(' ');
                if (cmd[0].ToUpper() != "CONSOLE-DUT" & cmd[0].ToUpper() != "CONSOLE-EUT" & cmd[0].ToUpper() != "TELNET")
                {
                    if (!serialPort1.IsOpen & !telnet.Connected)
                    {
                        lblStatus.Text = "Console-DUT 或 Telnet 未連接";
                        return RunTest_result = 3;
                    }
                    //if ((cmd[0].ToUpper() == "COMTOCOM" || cmd[0].ToUpper() == "CANTOCAN")
                    //    & !serialPort2.IsOpen)
                    //{
                    //    lblStatus.Text = "Console-EUT 未連接";
                    //    return RunTest_result = 3;
                    //}
                }
                // for excel log
                if (TEST_FunLog.Contains(cmd[0].ToUpper()))
                {
                    idx_funlog = TEST_FunLog.IndexOf(cmd[0].ToUpper());
                }

                //SendCmd(string.Empty);
                switch (cmd[0].ToUpper())
                {
                    case "CONSOLE-DUT":     // Console show
                        serialPort1_Close();
                        serialPort1.PortName = cmbDutCom.Text;
                        serialPort1.BaudRate = 115200;
                        serialPort1.Parity = Parity.None;
                        serialPort1.DataBits = 8;
                        serialPort1.StopBits = StopBits.One;
                        serialPort1.Handshake = Handshake.None; // 流量控制；交握協定
                        serialPort1.Open();
                        serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);
                        lblStatus.Text = "Console-DUT Connect OK !";
                        RunTest_result = 1;
                        if (cmd.GetUpperBound(0) >= 1)
                        {
                            if (cmd[1].ToUpper() == "SHOW")
                            {
                                consoleToolStripMenuItem.Checked = true;
                            }
                        }
                        else { consoleToolStripMenuItem.Checked = false; }

                        serialPort1.Write(((char)13).ToString());
                        Hold(500);
                        line = txt_Rx.Text.Split('\r');
                        for (i = line.GetUpperBound(0); i >= 0; i--)    // 從尾巴先搜尋
                        {
                            if (line[i].Contains("login"))
                            {
                                serialPort1.Write(USR + ((char)13).ToString());
                                Hold(200);
                                serialPort1.Write(PWD + ((char)13).ToString());
                                break;  // for
                            }
                            else if (line[i].Contains("Main Menu") || line[i].Contains("Manufactory Settings"))
                            {
                                break;  // for
                            }
                        }

                        SendCmd("");
                        break;
                    case "CONSOLE-EUT":     // Console
                        serialPort2_Close();
                        serialPort2.PortName = cmbEutCom.Text;
                        serialPort2.BaudRate = 115200;
                        serialPort2.Parity = Parity.None;
                        serialPort2.DataBits = 8;
                        serialPort2.StopBits = StopBits.One;
                        serialPort2.Handshake = Handshake.None; // 流量控制；交握協定
                        serialPort2.Open();
                        serialPort2.DataReceived += new SerialDataReceivedEventHandler(serialPort2_DataReceived);
                        lblStatus.Text = "Console-EUT Connect OK !";
                        RunTest_result = 1;
                        //serialPort2.Write("root" + ((char)13).ToString());
                        //Hold(200);
                        //serialPort2.Write("root" + ((char)13).ToString());
                        break;
                    case "TELNET":      // Telnet USR PWD
                        //Shell(appPATH, "arp-d.bat");
                        //Hold(1000);
                        //txt_Rx.Text = string.Empty;
                        RunTest_result = 1;
                        //if (telnet.Connected) { telnet.Close(); }
                        if (objping.Send(TARGET_IP, 1000).Status == System.Net.NetworkInformation.IPStatus.Success)
                        {
                            if (!telnet.Connected)
                            {
                                telnet = new TcpClient();
                                telnet.Connect(TARGET_IP, 23);   // 連接23端口 (Telnet的默認端口)
                                telentStream = telnet.GetStream();  // 建立網路資料流，將字串寫入串流

                                if (telnet.Connected)
                                {
                                    //lblStatus.Text = "連線成功，正在登錄...";
                                    lblStatus.Text = "正在登錄...";
                                    Hold(1000);
                                    // 背景telnet接收執行緒
                                    if (rcvThread == null || !rcvThread.IsAlive)
                                    {
                                        ThreadStart backgroundReceive = new ThreadStart(telnet_Receive);
                                        rcvThread = new Thread(backgroundReceive);
                                        rcvThread.IsBackground = true;
                                        rcvThread.Start();
                                    }
                                    bytWrite_telnet = System.Text.Encoding.Default.GetBytes(USR + ((char)13).ToString());
                                    telentStream.Write(bytWrite_telnet, 0, bytWrite_telnet.Length);
                                    Hold(200);
                                    bytWrite_telnet = System.Text.Encoding.Default.GetBytes(PWD + ((char)13).ToString());
                                    telentStream.Write(bytWrite_telnet, 0, bytWrite_telnet.Length);
                                    lblStatus.Text = "連線成功 ";
                                }
                            }
                        }
                        else
                        {
                            lblStatus.Text = "ping失敗，請確認你的IP設定或網路設定";
                            RecNote(idx, cmd[0].ToUpper() + " Test Fail.");
                            RunTest_result = 2;
                        }
                        break;
                    case "RESTART":
                        if (telnet.Connected) { telnet.Close(); }
                        RunTest_result = 3;
                        SendCmd("restart&");
                        SendCmd("atop_restart&");
                        RecNote(idx, "Restart");
                        if (cmd[1].ToUpper() == "LOGIN")
                        {
                            secs = Convert.ToInt32(cmd[2]);
                            RunTest_result = ReCntTelnet(secs);   // need to login
                        }
                        else if (cmd[1].ToUpper() == "NONE")
                        {
                            WaitKey = "U-Boot ";
                            if (Hold(5000))
                            {
                                secs = Convert.ToInt32(cmd[2]) * 1000;
                                WaitKey = "~#";     // doesn't need to login
                                if (Hold(secs))
                                {
                                    RunTest_result = 1;
                                }
                            }
                        }
                        break;
                    case "COM": // normal test: COM max_port mode
                        RunTest_result = 1;
                        // SE5901
                        if (MODEL_NAME.Equals("SE5901", StringComparison.CurrentCultureIgnoreCase))
                        {
                            SendCmd("/jffs2/" + cmd[2].ToLower() + "_loopback");
                            SendCmd("");// 讓WaitKey較能接收到資料做判斷
                            WaitKey = "PASS";
                            if (Hold(3000) == false)
                            {
                                RunTest_result = 2;
                                SendCmd(((char)3).ToString());
                                RecNote(idx, "RS232 loopback test Fail.");
                            }
                        }
                        // SE5901A
                        else if (MODEL_NAME.Contains("SE5901A"))
                        {
                            j = 0;
                            SendCmd("/jffs2/" + cmd[2].ToLower() + "_loopback " + j + " " + (j + 4));
                            SendCmd("");// 讓WaitKey較能接收到資料做判斷
                            WaitKey = "PASS";
                            if (Hold(3000) == false)
                            {
                                RunTest_result = 2;
                                SendCmd(((char)3).ToString());
                                string failmessage = "COM" + j + " -> COM" + (j + 4) + " Fail";
                                RecNote(idx, failmessage);
                            }
                            SendCmd("/jffs2/" + cmd[2].ToLower() + "_loopback " + (j + 4) + " " + j);
                            SendCmd("");
                            WaitKey = "PASS";
                            if (Hold(3000) == false)
                            {
                                RunTest_result = 2;
                                SendCmd(((char)3).ToString());
                                string failmessage = "COM" + (j + 4) + " -> COM" + j + " Fail";
                                RecNote(idx, failmessage);
                            }
                        }
                        // normal test
                        else
                        {
                            for (j = 1; j <= Convert.ToInt32(cmd[1]); j = j + 2)
                            {
                                SendCmd(cmd[2].ToLower() + "_loopback " + j + " " + (j + 1));
                                WaitKey = "test ok";
                                if (Hold(3000) == false)
                                {
                                    RunTest_result = 2;
                                    SendCmd(((char)3).ToString());
                                    string failmessage = "COM" + j + " -> COM" + (j + 1) + " Fail";
                                    RecNote(idx, failmessage);
                                }
                                SendCmd(cmd[2].ToLower() + "_loopback " + (j + 1) + " " + j);
                                WaitKey = "test ok";
                                if (Hold(3000) == false)
                                {
                                    RunTest_result = 2;
                                    SendCmd(((char)3).ToString());
                                    string failmessage = "COM" + (j + 1) + " -> COM" + j + " Fail";
                                    RecNote(idx, failmessage);
                                }
                            }
                        }

                        if (RunTest_result == 1)
                        {
                            lblStatus.Text = "COM loopback Test Pass.";
                        }
                        break;
                    case "COMTOCOM":    // COMtoCOM port(1-4 or 4 or 0-4) 陪測物IP mode BaudRate time unit
                        SendCmd("killall uart_link_mode");
                        Hold(200);
                        SendCmd("uart_link_mode &");
                        Hold(1000);
                        RunTest_result = 2;
                        // 建立檔案
                        fs = File.Open("Auto_Test", FileMode.OpenOrCreate, FileAccess.Write);
                        // 建構StreamWriter物件
                        sw = new StreamWriter(fs);
                        sw.Close();
                        fs.Close();
                        duration = Math.Round(TimeUnit(idx, 5) / 60, 2);
                        MultiPortTesting_settings(cmd[2], 1000, cmd[1], 4660, 1, duration.ToString());
                        COM_PID[0] = Shell(appPATH, "Multi-Port-Testingv1.6r.exe");
                        Hold(1000);
                        MultiPortTesting_settings(TARGET_IP, 1000, cmd[1], 4660, 0, duration.ToString());
                        COM_PID[1] = Shell(appPATH, "Multi-Port-Testingv1.6r.exe");
                        pause(duration);
                        if (File.Exists("Auto_Test"))
                        {
                            File.Delete("Auto_Test");
                        }
                        Hold(3000); // 因為Multi-Port-Testingv1.6p (2013/11/22)的行為，所以等待是必須的
                        CloseShell(COM_PID[0]);
                        CloseShell(COM_PID[1]);
                        COM_PID[0] = 0;
                        COM_PID[1] = 0;
                        if (!File.Exists("debug.txt"))
                        {
                            RunTest_result = 1;
                        }

                        if (RunTest_result == 1)
                        {
                            lblStatus.Text = cmd[0].ToUpper() + " Test Pass.";
                        }
                        else
                        {
                            RecNote(idx, cmd[0].ToUpper() + " Test Fail.");
                        }
                        break;
                    case "LOADTOOLS":   // "MODEL_NAME"_Tools資料夾裡的所有檔案載入待測物
                        RunTest_result = 1;

                        if (MODEL_NAME.Contains("SE5901A"))
                        {
                            fileDirectory = "SE5901A_Tools";
                        }
                        else
                        {
                            fileDirectory = MODEL_NAME + "_Tools";
                        }

                        filePath = appPATH + "\\" + fileDirectory;
                        if (Directory.Exists(fileDirectory))
                        {
                            // Process the list of files found in the directory.
                            string[] fileEntries = Directory.GetFiles(filePath);
                            foreach (string fileName in fileEntries)
                            {
                                string sourceFile = fileName.Replace(filePath + "\\", "");
                                uploadFile(TARGET_IP, fileDirectory + "\\" + sourceFile, USR, PWD);
                                Hold(1);
                                bool check = checkFile(TARGET_IP, sourceFile, USR, PWD);
                                if (!check) // false代表沒有上載成功，檔案不存在
                                {
                                    RecNote(idx, sourceFile + " not exist!");
                                    RunTest_result = 2;
                                }
                            }
                        }
                        SendCmd("chmod 755 /jffs2/*");
                        Hold(100);

                        SendCmd("ls -al /jffs2/");
                        break;
                    case "DELETE":
                        SendCmd("rm /jffs2/*");
                        RunTest_result = 1;

                        if (MODEL_NAME.Contains("SE5901A"))
                        {
                            fileDirectory = "SE5901A_Tools";
                        }
                        else
                        {
                            fileDirectory = MODEL_NAME + "_Tools";
                        }

                        filePath = appPATH + "\\" + fileDirectory;
                        if (Directory.Exists(fileDirectory))
                        {
                            // Process the list of files found in the directory.
                            string[] fileEntries = Directory.GetFiles(filePath);
                            foreach (string fileName in fileEntries)
                            {
                                string sourceFile = fileName.Replace(filePath + "\\", "");
                                bool check = checkFile(TARGET_IP, sourceFile, USR, PWD);
                                if (check)  // true代表沒有刪除成功
                                {
                                    RecNote(idx, sourceFile + " 沒有刪除成功!");
                                    RunTest_result = 2;
                                }
                            }
                        }
                        SendCmd("ls /jffs2/");
                        break;
                    case "RTC":
                        time = DateTime.Now;
                        time1 = String.Format("{0:00}/{1:00}/{2:00}-{3:00}:{4:00}:{5:00}", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
                        timeTmp = time1.Substring(0, time1.IndexOf("-"));
                        SendCmd("set_rtc " + time1);
                        Hold(100);
                        SendCmd("get_rtc");
                        Hold(300);
                        RunTest_result = 3;
                        line = txt_Rx.Text.Split('\r');
                        for (i = line.GetUpperBound(0); i >= 0; i--)    // 從尾巴先搜尋
                        {
                            if (line[i].Contains("get_rtc"))
                            {
                                if (line[i + 1].Contains(timeTmp))
                                {
                                    RunTest_result = 1;
                                    lblStatus.Text = "Test Pass !";
                                }
                                else
                                {
                                    RunTest_result = 2;
                                    lblStatus.Text = "Test Fail !";
                                }
                                break;  // for
                            }
                        }
                        break;
                    case "BUZZER":
                        SendCmd("atop_buzzer");
                        if (cmd.GetUpperBound(0) >= 1)
                        {
                            if (cmd[1].ToUpper() == "SKIP")
                            {
                                lblStatus.Text = "略過人工判斷";
                                Hold(2000); // wait for buzzer
                                RunTest_result = 1;
                            }
                        }
                        else if (chkHumanSkip.CheckState == CheckState.Checked)
                        {
                            lblStatus.Text = "略過人工判斷";
                            Hold(2000); // wait for buzzer
                            RunTest_result = 1;
                        }
                        else
                        {
                            lblStatus.Text = "人工判斷";
                            Hold(1000);
                            dr = MessageBox.Show("是否有聽到蜂鳴器發出聲響 ? ", cmd[0] + " Test", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                            if (dr == DialogResult.Yes) { RunTest_result = 1; }
                            else if (dr == DialogResult.No) { RecNote(idx, cmd[0].ToUpper() + " Test Fail."); RunTest_result = 2; }
                        }
                        break;
                    case "WATCHDOG":
                        SendCmd("/jffs2/atop_hwd 30 &");
                        if (cmd.GetUpperBound(0) >= 1)
                        {
                            if (cmd[1].ToUpper() == "KILL")
                            {
                                Hold(2000);
                                SendCmd("killall atop_hwd");
                                WaitKey = "U-Boot ";
                                if (Hold(60000))
                                {
                                    RunTest_result = 1;
                                    ReCntTelnet(50);
                                }
                                else
                                {
                                    RunTest_result = 2;
                                }
                            }
                        }
                        else
                        {
                            RunTest_result = 1;
                            WaitKey = "Disable Hardware Watchdog";
                            if (Hold(65000) == false)
                            {
                                RunTest_result = 2;
                                RecNote(idx, cmd[0].ToUpper() + " Test Fail.");
                            }
                        }
                        break;
                    case "SLEEP":
                        duration = Math.Round(TimeUnit(idx, 1) / 60, 2);
                        pause(duration);
                        break;
                    case "SYSTEM":
                        duration = Math.Round(TimeUnit(idx, 4) / 60, 2);
                        break;
                    case "RUN":
                        String Command, Output;
                        if (cmd.Length > 1)
                        {
                            for (i = 1; i < cmd.Length; i++)
                            {
                                Command = @"python " + cmd[i];
                                //Output = run(Command);   // debug "Output"
                                Hold(3000);
                                runpy(Command);
                                RunTest_result = 2;
                                WaitKey = "set ok";
                                if (Hold(60000))
                                {
                                    RunTest_result = 1;
                                }
                            }
                        }
                        break;
                    case "INVITE":
                        // step 1. Python傳回的值
                        int totelLan = 0;
                        for (j = 0; j < Device_Max; j++)
                        {
                            for (i = 0; i < 24; i++)
                            {
                                DA2_compare[j, i] = string.Empty;
                            }
                        }
                        if (LCdata == null)
                        {
                            lblStatus.Text = "請搭配 .py 檔案做測試";
                            return RunTest_result = 3;
                        }
                        line = txt_WebRx.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        for (i = line.GetUpperBound(0); i >= 0; i--)
                        {
                            string[] tmp = line[i].Split(',');
                            if (line[i].ToUpper().Contains("TOTAL LAN"))
                            {
                                totelLan = Convert.ToInt32(tmp[0].ToUpper().Replace("TOTAL LAN ", "").Trim());
                            }
                        }
                        for (j = 0; j < totelLan; j++)
                        {
                            for (i = line.GetUpperBound(0); i >= 0; i--)
                            {
                                string[] tmp = line[i].Split(',');
                                if (line[i].ToUpper().Contains("KERNEL") && line[i].ToUpper().Contains("AP"))
                                {
                                    DA2_compare[j, 22] = tmp[0].ToUpper().Replace("KERNEL ", "");
                                    DA2_compare[j, 23] = tmp[1].ToUpper().Replace("AP ", "");
                                }
                                //////////////////////////////////////// LAN與MAC的判斷，會隨著totalLan的數量，需要手動增加程式碼
                                if (line[i].ToUpper().Contains("LAN1") && line[i].ToUpper().Contains("MAC") && line[i].ToUpper().Contains("IP") && j == 0)
                                {
                                    DA2_compare[j, 2] = tmp[0].ToUpper().Replace("LAN1 MAC ", "");
                                    DA2_compare[j, 2] = DA2_compare[j, 2].Replace(":", "");
                                    DA2_compare[j, 0] = tmp[1].ToUpper().Replace("IP ", "");
                                }
                                if (line[i].ToUpper().Contains("LAN2") && line[i].ToUpper().Contains("MAC") && line[i].ToUpper().Contains("IP") && j == 1)
                                {
                                    DA2_compare[j, 2] = tmp[0].ToUpper().Replace("LAN2 MAC ", "");
                                    DA2_compare[j, 2] = DA2_compare[j, 2].Replace(":", "");
                                    DA2_compare[j, 0] = tmp[1].ToUpper().Replace("IP ", "");
                                }
                                ///////////////////////////////////////////////////////////////////////////////////////////////////
                                if (line[i].ToUpper().Contains("MODEL"))
                                {
                                    DA2_compare[j, 7] = line[i].ToUpper().Replace("MODEL ", "");
                                    break;
                                }
                            }
                        }

                        // step 2. Invite掃描到的值
                        DevCount = 0;
                        for (j = 0; j < Device_Max; j++)
                        {
                            for (i = 0; i < 24; i++)
                            {
                                DA2[j, i] = string.Empty;
                            }
                            stringData[j] = string.Empty;
                        }
                        try
                        {
                            byte[] bdata = new byte[300];
                            bdata[0] = 2;
                            bdata[1] = 1;
                            bdata[2] = 6;
                            bdata[4] = Convert.ToByte(Convert.ToUInt64("92", 16)); //將十六進制轉換為十進制
                            bdata[5] = Convert.ToByte(Convert.ToUInt64("DA", 16));
                            IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Broadcast, UDPport);   // 目標 port

                            // 背景接收執行緒
                            if (udpRcvThread == null || !udpRcvThread.IsAlive)
                            {
                                udpRcvThread = new Thread(new ThreadStart(ReceiveBroadcast));
                                udpRcvThread.IsBackground = true;
                                udpRcvThread.Priority = ThreadPriority.Highest;
                                udpRcvThread.Start();
                            }

                            if (!PortInUse(UDPport))  // 判斷 local port 是否占用中
                            {
                                udpClient = new UdpClient(UDPport); // 未使用就綁定 local port
                                udpClient.Send(bdata, bdata.Length, ipEndpoint);
                            }
                            else
                            {
                                udpClient.Send(bdata, bdata.Length, ipEndpoint);
                            }
                        }
                        catch (Exception)
                        {
                            lblStatus.Text = "請關掉 Serial Manager、Monitor 相關軟體";
                            return RunTest_result = 3;
                        }

                        // step 3. 將python回傳的值和invite掃到的值進行比對
                        Hold(1000); // 等待 udpRcvThread 執行
                        if (DevCount != totelLan)
                        {
                            RecNote(idx, "掃描到的IP數量與待測物的IP數量不符");
                            lblStatus.Text = "掃描到的IP數量與待測物的IP數量不符";
                            return RunTest_result = 3;
                        }
                        for (j = 0; j < totelLan; j++)
                        {
                            for (i = 0; i < 24; i++)
                            {
                                if (DA2[j, i].Contains(DA2_compare[j, i]))
                                {
                                    RunTest_result = 1;
                                }
                                else
                                {
                                    RecNote(idx, cmd[0].ToUpper() + " Test Fail");
                                    return RunTest_result = 2;
                                }
                            }
                        }
                        break;
                    case "EMAIL":
                        string judgment = string.Empty;
                        string test_case = string.Empty;
                        int retry = 20;

                        POP3Client.POP3client Demo = new POP3Client.POP3client();

                        #region practice code

                        //Console.WriteLine("****connecting to server:");
                        //Console.WriteLine(Demo.connect("www.hibox.hinet.net"));     // mail Server
                        //Console.WriteLine("****Issuing USER");
                        //Console.WriteLine(Demo.USER("parkerdai@atop.com.tw"));      // mail User
                        //Console.WriteLine("****Issuing PASS");
                        //Console.WriteLine(Demo.PASS("101026"));                     // mail Password
                        //Console.WriteLine("****Issuing STAT");
                        //Console.WriteLine(Demo.STAT());
                        //Console.WriteLine("****Issuing LIST");
                        //Console.WriteLine(Demo.LIST());
                        ////Console.WriteLine("****Issuing RETR 700...this will cause the POP3 server to gack a hairball since there is no message 700");
                        ////Console.WriteLine(Demo.RETR(700));			//this will cause the pop3 server to throw an error since there is no message 700
                        ////Console.WriteLine("****Issuing RETR 7");
                        ////Console.WriteLine(Demo.RETR(1));
                        // 印出全部mail
                        //Console.WriteLine("****Issuing RETR 7");
                        //for (i = 1; i <= Demo.counter; i++)
                        //{
                        //    Console.WriteLine(Demo.RETR(i));
                        //}
                        // 刪除指定mail，執行QUIT指令後生效
                        //Console.WriteLine("****Issuing DELETE");
                        //for (i = 1; i <= Demo.counter; i++)
                        //{
                        //    Console.WriteLine(Demo.DELE(i));
                        //}
                        //Console.WriteLine("****Issuing QUIT");
                        //Console.WriteLine(Demo.QUIT());
                        # endregion

                        myUI("****connecting to server:" + "\r\n", txt_WebRx);
                        try
                        {
                            judgment = Demo.connect(cmd[1]) + "\r\n";       // mail Server
                        }
                        catch (Exception ex)
                        {
                            lblStatus.Text = "(Server error)" + ex.Message;
                            return RunTest_result = 3;
                        }
                        myUI(judgment, txt_WebRx);
                        myUI("****Issuing USER" + "\r\n", txt_WebRx);
                        judgment = Demo.USER(cmd[2]) + "\r\n";      // mail User
                        myUI(judgment, txt_WebRx);
                        myUI("****Issuing PASS" + "\r\n", txt_WebRx);
                        judgment = Demo.PASS(cmd[3]) + "\r\n";      // mail Password
                        myUI(judgment, txt_WebRx);
                        if (!judgment.Contains("-ERR"))
                        {
                            myUI("****Issuing NOOP" + "\r\n", txt_WebRx);
                            judgment = Demo.NOOP() + "\r\n";
                            myUI(judgment, txt_WebRx);
                            if (judgment.Contains("+OK"))
                            {
                                myUI("****Issuing STAT" + "\r\n", txt_WebRx);
                                judgment = Demo.STAT() + "\r\n";
                                myUI(judgment, txt_WebRx);
                                myUI("****Issuing LIST" + "\r\n", txt_WebRx);
                                judgment = Demo.LIST() + "\r\n";
                                myUI(judgment, txt_WebRx);
                                myUI("****Issuing RETR" + "\r\n", txt_WebRx);
                                // DELETE指定的Sender的郵件，避免造成誤刪
                                for (i = 1; i <= Demo.counter; i++)
                                {
                                    judgment = Demo.RETR(i) + "\r\n";
                                    if (judgment.Contains("Sender: " + cmd[4]))
                                    {
                                        // 刪除指定的mail，執行QUIT指令後生效
                                        myUI("****Issuing DELETE " + i + "\r\n", txt_WebRx);
                                        myUI(Demo.DELE(i) + "\r\n", txt_WebRx);
                                    }
                                }
                            }
                        }
                        else
                        {
                            myUI("****Issuing QUIT" + "\r\n", txt_WebRx);
                            judgment = Demo.QUIT() + "\r\n";
                            myUI(judgment, txt_WebRx);
                            myUI("\r\n\r\n", txt_WebRx);
                            lblStatus.Text = "Authentication failed";
                            return RunTest_result = 3;
                        }
                        myUI("****Issuing QUIT" + "\r\n", txt_WebRx);
                        judgment = Demo.QUIT() + "\r\n";
                        myUI(judgment, txt_WebRx);
                        myUI("\r\n\r\n", txt_WebRx);

                        if (Run_Stop) { return RunTest_result = 0; }
                        txt_Rx.Text = string.Empty;

                        # region "practice code"
                        //test_case = "Send Test E-mail";
                        //runpy("alert_email_sendtestmail.py");
                        //WaitKey = "set ok";
                        //if (Hold(60000))
                        //{
                        //    Hold(3000); // 等待 mail server 處理郵件
                        //    myUI("****connecting to server:" + "\r\n", txt_WebRx);
                        //    judgment = Demo.connect(cmd[1]) + "\r\n";       // mail Server
                        //    myUI(judgment, txt_WebRx);
                        //    if (judgment.Contains("+OK"))
                        //    {
                        //        myUI("****Issuing USER" + "\r\n", txt_WebRx);
                        //        judgment = Demo.USER(cmd[2]) + "\r\n";      // mail User
                        //        myUI(judgment, txt_WebRx);
                        //        myUI("****Issuing PASS" + "\r\n", txt_WebRx);
                        //        judgment = Demo.PASS(cmd[3]) + "\r\n";      // mail Password
                        //        myUI(judgment, txt_WebRx);
                        //        myUI("****Issuing NOOP" + "\r\n", txt_WebRx);
                        //        judgment = Demo.NOOP() + "\r\n";
                        //        myUI(judgment, txt_WebRx);
                        //        if (judgment.Contains("+OK"))
                        //        {
                        //            myUI("****Issuing STAT" + "\r\n", txt_WebRx);
                        //            judgment = Demo.STAT() + "\r\n";
                        //            myUI(judgment, txt_WebRx);
                        //            myUI("****Issuing LIST" + "\r\n", txt_WebRx);
                        //            judgment = Demo.LIST() + "\r\n";
                        //            myUI(judgment, txt_WebRx);
                        //            myUI("****Issuing RETR" + "\r\n", txt_WebRx);
                        //            // 印出最後一封mail
                        //            if (Demo.counter > 0)
                        //            {
                        //                judgment = Demo.RETR(Demo.counter) + "\r\n";
                        //                myUI(judgment, txt_WebRx);

                        //                if (judgment.Contains("Sender: " + cmd[4]))
                        //                {
                        //                    if (judgment.Contains("Alert Condition: " + test_case))
                        //                    {
                        //                        RunTest_result = 1;
                        //                    }
                        //                    else
                        //                    {
                        //                        lblStatus.Text = "信件 Alert Condition 比對錯誤";
                        //                        RunTest_result = 2;
                        //                    }
                        //                }
                        //                else
                        //                {
                        //                    lblStatus.Text = "找不到來自" + cmd[4] + "的信件";
                        //                    RunTest_result = 2;
                        //                }
                        //            }
                        //        }
                        //        myUI("****Issuing QUIT" + "\r\n", txt_WebRx);
                        //        judgment = Demo.QUIT() + "\r\n";
                        //        myUI(judgment, txt_WebRx);
                        //    }
                        //}

                        #endregion practice code

                        test_case = "Send Test E-mail";
                        runpy("alert_email_sendtestmail.py");
                        WaitKey = "set ok";
                        if (Hold(60000))
                        {
                            for (i = 1; i <= retry; i++)
                            {
                                RunTest_result = GetMAIL(cmd[1], cmd[2], cmd[3], cmd[4], test_case, Demo);
                                if (RunTest_result == 1)
                                {
                                    break;
                                }
                                Hold(1000);
                            }
                            if (RunTest_result != 1)
                            {
                                RecNote(idx, test_case + " failed");
                                lblStatus.Text = test_case + " failed";
                                return RunTest_result = 2;
                            }
                        }
                        else
                        {
                            RecNote(idx, test_case + " failed");
                            lblStatus.Text = test_case + " failed";
                            return RunTest_result = 2;
                        }
                        myUI(".\r\n.\r\n", txt_WebRx);
                        if (Run_Stop) { return RunTest_result = 0; }

                        test_case = "Cold Start";
                        serialPort2.Write("atop_do 1 0" + ((char)13).ToString());
                        Hold(3000);
                        serialPort2.Write("atop_do 1 1" + ((char)13).ToString());
                        WaitKey = "ESC";
                        if (Hold(5000))
                        {
                            //enterTmr.Enabled = true;    // 5秒按一次enter
                            WaitKey = "#";     // doesn't need to login
                            if (Hold(60000))
                            {
                                for (i = 1; i <= retry; i++)
                                {
                                    RunTest_result = GetMAIL(cmd[1], cmd[2], cmd[3], cmd[4], test_case, Demo);
                                    if (RunTest_result == 1)
                                    {
                                        break;
                                    }
                                    Hold(1000);
                                }
                                if (RunTest_result != 1)
                                {
                                    RecNote(idx, test_case + " failed");
                                    lblStatus.Text = test_case + " failed";
                                    return RunTest_result = 2;
                                }
                            }
                            //enterTmr.Enabled = false;
                        }
                        else
                        {
                            RecNote(idx, test_case + " failed");
                            lblStatus.Text = test_case + " failed";
                            return RunTest_result = 2;
                        }
                        myUI(".\r\n.\r\n", txt_WebRx);
                        if (Run_Stop) { return RunTest_result = 0; }

                        ////test_case = "Warm Start";
                        ////SendCmd("atop_restart");
                        ////SendCmd("restart");
                        ////WaitKey = "ESC";
                        ////if (Hold(5000))
                        ////{
                        ////    //enterTmr.Enabled = true;    // 5秒按一次enter
                        ////    WaitKey = "#";     // doesn't need to login
                        ////    if (Hold(60000))
                        ////    {
                        ////        for (i = 1; i <= retry; i++)
                        ////        {
                        ////            RunTest_result = GetMAIL(cmd[1], cmd[2], cmd[3], cmd[4], test_case, Demo);
                        ////            if (RunTest_result == 1)
                        ////            {
                        ////                break;
                        ////            }
                        ////            Hold(1000);
                        ////        }
                        ////        if (RunTest_result != 1)
                        ////        {
                        ////            RecNote(idx, test_case + " failed");
                        ////            lblStatus.Text = test_case + " failed";
                        ////            return RunTest_result = 2;
                        ////        }
                        ////    }
                        ////    //enterTmr.Enabled = false;
                        ////}
                        ////else
                        ////{
                        ////    RecNote(idx, test_case + " failed");
                        ////    lblStatus.Text = test_case + " failed";
                        ////    return RunTest_result = 2;
                        ////}
                        ////myUI(".\r\n.\r\n", txt_WebRx);
                        ////if (Run_Stop) { return RunTest_result = 0; }

                        test_case = "Password Changed";
                        runpy("security_changepassword.py");
                        WaitKey = "set ok";
                        if (Hold(60000))
                        {
                            for (i = 1; i <= retry; i++)
                            {
                                RunTest_result = GetMAIL(cmd[1], cmd[2], cmd[3], cmd[4], test_case, Demo);
                                if (RunTest_result == 1)
                                {
                                    break;
                                }
                                Hold(1000);
                            }
                            if (RunTest_result != 1)
                            {
                                RecNote(idx, test_case + " failed");
                                lblStatus.Text = test_case + " failed";
                                return RunTest_result = 2;
                            }
                        }
                        else
                        {
                            RecNote(idx, test_case + " failed");
                            lblStatus.Text = test_case + " failed";
                            return RunTest_result = 2;
                        }
                        myUI(".\r\n.\r\n", txt_WebRx);
                        if (Run_Stop) { return RunTest_result = 0; }

                        test_case = "IP Address Changed";
                        runpy("change_lan2IP_forEmail.py");
                        WaitKey = "set ok";
                        if (Hold(60000))
                        {
                            for (i = 1; i <= retry; i++)
                            {
                                RunTest_result = GetMAIL(cmd[1], cmd[2], cmd[3], cmd[4], test_case, Demo);
                                if (RunTest_result == 1)
                                {
                                    break;
                                }
                                Hold(1000);
                            }
                            if (RunTest_result != 1)
                            {
                                RecNote(idx, test_case + " failed");
                                lblStatus.Text = test_case + " failed";
                                return RunTest_result = 2;
                            }
                        }
                        else
                        {
                            RecNote(idx, test_case + " failed");
                            lblStatus.Text = test_case + " failed";
                            return RunTest_result = 2;
                        }
                        myUI(".\r\n.\r\n", txt_WebRx);
                        if (Run_Stop) { return RunTest_result = 0; }
                        // IP Address Changed會重開機，所以接著測Warm Start
                        test_case = "Warm Start";
                        WaitKey = "#";     // doesn't need to login
                        if (Hold(60000))
                        {
                            for (i = 1; i <= retry; i++)
                            {
                                RunTest_result = GetMAIL(cmd[1], cmd[2], cmd[3], cmd[4], test_case, Demo);
                                if (RunTest_result == 1)
                                {
                                    break;
                                }
                                Hold(1000);
                            }
                            if (RunTest_result != 1)
                            {
                                RecNote(idx, test_case + " failed");
                                lblStatus.Text = test_case + " failed";
                                return RunTest_result = 2;
                            }
                        }
                        else
                        {
                            RecNote(idx, test_case + " failed");
                            lblStatus.Text = test_case + " failed";
                            return RunTest_result = 2;
                        }

                        ////test_case = "Authentication Failure";
                        ////runpy(".py");
                        ////WaitKey = "set ok";
                        ////if (Hold(60000))
                        ////{
                        ////    for (i = 1; i <= retry; i++)
                        ////    {
                        ////        RunTest_result = GetMAIL(cmd[1], cmd[2], cmd[3], cmd[4], test_case, Demo);
                        ////        if (RunTest_result == 1)
                        ////        {
                        ////            break;
                        ////        }
                        ////        Hold(1000);
                        ////    }
                        ////    if (RunTest_result != 1)
                        ////    {
                        ////        RecNote(idx, test_case + " failed");
                        ////        lblStatus.Text = test_case + " failed";
                        ////        return RunTest_result = 2;
                        ////    }
                        ////}
                        ////else
                        ////{
                        ////    RecNote(idx, test_case + " failed");
                        ////    lblStatus.Text = test_case + " failed";
                        ////    return RunTest_result = 2;
                        ////}
                        ////myUI(".\r\n.\r\n", txt_WebRx);
                        break;
                    default:
                        break;
                }
                // Excel log
                if (TEST_FunLog.Contains(cmd[0].ToUpper()))
                {
                    if (RunTest_result == 1)
                    {
                        TEST_RESULT[idx_funlog] = TEST_RESULT[idx_funlog] + "o";
                    }
                    else if (RunTest_result == 2)
                    {
                        TEST_RESULT[idx_funlog] = TEST_RESULT[idx_funlog] + "X";
                    }
                    else if (RunTest_result == 3)
                    {
                        TEST_RESULT[idx_funlog] = TEST_RESULT[idx_funlog] + "-";
                    }
                }
                return RunTest_result;  // switch use
            }
            catch (Exception ex)
            {
                RecNote(idx, ex.Message);
                SendCmd(((char)3).ToString()); // ((char)3):Ctrl+c
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Run_Stop = true;
                return RunTest_result = 3;
            }
            finally
            {
                if (RunTest_result == 1)
                {
                    lblFunction[idx].BackColor = Color.FromArgb(0, 255, 0); /* 1:PASS Green */
                }
                else if (RunTest_result == 2)
                {
                    lblFunction[idx].BackColor = Color.FromArgb(255, 0, 0); /* 2:Fail Red */
                }
                else if (RunTest_result == 3)
                {
                    lblFunction[idx].BackColor = Color.FromArgb(255, 255, 0); /* 3:error Yellow */
                }
                else if (RunTest_result == 0) { lblFunction[idx].BackColor = Color.FromArgb(255, 255, 255); /* 0 */}
            }
        }

        private int GetMAIL(string server, string user, string password, string sender, string test_case, POP3Client.POP3client Demo)
        {
            string judgment = string.Empty;
            lblStatus.Text = string.Empty;

            try
            {
                try
                {
                    //myUI("****connecting to server:" + "\r\n", txt_WebRx);
                    judgment = Demo.connect(server) + "\r\n";       // mail Server
                    //myUI(judgment, txt_WebRx);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "(Server error)" + ex.Message;
                    return 3;
                }
                //myUI("****Issuing USER" + "\r\n", txt_WebRx);
                judgment = Demo.USER(user) + "\r\n";      // mail User
                //myUI(judgment, txt_WebRx);
                //myUI("****Issuing PASS" + "\r\n", txt_WebRx);
                judgment = Demo.PASS(password) + "\r\n";      // mail Password
                //myUI(judgment, txt_WebRx);
                if (!judgment.Contains("-ERR"))
                {
                    //myUI("****Issuing NOOP" + "\r\n", txt_WebRx);
                    judgment = Demo.NOOP() + "\r\n";
                    //myUI(judgment, txt_WebRx);
                    if (judgment.Contains("+OK"))
                    {
                        //myUI("****Issuing STAT" + "\r\n", txt_WebRx);
                        judgment = Demo.STAT() + "\r\n";
                        //myUI(judgment, txt_WebRx);
                        //myUI("****Issuing LIST" + "\r\n", txt_WebRx);
                        judgment = Demo.LIST() + "\r\n";
                        //myUI(judgment, txt_WebRx);
                        //myUI("****Issuing RETR" + "\r\n", txt_WebRx);
                        // 印出最後一封mail
                        if (Demo.counter > 0)
                        {
                            judgment = Demo.RETR(Demo.counter) + "\r\n";

                            if (judgment.Contains("Sender: " + sender))
                            {
                                if (judgment.Contains("Alert Condition: " + test_case))
                                {
                                    myUI(judgment, txt_WebRx);
                                    return 1;
                                }
                            }
                            else
                            {
                                lblStatus.Text = "找不到來自" + sender + "的信件";
                                return 2;
                            }
                        }
                    }
                }
                ////myUI("****Issuing QUIT" + "\r\n", txt_WebRx);
                //judgment = Demo.QUIT() + "\r\n";
                ////myUI(judgment, txt_WebRx);
                return 2;
            }
            finally
            {
                //myUI("****Issuing QUIT" + "\r\n", txt_WebRx);
                judgment = Demo.QUIT() + "\r\n";
                //myUI(judgment, txt_WebRx);
            }
        }

        /// <summary>
        /// 回傳關鍵字所在的整行文字
        /// </summary>
        /// <param name="Key">目標關鍵字</param>
        /// <param name="stopSearch">如果目標關鍵字不存在，則搜尋到stopSearch時停止搜尋</param>
        /// <returns>回傳整行文字，關鍵字不存在則回傳string.Empty</returns>
        private string GetLine(string Key, string stopSearch)
        {
            int i;
            string[] line;
            string get_line = string.Empty;
            line = txt_Rx.Text.Split('\r');
            for (i = line.GetUpperBound(0); i >= 0; i--)
            {
                if (line[i].Contains(Key))
                {
                    get_line = line[i].Replace("\n", "");
                    break;  // for
                }
                else if (line[i].Contains(stopSearch))
                {
                    break;
                }
            }
            return get_line;
        }

        // 判斷陣列中有無string.empty，並刪除此元素
        private static bool isNotStringEmpty(string element)
        {
            return element != "";
        }

        private void txt_Tx_KeyDown(object sender, KeyEventArgs e)
        {
            // Initialize the flag to false.
            nonNumberEntered = false;
            int key = e.KeyValue;
            //if (e.Control != true)//如果沒按Ctrl鍵
            //    return;
            switch (key)
            {
                case 13:
                    // 按下Enter以後
                    SendCmd(txt_Tx.Text);
                    txt_Tx.Text = string.Empty;
                    nonNumberEntered = true;
                    break;
                case 38:
                    // 按下向上鍵以後
                    SendCmd(((char)27).ToString() + ((char)91).ToString() + ((char)65).ToString()); // ←[A
                    nonNumberEntered = true;
                    break;
                case 40:
                    // 按下向下鍵以後
                    SendCmd(((char)27).ToString() + ((char)91).ToString() + ((char)66).ToString()); // ←[B
                    nonNumberEntered = true;
                    break;
                default:
                    break;
            }
        }

        private void txt_Tx_KeyPress(object sender, KeyPressEventArgs e)
        {
            // KeyChar 無法抓取上下左右鍵
            // http://msdn.microsoft.com/zh-tw/library/system.windows.forms.keyeventargs.handled%28v=vs.110%29.aspx
            // Check for the flag being set in the KeyDown event.
            if (nonNumberEntered)
            {
                // Stop the character from being entered into the control since it is non-numerical.
                e.Handled = true;
            }
        }

        private void txt_WebTx_KeyDown(object sender, KeyEventArgs e)
        {
            nonNumberEntered = false;
            int key = e.KeyValue;
            //if (e.Control != true)//如果沒按Ctrl鍵
            //    return;
            switch (key)
            {
                case 13:
                    // 按下Enter以後
                    SendWeb(txt_WebTx.Text);
                    txt_WebTx.Text = string.Empty;
                    nonNumberEntered = true;
                    break;
                default:
                    break;
            }
        }

        private void txt_WebTx_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (nonNumberEntered)
            {
                // Stop the character from being entered into the control since it is non-numerical.
                e.Handled = true;
            }
        }

        private void consoleToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if (consoleToolStripMenuItem.Checked)
                tabControl1.SelectedTab = tabPage1;
            else if (tabControl1.SelectedTab == tabPage3)
                tabControl1.SelectedTab = tabPage3;
            else
                tabControl1.SelectedTab = tabPage2;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
                consoleToolStripMenuItem.Checked = true;
            else
                consoleToolStripMenuItem.Checked = false;
        }

        #region 自動保持 TextBox 垂直捲軸在最下方

        private void txt_Rx_TextChanged(object sender, EventArgs e)
        {
            // 自動保持捲軸在最下方
            txt_Rx.SelectionStart = txt_Rx.Text.Length;
            txt_Rx.ScrollToCaret();
        }

        private void txt_Note_TextChanged(object sender, EventArgs e)
        {
            txt_Note.SelectionStart = txt_Note.Text.Length;
            txt_Note.ScrollToCaret();
        }

        private void txt_Rx_EUT_TextChanged(object sender, EventArgs e)
        {
            // 自動保持捲軸在最下方
            txt_Rx_EUT.SelectionStart = txt_Rx_EUT.Text.Length;
            txt_Rx_EUT.ScrollToCaret();
        }

        private void txt_WebRx_TextChanged(object sender, EventArgs e)
        {
            // 自動保持捲軸在最下方
            txt_WebRx.SelectionStart = txt_WebRx.Text.Length;
            txt_WebRx.ScrollToCaret();
        }

        #endregion 自動保持 TextBox 垂直捲軸在最下方

        private void composingTmr_Tick(object sender, EventArgs e)
        {
            int idx, X_StartPos, Y_StartPos;
            int X, Y;   // every position(location) of the panel
            X_StartPos = 52; Y_StartPos = 25;    // initial position(location) of the panel
            row_num = (this.Height - Y_StartPos) / (lblFunction[0].Height * 2) - 6;
            for (idx = 0; idx < TestFun_MaxIdx; idx++)    // composing Label
            {
                X = X_StartPos + (idx / row_num) * X_StartPos * 3;
                Y = Y_StartPos + (lblFunction[idx].Height * (idx % row_num) * 2);
                lblFunction[idx].Location = new Point(X, Y);
                lblFunction[idx].Visible = true;
            }
            composingTmr.Enabled = false;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (lblFunction[0] != null)
            {
                composingTmr.Enabled = true;
            }
        }

        private void 從這個測項開始測試ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Test_Idx = MOUSE_Idx;
            chooseStart = true;
            cmdStart_Click(null, null);
        }

        private void 無限次測試這個測項ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmdOpeFile.Enabled = false;
            cmdStart.Enabled = false;
            cmdStop.Enabled = true;
            cmdNext.Enabled = false;
            Run_Stop = false;
            do
            {
                TEST_STATUS[MOUSE_Idx] = RunTest(MOUSE_Idx);
                if (STOP_WHEN_FAIL && Convert.ToInt32(TEST_STATUS[MOUSE_Idx]) == 2)
                {
                    return;
                }
                Hold(1000);
            } while (Run_Stop == false);
            cmdOpeFile.Enabled = true;
            cmdStart.Enabled = true;
            cmdStop.Enabled = true;
            cmdNext.Enabled = true;
        }

        private void 用Putty開啟ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] cmd;
            cmd = Convert.ToString(lblFunction[MOUSE_Idx].Tag).Split(' ');
            // Uses the ProcessStartInfo class to start new processes
            ProcessStartInfo startInfo = new ProcessStartInfo("putty.exe");
            startInfo.UseShellExecute = false;
            if (cmd[0].ToUpper() == "CONSOLE-DUT" || cmd[0].ToUpper() == "CONSOLE-EUT")
            {
                if (cmd[0].ToUpper() == "CONSOLE-DUT")
                {
                    serialPort1_Close();
                    //if (serialPort1.IsOpen) { serialPort1.Close(); }
                }
                else if (cmd[0].ToUpper() == "CONSOLE-EUT")
                {
                    //serialPort2_Close();
                    //if (serialPort2.IsOpen) { serialPort2.Close(); }
                }
                string info = "-serial COM" + cmd[1] + " -sercfg " + cmd[2] + ",8,n,1,n";
                startInfo.Arguments = info;
                Process.Start(startInfo);
            }
            else if (cmd[0].ToUpper() == "TELNET")
            {
                //USR = cmd[1];
                //if (cmd.GetUpperBound(0) > 1) { PWD = cmd[2]; }
                //else { PWD = string.Empty; }
                startInfo.Arguments = "-telnet -t " + TARGET_IP;
                Process.Start(startInfo);
                Hold(1000);
                SendKeys.SendWait(USR + "{ENTER}");
                Hold(1000);
                SendKeys.SendWait(PWD + "{ENTER}");
            }
            else if (cmd[0].ToUpper() == "POWER")
            {
            }
        }

        #region Hold / atop_timer

        public bool Hold(long timeout)
        {
            bool tmp_Hold = true;
            long delay = 0;
            WAIT = true;
            if (timeout > 0) { delay = timeout / 10; }
            while (WAIT)
            {
                Application.DoEvents();
                Thread.Sleep(10);
                if (timeout > 0)
                {
                    if (delay > 0)
                    {
                        delay -= 1;
                    }
                    else
                    {
                        tmp_Hold = false;   // 時間等到底
                        break;
                    }
                }
            }
            return tmp_Hold;
        }

        #endregion Hold / atop_timer

        #region lblStatus.ForeColor 隨著測試項目改變而變化Color

        // RGB to Hex
        // http://www.rapidtables.com/convert/color/rgb-to-hex.htm
        private void timer2_Tick(object sender, EventArgs e)
        {
            //Debug.Print(lblStatus.ForeColor.ToArgb().ToString());
            if (lblStatus.ForeColor.ToArgb() > 10 * 65536)
            {
                int hex_tmp = Convert.ToInt32(lblStatus.ForeColor.ToArgb());
                lblStatus.ForeColor = Color.FromArgb(hex_tmp - 50 * 65536);
            }
        }

        private void lblStatus_TextChanged(object sender, EventArgs e)
        {
            lblStatus.ForeColor = Color.FromArgb(255 * 65536);
        }

        #endregion lblStatus.ForeColor 隨著測試項目改變而變化Color

        public int ReCntTelnet(long timeout)
        {
            if (serialPort1.IsOpen)
            {
                WaitKey = "login";
                enterTmr.Enabled = true;    // 5秒按一次enter
                if (Hold(timeout * 1000) == false)
                {
                    enterTmr.Enabled = false;
                    return 2;
                }
                else
                {
                    enterTmr.Enabled = false;
                    return 1;
                }
            }
            else
            {
                int tm = 0;
                lblStatus.Text = "等待系統重開機...";
                do
                {
                    Hold(1000);
                    tm += 1;
                    if (tm > (timeout / 2))
                    {
                        lblStatus.Text = "連線失敗";
                        return 2; // 逾時
                    }
                } while (objping.Send(TARGET_IP, 1000).Status != IPStatus.Success);

                telnet = new TcpClient();
                if (!telnet.Connected)
                {
                    try
                    {
                        telnet.Connect(TARGET_IP, 23);   // 連接23端口 (Telnet的默認端口)
                        telentStream = telnet.GetStream();  // 建立網路資料流，將字串寫入串流

                        if (telnet.Connected)
                        {
                            //lblStatus.Text = "連線成功，正在登錄...";
                            lblStatus.Text = "正在登錄...";
                            Hold(1000);
                            // 背景telnet接收執行緒
                            ThreadStart backgroundReceive = new ThreadStart(telnet_Receive);
                            Thread rcvThread = new Thread(backgroundReceive);
                            rcvThread.IsBackground = true;
                            rcvThread.Start();

                            bytWrite_telnet = System.Text.Encoding.Default.GetBytes(USR + ((char)13).ToString());
                            telentStream.Write(bytWrite_telnet, 0, bytWrite_telnet.Length);
                            Hold(200);
                            bytWrite_telnet = System.Text.Encoding.Default.GetBytes(PWD + ((char)13).ToString());
                            telentStream.Write(bytWrite_telnet, 0, bytWrite_telnet.Length);
                            lblStatus.Text = "連線成功";
                            return 1;
                        }
                    }
                    catch (Exception)
                    {
                        return 2;   // 目標主機連線沒反應
                    }
                }
            }
            return 2;
        }

        #region 驗證IP

        /// <summary>
        /// 驗證IP
        /// </summary>
        /// <param name="source"></param>
        /// <returns>規則運算式尋找到符合項目，則為 true，否則為 false</returns>
        public static bool IsIP(string source)
        {
            // Regex.IsMatch 方法 (String, String, RegexOptions)
            // 表示指定的規則運算式是否使用指定的比對選項，在指定的輸入字串中尋找相符項目
            return Regex.IsMatch(source, @"^(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])$", RegexOptions.IgnoreCase);
        }

        public static bool HasIP(string source)
        {
            return Regex.IsMatch(source, @"(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])", RegexOptions.IgnoreCase);
        }

        #endregion 驗證IP

        /// <summary>
        /// todo: 判斷指定的本機 Udp 端口（只判斷端口）是否被占用
        /// </summary>
        public bool PortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveUdpListeners();
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;    // 占用中
                }
            }
            return false;
        }

        #region FTP

        /// <summary>
        /// FTP 上傳檔案至目標位置
        /// </summary>
        /// <param name="FTPAddress">目標位置</param>
        /// <param name="filePath">上傳的檔案</param>
        /// <param name="username">帳號</param>
        /// <param name="password">密碼</param>
        public void uploadFile(string IP, string filePath, string username, string password)
        {
            //if (!IP.StartsWith("ftp://")) { IP = "ftp://" + IP; }
            string FTPAddress = "ftp://" + IP;
            //Create FTP request
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPAddress + "/" + Path.GetFileName(filePath));
            request.Method = WebRequestMethods.Ftp.UploadFile;
            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential(username, password);
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = false;
            request.ReadWriteTimeout = 7000;
            request.Timeout = 3000;

            //Load the file
            FileStream stream = File.OpenRead(filePath);
            byte[] buffer = new byte[stream.Length];

            stream.Read(buffer, 0, buffer.Length);
            stream.Close();

            //Upload file
            Stream reqStream = request.GetRequestStream();
            reqStream.Write(buffer, 0, buffer.Length);
            reqStream.Close();

            //Debug.Print("Uploaded Successfully !");
        }

        /// <summary>
        /// 列出 FTP 目錄的內容，並檢查檔案是否存在內容中。
        /// </summary>
        /// <param name="IP">目標 IP</param>
        /// <param name="fileName">欲檢查的檔案</param>
        /// <param name="username">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns>true代表存在；false代表不存在</returns>
        public bool checkFile(string IP, string fileName, string username, string password)
        {
            string FTPAddress = "ftp://" + IP;
            //Create FTP request
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPAddress);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential(username, password);
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = false;
            request.ReadWriteTimeout = 5000;
            request.Timeout = 3000;

            string responseTmp = string.Empty;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            responseTmp = reader.ReadToEnd();
            if (responseTmp.Contains(fileName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///* Rename File */
        //public void renameFile(string IP, string currentFileNameAndPath, string newFileName, string username, string password)
        //{
        //    string FTPAddress = "ftp://" + IP + "//jffs2";
        //    //Create FTP request
        //    FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPAddress);
        //    // This example assumes the FTP site uses anonymous logon.
        //    request.Credentials = new NetworkCredential(username, password);
        //    /* When in doubt, use these options */
        //    request.UseBinary = true;
        //    request.UsePassive = true;
        //    request.KeepAlive = false;
        //    /* Specify the Type of FTP Request */
        //    request.Method = WebRequestMethods.Ftp.Rename;
        //    /* Rename the File */
        //    request.RenameTo = newFileName;
        //    /* Establish Return Communication with the FTP Server */
        //    request = (FtpWebResponse)request.GetResponse();
        //    /* Resource Cleanup */
        //    request.Close();
        //    request = null;
        //}

        ///* Delete File */
        //public void deleteFile(string deleteFile)
        //{
        //    try
        //    {
        //        /* Create an FTP Request */
        //        ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + deleteFile);
        //        /* Log in to the FTP Server with the User Name and Password Provided */
        //        ftpRequest.Credentials = new NetworkCredential(user, pass);
        //        /* When in doubt, use these options */
        //        ftpRequest.UseBinary = true;
        //        ftpRequest.UsePassive = true;
        //        ftpRequest.KeepAlive = true;
        //        /* Specify the Type of FTP Request */
        //        ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
        //        /* Establish Return Communication with the FTP Server */
        //        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
        //        /* Resource Cleanup */
        //        ftpResponse.Close();
        //        ftpRequest = null;
        //    }
        //    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        //    return;
        //}

        #endregion FTP

        #region 貓頭鷹 v1.6p (2013/11/22)

        private void MultiPortTesting_settings(string ip, int interval, string max_port, int server_port, int loopback, string duration)
        {
            int i;
            int min_port;

            if (max_port.Contains("-")) // 格式ex: 0-4、1-4
            {
                string[] port;
                port = max_port.Split(new char[] { '-' });      // 0-4 跳port數；1-4 全port數
                min_port = Convert.ToInt32(port[0]);
                max_port = port[1];
            }
            else
            {
                min_port = Convert.ToInt32(max_port);    // 格式ex: 4   指定單一port
            }

            if (File.Exists(appPATH + "\\setting.txt"))
            {
                File.Delete(appPATH + "\\setting.txt");
            }

            // 建立檔案
            FileStream fs = File.Open(appPATH + "\\setting.txt", FileMode.OpenOrCreate, FileAccess.Write);
            // 建構StreamWriter物件
            StreamWriter sw = new StreamWriter(fs);

            // 寫入
            sw.WriteLine(ip);           // IP
            sw.WriteLine("50");         // Send Lenth
            sw.WriteLine(interval);     // Send Interval
            sw.WriteLine(max_port);     // total port
            sw.WriteLine(server_port);
            sw.WriteLine(server_port);
            sw.WriteLine("5");          // timeout
            sw.WriteLine("0");          // pingpong
            sw.WriteLine("0");
            sw.WriteLine("0");
            sw.WriteLine("0");
            sw.WriteLine("0");
            sw.WriteLine("True");
            sw.WriteLine("False");
            sw.WriteLine("False");
            sw.WriteLine("0");
            sw.WriteLine(loopback);
            //sw.WriteLine(duration);
            sw.WriteLine("9999");
            for (i = 1; i <= 32; i++)
            {
                if (min_port <= i && i <= Convert.ToInt32(max_port))
                {
                    if (min_port == 0)
                    {
                        if (i % 2 == 1)
                        {
                            sw.WriteLine(Convert.ToString(Math.Abs(loopback - 1)));
                        }
                        else
                        {
                            sw.WriteLine(loopback);
                        }
                    }
                    else
                    {
                        sw.WriteLine("1");
                    }
                }
                else
                {
                    sw.WriteLine("0");
                }
            }

            // 清除目前寫入器(Writer)的所有緩衝區，並且造成任何緩衝資料都寫入基礎資料流
            sw.Flush();

            // 關閉目前的StreamWriter物件和基礎資料流
            sw.Close();
            fs.Close();
        }

        #endregion 貓頭鷹 v1.6p (2013/11/22)

        private float TimeUnit(int idx, int i)
        {
            string[] line;
            string tag = Convert.ToString(lblFunction[idx].Tag);
            line = tag.Split(' ');
            if (line.GetUpperBound(0) >= i + 1)
            {
                switch (line[i + 1].ToLower())
                {
                    case "s":
                        return Convert.ToInt64(line[i]) * 1;
                    case "m":
                        return Convert.ToInt64(line[i]) * 60;
                    case "h":
                        return Convert.ToInt64(line[i]) * 60 * 60;
                    case "d":
                        return Convert.ToInt64(line[i]) * 60 * 60 * 24;
                    default:
                        return Convert.ToInt64(line[i]) * 60;
                }
            }
            else { return Convert.ToInt64(line[i]) * 60; }
        }

        private void pause(double delay)
        {
            DateTime time_before = DateTime.Now;
            while (((TimeSpan)(DateTime.Now - time_before)).TotalMinutes < delay)
            {
                Application.DoEvents();
                Thread.Sleep(1000); // 至少打資料兩次
            }
        }

        private void lblSecret_Click(object sender, EventArgs e)
        {
            secretX += 1;
            if (secretX == 5)
            {
                debugMode.Visible = true;
                txt_Rx_EUT.Visible = true;
            }
        }

        private void disconnectALL_Click(object sender, EventArgs e)
        {
            int n;
            if (telnet.Connected) { telnet.Close(); }
            serialPort1_Close();
            //serialPort2_Close();
            //if (serialPort1.IsOpen) { serialPort1.Close(); }
            //if (serialPort2.IsOpen) { serialPort2.Close(); }

            Run_Stop = true;
            WAIT = false;

            for (n = 0; n < TestFun_MaxIdx; n++)
            {
                if (lblFunction[n].Text.ToUpper().Contains("CONSOLE-DUT") || lblFunction[n].Text.ToUpper().Contains("CONSOLE-EUT") || lblFunction[n].Text.ToUpper().Contains("TELNET"))
                {
                    lblFunction[n].BackColor = Color.FromArgb(255, 255, 255);
                }
            }
            lblStatus.Text = "所有的連線已經中斷";
        }

        private void lanEnvironmentSetting_Click(object sender, EventArgs e)
        {
            Shell(appPATH, "LAN_setting.bat");
        }

        private void 開啟Monitor_Click(object sender, EventArgs e)
        {
            Shell(appPATH, "monitor2.5.exe");
        }

        private void 執行TFTPServer_Click(object sender, EventArgs e)
        {
            Shell(appPATH + "\\tftpd32.400", "tftpd32.exe");
        }

        // http://msdn.microsoft.com/zh-cn/library/aa168292(office.11).aspx
        // 設定必要的物件
        // 按照順序分別是Application > Workbook > Worksheet > Range > Cell
        // (1) Application ：代表一個 Excel 程序。
        // (2) WorkBook ：代表一個 Excel 工作簿。
        // (3) WorkSheet ：代表一個 Excel 工作表，一個 WorkBook 包含好幾個工作表。
        // (4) Range ：代表 WorkSheet 中的多個單元格區域。
        // (5) Cell ：代表 WorkSheet 中的一個單元格。
        private void writeExcelLog()
        {
            int j;

            // 檢查路徑的資料夾是否存在，沒有則建立
            if (!Directory.Exists(@"C:\Atop_Log\ATC\" + MODEL_NAME))
            {
                Directory.CreateDirectory(@"C:\Atop_Log\ATC\" + MODEL_NAME);
            }

            // 設定儲存檔名，不用設定副檔名，系統自動判斷 excel 版本，產生 .xls 或 .xlsx 副檔名
            // C:\Atop_Log\ATC\產品名稱\年_月_工單號碼.xls
            time = DateTime.Now;
            string name = time.Year + "_" + time.Month + "_" + productNum_forExcel.ToUpper() + ".xls";
            string pathFile = @"C:\Atop_Log\ATC\" + MODEL_NAME + @"\" + name;

            Microsoft.Office.Interop.Excel.Application excelApp;
            Microsoft.Office.Interop.Excel._Workbook wBook;
            Microsoft.Office.Interop.Excel._Worksheet wSheet;
            Microsoft.Office.Interop.Excel.Range wRange;

            // 開啟一個新的應用程式
            excelApp = new Microsoft.Office.Interop.Excel.Application();
            // 讓Excel文件可見
            excelApp.Visible = false;
            // 停用警告訊息
            excelApp.DisplayAlerts = false;
            // 開啟舊檔案
            if (GetFiles(pathFile))
            {
                wBook = excelApp.Workbooks.Open(pathFile, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }
            // 創建一個新的工作簿
            excelApp.Workbooks.Add(Type.Missing);
            // 引用第一個活頁簿
            wBook = excelApp.Workbooks[1];
            // 設定活頁簿焦點
            wBook.Activate();
            // 設定開啟密碼
            //wBook.Password = "23242249";

            try
            {
                // 引用第一個工作表(轉型)
                wSheet = (Microsoft.Office.Interop.Excel._Worksheet)wBook.Worksheets[1];
                // 命名工作表的名稱
                wSheet.Name = "測試紀錄";
                // Worksheet.Protect 方法。保護工作表，使工作表無法修改
                wSheet.Protect("23242249", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true);
                // 設定工作表焦點
                wSheet.Activate();
                // 所有儲存格 文字置中
                excelApp.Cells.HorizontalAlignment = 3;
                // 所有儲存格 自動換行
                excelApp.Cells.WrapText = true;
                // 所有儲存格格式強迫以文字來儲存
                excelApp.Cells.NumberFormat = "@";

                // 設定第1列資料
                excelApp.Cells[1, 1] = "測試人員";
                excelApp.Cells[1, 2] = "工單號碼";
                excelApp.Cells[1, 3] = "產品序號(SN)";
                excelApp.Cells[1, 4] = "產品名稱";
                excelApp.Cells[1, 5] = "MAC1";
                excelApp.Cells[1, 6] = "Kernel";
                excelApp.Cells[1, 7] = "AP";
                excelApp.Cells[1, 8] = "開始測試時間";
                excelApp.Cells[1, 9] = "結束測試時間";
                // 取得已經使用的Columns數(X軸)
                //int usedRangeColumns = wSheet.UsedRange.Columns.Count + 1;
                //for (j = usedRangeColumns; j < TEST_RESULT.Count + usedRangeColumns; j++)
                //{
                //    excelApp.Cells[1, j] = TEST_RESULT[j - usedRangeColumns];
                //}
                for (j = 10; j < TEST_FunLog.Count + 10; j++)
                {
                    excelApp.Cells[1, j] = TEST_FunLog[j - 10];
                    //Debug.Print(TEST_FunLog[j - 10].ToString());
                }
                // 設定第1列顏色
                wRange = wSheet.get_Range(wSheet.Cells[1, 1], wSheet.Cells[1, TEST_FunLog.Count + 9]);
                wRange.Select();
                wRange.Font.Color = ColorTranslator.ToOle(Color.White);
                wRange.Interior.Color = ColorTranslator.ToOle(Color.DimGray);
                //wRange.Columns.AutoFit();   // 自動調整欄寬
                wRange.ColumnWidth = 15; // 設置儲存格的寬度

                // 取得已經使用的Rows數(Y軸)
                int usedRangeRows = wSheet.UsedRange.Rows.Count + 1;
                // 設定第usedRange列資料
                excelApp.Cells[usedRangeRows, 1] = tester_forExcel.ToUpper();
                excelApp.Cells[usedRangeRows, 2] = productNum_forExcel.ToUpper();
                string snTemp = string.Empty;
                if (coreSN_forExcel != string.Empty && coreSN_forExcel != null)
                {
                    snTemp = "Core:" + coreSN_forExcel;
                }
                if (lanSN_forExcel != string.Empty && lanSN_forExcel != null)
                {
                    if (snTemp == string.Empty)
                    {
                        snTemp = "Lan:" + lanSN_forExcel;
                    }
                    else
                    {
                        snTemp = snTemp + ((char)10).ToString() + "Lan:" + lanSN_forExcel;
                    }
                }
                if (uartSN_forExcel != string.Empty && uartSN_forExcel != null)
                {
                    if (snTemp == string.Empty)
                    {
                        snTemp = "Uart:" + uartSN_forExcel;
                    }
                    else
                    {
                        snTemp = snTemp + ((char)10).ToString() + "Uart:" + uartSN_forExcel;
                    }
                }
                if (serial1SN_forExcel != string.Empty && serial1SN_forExcel != null)
                {
                    if (snTemp == string.Empty)
                    {
                        snTemp = "Serial1:" + serial1SN_forExcel;
                    }
                    else
                    {
                        snTemp = snTemp + ((char)10).ToString() + "Serial1:" + serial1SN_forExcel;
                    }
                }
                if (serial2SN_forExcel != string.Empty && serial2SN_forExcel != null)
                {
                    if (snTemp == string.Empty)
                    {
                        snTemp = "Serial2:" + serial2SN_forExcel;
                    }
                    else
                    {
                        snTemp = snTemp + ((char)10).ToString() + "Serial2:" + serial2SN_forExcel;
                    }
                }
                if (serial3SN_forExcel != string.Empty && serial3SN_forExcel != null)
                {
                    if (snTemp == string.Empty)
                    {
                        snTemp = "Serial3:" + serial3SN_forExcel;
                    }
                    else
                    {
                        snTemp = snTemp + ((char)10).ToString() + "Serial3:" + serial3SN_forExcel;
                    }
                }
                if (serial4SN_forExcel != string.Empty && serial4SN_forExcel != null)
                {
                    if (snTemp == string.Empty)
                    {
                        snTemp = "Serial4:" + serial4SN_forExcel;
                    }
                    else
                    {
                        snTemp = snTemp + ((char)10).ToString() + "Serial4:" + serial4SN_forExcel;
                    }
                }
                excelApp.Cells[usedRangeRows, 3] = snTemp;
                excelApp.Cells[usedRangeRows, 4] = MODEL_NAME;
                excelApp.Cells[usedRangeRows, 5] = "";
                excelApp.Cells[usedRangeRows, 6] = "";
                excelApp.Cells[usedRangeRows, 7] = "";
                excelApp.Cells[usedRangeRows, 8] = startTime;
                excelApp.Cells[usedRangeRows, 9] = endTime;
                for (j = 10; j < TEST_FunLog.Count + 10; j++)
                {
                    excelApp.Cells[usedRangeRows, j] = TEST_RESULT[j - 10];
                }

                try
                {
                    // 另存活頁簿
                    wBook.SaveAs(pathFile, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                    MessageBox.Show("Excel log 儲存於 " + Environment.NewLine + pathFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("儲存檔案出錯，檔案可能正在使用" + Environment.NewLine + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("產生 Excel log 時出錯！" + Environment.NewLine + ex.Message);
            }

            //關閉活頁簿
            wBook.Close(false, Type.Missing, Type.Missing);

            //關閉Excel
            excelApp.Quit();

            //釋放Excel資源
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            wBook = null;
            wSheet = null;
            wRange = null;
            excelApp = null;
            GC.Collect();
        }

        // 讀取目錄下所有檔案，並判斷指定檔案(不含副檔名)是否存在
        private bool GetFiles(string filename)
        {
            int i;
            string[] files;
            string keyword;

            files = Directory.GetFiles(@"C:\Atop_Log\ATC\" + MODEL_NAME);
            keyword = filename.Replace("C:\\Atop_Log\\ATC\\" + MODEL_NAME + "\\", string.Empty);
            for (i = 0; i < files.Length; i++)
            {
                files[i] = files[i].Replace(@"C:\Atop_Log\ATC\" + MODEL_NAME + "\\", string.Empty);
                if (files[i].Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }

        private void cmdNext_Click(object sender, EventArgs e)
        {
            int n;
            if (cmdOpeFile.Text != "檔案名稱")
            {
                InputBox inputbox = new InputBox();
                inputbox.ShowDialog();
                tester_forExcel = InputBox.tester;
                productNum_forExcel = InputBox.productNum;
                coreSN_forExcel = InputBox.coreSN;
                lanSN_forExcel = InputBox.lanSN;
                uartSN_forExcel = InputBox.uartSN;
                serial1SN_forExcel = InputBox.serial1SN;
                serial2SN_forExcel = InputBox.serial2SN;
                serial3SN_forExcel = InputBox.serial3SN;
                serial4SN_forExcel = InputBox.serial4SN;

                time = DateTime.Now;
                endTime = String.Format("{0:00}/{1:00}" + ((char)10).ToString() + "{2:00}:{3:00}:{4:00}", time.Month, time.Day, time.Hour, time.Minute, time.Second);

                writeExcelLog();

                Shell(appPATH, "arp-d.bat");
                if (telnet.Connected) { telnet.Close(); }
                serialPort1_Close();
                //serialPort2_Close();

                Test_Idx = 0;
                Run_Stop = true;
                WAIT = false;
                txt_Rx.Text = string.Empty;
                for (n = 0; n < TestFun_MaxIdx; n++)
                {
                    lblFunction[n].BackColor = Color.FromArgb(255, 255, 255);
                }
                for (n = 0; n < TEST_RESULT.Length; n++)
                {
                    TEST_RESULT[n] = string.Empty;
                }
            }
        }

        /// <summary>
        /// 為了設備Restart後，等待login字串而沒登入成功，導致循環測試無法順利，所以加此timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void enterTmr_Tick(object sender, EventArgs e)
        {
            SendCmd("");
        }

        private void txtDutIP_TextChanged(object sender, EventArgs e)
        {
            if (IsIP(txtDutIP.Text))
            {
                TARGET_IP = txtDutIP.Text;
                txtDutIP.ForeColor = Color.Green;
            }
            else
            {
                txtDutIP.ForeColor = Color.Red;
            }
        }

        public void ReceiveBroadcast()
        {
            try
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, UDPport);
                aLabel:
                while (true)    // 條件永遠成立，是無窮迴圈
                {
                    byte[] bytes = udpClient.Receive(ref ip);
                    if ((bytes[0] == 1 || bytes[0] == 3) && bytes[4] == Convert.ToByte(Convert.ToUInt64("92", 16)) && bytes[5] == Convert.ToByte(Convert.ToUInt64("DA", 16)))
                    {
                        string tmpstr = string.Empty;
                        int idx, i;

                        // get IP
                        tmpIP = Convert.ToString(bytes[12] + "." + bytes[13] + "." + bytes[14] + "." + bytes[15]);
                        string displayIP = tmpIP;
                        for (i = tmpIP.Length; i <= 15; i++)    // 對齊IP長度(lenth 15)
                        {
                            displayIP = displayIP + " ";
                        }
                        // get Netmask
                        string tmpNetmask = Convert.ToString(bytes[236] + "." + bytes[237] + "." + bytes[238] + "." + bytes[239]);
                        // get MAC
                        string tmpMAC = TenToSixteen(bytes, 28, 6).ToUpper();
                        // get Model
                        string tmpModel = DecToChar(bytes, 44, 16);
                        // get Kernel
                        string tmpKernel = "[Kernel]:v" + Convert.ToString(bytes[109] + "." + bytes[108]);
                        // get AP version
                        tmpstr = DecToChar(bytes, 110, 125);
                        string tmpAP = "[AP]:" + tmpstr;
                        // get Hostname
                        string tmphostname = DecToChar(bytes, 90, 16);

                        if (!tmpModel.ToUpper().Contains(MODEL_NAME))
                        {
                            goto aLabel;
                        }

                        // 比對有無重複的資料
                        for (idx = 0; idx < DevCount; idx++)
                        {
                            if (string.Equals(DA2[idx, 2], tmpMAC)) // MAC already recorded
                            {
                                if (string.Equals(DA2[idx, 0], tmpIP))
                                {
                                    goto aLabel;    // IP is the same then not necessary to be updated
                                }
                            }
                        }

                        if (idx == DevCount)
                        {
                            if (DevCount >= 300)
                            {
                                break;
                            }  // break while
                        }

                        DA2[idx, 0] = tmpIP;
                        DA2[idx, 1] = tmpNetmask;
                        DA2[idx, 2] = tmpMAC;
                        // IP
                        DA2[idx, 3] = Convert.ToString(bytes[12]);
                        DA2[idx, 4] = Convert.ToString(bytes[13]);
                        DA2[idx, 5] = Convert.ToString(bytes[14]);
                        DA2[idx, 6] = Convert.ToString(bytes[15]);
                        // Model name
                        DA2[idx, 7] = tmpModel;
                        DA2[idx, 8] = string.Empty;
                        //DA2[i, 8] = CStr(ProcStep);
                        // MAC
                        DA2[idx, 9] = Convert.ToString(bytes[28]);
                        DA2[idx, 10] = Convert.ToString(bytes[29]);
                        DA2[idx, 11] = Convert.ToString(bytes[30]);
                        DA2[idx, 12] = Convert.ToString(bytes[31]);
                        DA2[idx, 13] = Convert.ToString(bytes[32]);
                        DA2[idx, 14] = Convert.ToString(bytes[33]);
                        // Netmask
                        DA2[idx, 15] = Convert.ToString(bytes[236]);
                        DA2[idx, 16] = Convert.ToString(bytes[237]);
                        DA2[idx, 17] = Convert.ToString(bytes[238]);
                        DA2[idx, 18] = Convert.ToString(bytes[239]);
                        // last 2 bytes of Gateway
                        DA2[idx, 19] = Convert.ToString(bytes[26]);
                        DA2[idx, 20] = Convert.ToString(bytes[27]);
                        // Hostname
                        DA2[idx, 21] = tmphostname;
                        // Kernel
                        DA2[idx, 22] = tmpKernel;
                        // AP
                        DA2[idx, 23] = tmpAP;

                        // Devices quantity counter
                        DevCount += 1;

                        //string stringData = DA2[idx, 2] + ", " + displayIP + ", " + DA2[idx, 7] + ", " + DA2[idx, 22] + ", " + DA2[idx, 23] + ", [Host name]:" + DA2[idx, 21];
                        stringData[idx] = DA2[idx, 2] + ", " + displayIP + ", " + DA2[idx, 7] + ", " + DA2[idx, 22] + ", " + DA2[idx, 23] + ", [Host name]:" + DA2[idx, 21];
                        Debug.Print(stringData[idx]);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public string TenToSixteen(byte[] bdata, int Start, int Count)
        {
            int i;
            string op_str = string.Empty;
            string tmpstr = string.Empty;
            int EndNum = Start + Count;
            for (i = Start; i < EndNum; i++)
            {
                tmpstr = Convert.ToString(bdata[i], 16); //十進制轉十六進制,Convert.ToString(166, 16));
                if (tmpstr.Length < 2)
                {
                    tmpstr = "0" + tmpstr;
                }
                op_str = op_str + tmpstr;
            }
            return op_str;
        }

        public string DecToChar(byte[] bdata, int Start, int Count)
        {
            int i;
            string op_str = string.Empty;
            int EndNum = Start + Count;
            for (i = Start; i < EndNum; i++)
            {
                short ch = bdata[i];
                if (ch == 0)
                {
                    break;
                } // break for
                op_str = op_str + Convert.ToChar(ch); // vb6,ChrW() => c#,Convert.ToChar()
            }
            return op_str;
        }
    }
}

/*
 -----選取 columns-----
 xlWs.columns("H").select	'選取單行
 xlWs.columns("E:H").select	'選取連續行
 xlWs.columns("E:E,H:H")	'選取多行
 xlWs.range("E:E,G:G").select	'用range選取多行
 xlWs.columns.select	'選取全部行 = 全選
 -----用數字選取 columns-----
 xlWs.columns(3).select	'選取第3行
 xlWs.columns(i).select	'單選第i行

 xlWs.columns(i).columnwidth = 5	'第i行的欄寬=5
 xlWs.range("C:C,E:E,G:G").columnwidth = 5
 xlWs.columns(i).AutoFit	'第i行的欄寬=最適欄寛
 xlWs.columns("D:F").delete	'刪除行
 xlWs.range("C:C,E:E,G:G").delete	'刪除行

 -----選取 rows-----
 xlWs.rows(i).select	'選取單列
 xlWs.rows("2:6").select	'選取連續列
 xlWs.rows.select	'選取全部列 = 全選
 xlWs.range("3:3, 5:5, 8:8").select	'選取多列

 xlWs.rows(3).rowheight = 5	'列高
 xlWs.rows(3).insert	'插入列
 xlWs.rows(3).delete	'刪除列

 -----選取 cells-----
 xlWs.range("D4:D4").select	'選取單格
 xlWs.range("B2:H6").select	'選取範圍
 xlWs.range("D2:B5, F8:I9").select	'選取多個範圍

 xlWs.range("D4") = "TEST"	'儲存格內容
 xlWs.range("D4").font.name = "cambria"	'設定字型
 xlWs.range("D4").font.size = 24	'設定字體
 xlWs.range("D4").font.bold = true	'粗體
 xlWs.range("D4").font.color = vbblue	'設定文字顏色
 xlWs.range("D4").Interior.colorindex = 36	'設定背景顏色

 -----合併儲存格-----
 xlWs.range("E5:I6").mergecells = true	'合併儲存格
 tstring = "E" & i & ":" & "I" & j
 xlWs.range(tstring).mergecells = true	'合併儲存格

 -----儲存格對齊-----
 xlWs.range("D4").verticalalignment = 2	'上下對齊
 1=靠上 , 2=置中 , 3=靠下 , 4=垂直對齊??
 xlWs.range("D4").horizontalalignment = 1	'左右對齊
 1=一般 , 2=置左 , 3=置中 , 4=靠右 , 5=填滿 , 6=水平對齊? , 7=跨欄置中

 -----儲存格框線-----
 xlWs.range("D4").borders(n)	'框線方向
 n= 1:左, 2:右, 3:上, 4:下, 5:斜, 6:斜
 xlWs.range("D4").borders(4).color = 5
 xlWs.range("D4").borders(4).weight = 3	'框線粗細
 xlWs.range("D4").borders(4).linestyle = 1	'框線樣式
 線種類= 1,7:細實 2:細虛 4:一點虛 9:雙細實線
 xlWs.range("D4").borders(4).color = 6

 -----儲存格計算-----
 xlWs.range("I17").value = 20
 xlWs.range("I18").value = 30
 xlWs.Range("I19").Formula = xlWs.Range("I17") * xlWs.Range("I18") / 100
 xlWs.Range("I20").Formula = "=SUM(I17:I19)"

 -----加入註解-----
 xlWs.cells(n,1).AddComment
 xlWs.cells(n,1).Comment.visible = False
 xlWs.cells(n,1).Comment.text("有建BOM表,卻不計算BOM的成本")
 -----讀取註解,待測-----
 comment-text = xlWs.cells(n,1).Comment.text()
 comment-text = xlWs.cells(n,1).Comment.text

 -----列出 excel 字體顏色 color values-----
 for i = 1 to 56
 xlWs.cells(i + 3, 1).value = "value = " & i
 xlWs.cells(i + 3, 2).interior.colorindex = i
 next
*/