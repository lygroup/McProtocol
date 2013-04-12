using System.Globalization;
using System.Windows.Forms;
using Nc.Mitsubishi;

namespace McProtocolTester
{
    public partial class Form1 : Form
    {
        private readonly Plc FApp;
        public Form1()
        {
            FApp = new McProtocolTcp("192.168.40.103", 0x1394);
//            FApp = new McProtocolUdp("192.168.40.103", 0x1395);
            FApp.Open();
            InitializeComponent();
        }

        private void comboBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ComboBox cb = comboBox1;
                string buff = cb.Text;
                if (0 < buff.IndexOf(','))
                {   // "D10,2"のパターン
                    string[] s = buff.Split(',');
                    if (1 < s.Length)   
                    {
                        PlcDeviceType type;
                        int addr;
                        McProtocolApp.GetDeviceCode(s[0], out type, out addr);

                        var val = new int[int.Parse(s[1])];
                        int rtCode = McProtocolApp.IsBitDevice(type) ? FApp.GetBitDevice(s[0], val.Length, val) : 
                                                                       FApp.ReadDeviceBlock(s[0], val.Length, val);
                        if (0 < rtCode)
                        {
                            listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                        }
                        else
                        {
                            for (int i = 0; i < val.Length; ++i)
                            {
                                listBox1.Items.Add(type.ToString() + (addr + i).ToString(CultureInfo.InvariantCulture) + "=" + val[i].ToString(CultureInfo.InvariantCulture));
                            }
                        }
                    }
                }
                else if (0 < buff.IndexOf('='))
                {
                    string[] s = buff.Split('=');
                    if (0 < s[0].IndexOf("..", System.StringComparison.Ordinal))
                    {   // "D10..12=0"のパターン
                        string[] t = s[0].Replace("..", "=").Split('=');
                        int m;
                        int n = int.Parse(t[1]);
                        PlcDeviceType type;
                        McProtocolApp.GetDeviceCode(t[0], out type, out m);
                        var data = new int[n - m + 1];
                        int v = int.Parse(s[1]);
                        for (int i = 0; i < data.Length; ++i)
                        {
                            data[i] = v;
                        }
                        int rtCode = McProtocolApp.IsBitDevice(type) ? FApp.SetBitDevice(t[0], data.Length, data) : 
                                                                       FApp.WriteDeviceBlock(t[0], data.Length, data);
                        listBox1.Items.Add(buff.ToUpper());
                        if (0 < rtCode)
                        {
                            listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                        }
                    }
                    else
                    {   // "D10=0"のパターン
                        PlcDeviceType type;
                        int addr;
                        McProtocolApp.GetDeviceCode(s[0], out type, out addr);

                        int val = int.Parse(s[1]);
                        int rtCode;
                        if (McProtocolApp.IsBitDevice(type))
                        {
                            var data = new int[1];
                            data[0] = val;
                            rtCode = FApp.SetBitDevice(s[0], data.Length, data);
                        }
                        else
                        {
                            rtCode = FApp.SetDevice(s[0], val);
                        }
                        listBox1.Items.Add(buff.ToUpper());
                        if (0 < rtCode)
                        {
                            listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                        }
                    }
                }
                else
                {   // "D10"のパターン
                    PlcDeviceType type;
                    int addr;
                    McProtocolApp.GetDeviceCode(buff.ToUpper(), out type, out addr);

                    int n;
                    int rtCode;
                    if (McProtocolApp.IsBitDevice(type))
                    {
                        var data = new int[1];
                        rtCode = FApp.GetBitDevice(buff, data.Length, data);
                        n = data[0];
                    }
                    else
                    {
                        rtCode = FApp.GetDevice(buff.ToUpper(), out n);
                    }
                    listBox1.Items.Add(buff.ToUpper() + "=" + n.ToString(CultureInfo.InvariantCulture));
                    if (0 < rtCode)
                    {
                        listBox1.Items.Add("ERROR:0x" + rtCode.ToString("X4"));
                    }
                }
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                cb.Items.Insert(0, cb.Text);
                cb.Text = "";
            }
        }
    }
}
