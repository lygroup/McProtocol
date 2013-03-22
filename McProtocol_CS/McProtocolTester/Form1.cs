using System.Windows.Forms;
using Nc.Mitsubishi;

namespace McProtocolTester
{
    public partial class Form1 : Form
    {
        private Plc FApp;
        public Form1()
        {
            this.FApp = new McProtocolTcp("192.168.40.103", 0x1394);
//            this.FApp = new McProtocolUdp("192.168.40.103", 0x1395);
            this.FApp.Open();
            InitializeComponent();
        }

        private void comboBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ComboBox cb = this.comboBox1;
                string buff = cb.Text;
                if (0 < buff.IndexOf(','))
                {   // "D10,2"のパターン
                    string[] s = buff.Split(',');
                    if (1 < s.Length)   
                    {
                        PlcDeviceType type;
                        int addr;
                        McProtocolTcp.GetDeviceCode(s[0], out type, out addr);

                        int[] val = new int[int.Parse(s[1])];
                        int rtCode;
                        if (McProtocolApp.IsBitDevice(type))
                        {
                            rtCode = this.FApp.GetBitDevice(s[0], val.Length, val);
                        }
                        else
                        {
                            rtCode = this.FApp.ReadDeviceBlock(s[0], val.Length, val);
                        }
                        if (0 < rtCode)
                        {
                            this.listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                        }
                        else
                        {
                            for (int i = 0; i < val.Length; ++i)
                            {
                                this.listBox1.Items.Add(type.ToString() + (addr + i).ToString() + "=" + val[i].ToString());
                            }
                        }
                    }
                }
                else if (0 < buff.IndexOf('='))
                {
                    string[] s = buff.Split('=');
                    if (0 < s[0].IndexOf(".."))
                    {   // "D10..12=0"のパターン
                        string[] t = s[0].Replace("..", "=").Split('=');
                        int m;
                        int n = int.Parse(t[1]);
                        PlcDeviceType type;
                        McProtocolTcp.GetDeviceCode(t[0], out type, out m);
                        int[] data = new int[n - m + 1];
                        int v = int.Parse(s[1]);
                        for (int i = 0; i < data.Length; ++i)
                        {
                            data[i] = v;
                        }
                        int rtCode;
                        if (McProtocolApp.IsBitDevice(type))
                        {
                            rtCode = this.FApp.SetBitDevice(t[0], data.Length, data);
                        }
                        else
                        {
                            rtCode = this.FApp.WriteDeviceBlock(t[0], data.Length, data);
                        }
                        this.listBox1.Items.Add(buff.ToUpper());
                        if (0 < rtCode)
                        {
                            this.listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                        }
                    }
                    else
                    {   // "D10=0"のパターン
                        PlcDeviceType type;
                        int addr;
                        McProtocolTcp.GetDeviceCode(s[0], out type, out addr);

                        int val = int.Parse(s[1]);
                        int rtCode;
                        if (McProtocolApp.IsBitDevice(type))
                        {
                            int[] data = new int[1];
                            data[0] = val;
                            rtCode = this.FApp.SetBitDevice(s[0], data.Length, data);
                        }
                        else
                        {
                            rtCode = this.FApp.SetDevice(s[0], val);
                        }
                        this.listBox1.Items.Add(buff.ToUpper());
                        if (0 < rtCode)
                        {
                            this.listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                        }
                    }
                }
                else
                {   // "D10"のパターン
                    PlcDeviceType type;
                    int addr;
                    McProtocolTcp.GetDeviceCode(buff.ToUpper(), out type, out addr);

                    int n;
                    int rtCode;
                    if (McProtocolApp.IsBitDevice(type))
                    {
                        int[] data = new int[1];
                        rtCode = this.FApp.GetBitDevice(buff, data.Length, data);
                        n = data[0];
                    }
                    else
                    {
                        rtCode = this.FApp.GetDevice(buff.ToUpper(), out n);
                    }
                    this.listBox1.Items.Add(buff.ToUpper() + "=" + n.ToString());
                    if (0 < rtCode)
                    {
                        this.listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                    }
                }
                this.listBox1.SelectedIndex = this.listBox1.Items.Count - 1;
                cb.Items.Insert(0, cb.Text);
                cb.Text = "";
            }
        }
    }
}
