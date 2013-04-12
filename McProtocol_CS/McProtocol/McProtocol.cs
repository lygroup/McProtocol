using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace Nc
{
    namespace Mitsubishi
    {
        // $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        public enum McFrame
        {
              MC3E
            , MC4E
        }

        // $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        // PLCデバイスの種類を定義した列挙体
        public enum PlcDeviceType
        {
            // PLC用デバイス
            M  = 0x90
          , SM = 0x91
          , L  = 0x92
          , F  = 0x93
          , V  = 0x94
          , S  = 0x98
          , X  = 0x9C
          , Y  = 0x9D
          , B  = 0xA0
          , SB = 0xA1
          , DX = 0xA2
          , DY = 0xA3
          , D  = 0xA8
          , SD = 0xA9
          , R  = 0xAF
          , ZR = 0xB0
          , W  = 0xB4
          , SW = 0xB5
          , TC = 0xC0
          , TS = 0xC1
          , TN = 0xC2
          , CC = 0xC3
          , CS = 0xC4
          , CN = 0xC5
          , SC = 0xC6
          , SS = 0xC7
          , SN = 0xC8
          , Z  = 0xCC
          , TT
          , TM
          , CT
          , CM
          , A
          , Max
        }

        // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // PLCと接続するための共通のインターフェースを定義する
        public interface Plc : IDisposable 
        {
            int Open();
            int Close();
            int SetBitDevice(string iDeviceName, int iSize, int[] iData);
            int SetBitDevice(PlcDeviceType iType, int iAddress, int iSize, int[] iData);
            int GetBitDevice(string iDeviceName, int iSize, int[] oData);
            int GetBitDevice(PlcDeviceType iType, int iAddress, int iSize, int[] oData);
            int WriteDeviceBlock(string iDeviceName, int iSize, int[] iData);
            int WriteDeviceBlock(PlcDeviceType iType, int iAddress, int iSize, int[] iData);
            int ReadDeviceBlock(string iDeviceName, int iSize, int[] oData);
            int ReadDeviceBlock(PlcDeviceType iType, int iAddress, int iSize, int[] oData);
            int SetDevice(string iDeviceName, int iData);
            int SetDevice(PlcDeviceType iType, int iAddress, int iData);
            int GetDevice(string iDeviceName, out int oData);
            int GetDevice(PlcDeviceType iType, int iAddress, out int oData);
        }
        // ########################################################################################
        abstract public class McProtocolApp : Plc
        {
            // ====================================================================================
            public McFrame CommandFrame { get; set; }   // 使用フレーム
            public string HostName { get; set; }   // ホスト名またはIPアドレス
            public int PortNumber { get; set; }    // ポート番号
            // ====================================================================================
            // コンストラクタ
            protected McProtocolApp(string iHostName, int iPortNumber)
            {
                CommandFrame = McFrame.MC3E;
                HostName = iHostName;
                PortNumber = iPortNumber;
            }

            // ====================================================================================
            // 後処理
            public void Dispose()
            {
                Close();
            }

            // ====================================================================================
            public int Open()
            {
                DoConnect();
                Command = new McCommand(CommandFrame);
                return 0;
            }
            // ====================================================================================
            public int Close()
            {
                DoDisconnect();
                return 0;
            }
            // ====================================================================================
            public int SetBitDevice(string iDeviceName, int iSize, int[] iData)
            {
                PlcDeviceType type;
                int addr;
                GetDeviceCode(iDeviceName, out type, out addr);
                return SetBitDevice(type, addr, iSize, iData);
            }
            // ====================================================================================
            public int SetBitDevice(PlcDeviceType iType, int iAddress, int iSize, int[] iData)
            {
                var type = iType;
                var addr = iAddress;
                var data = new List<byte>(6)
                    {
                        (byte) addr
                      , (byte) (addr >> 8)
                      , (byte) (addr >> 16)
                      , (byte) type
                      , (byte) iSize
                      , (byte) (iSize >> 8)
                    };
                var d = (byte)iData[0];
                var i = 0;
                while (i < iData.Length)
                {
                    if (i % 2 == 0)
                    {
                        d = (byte)iData[i];
                        d <<= 4;
                    }
                    else
                    {
                        d |= (byte)(iData[i] & 0x01);
                        data.Add(d);
                    }
                    ++i;
                }
                if (i % 2 != 0)
                {
                    data.Add(d);
                }
                byte[] sdCommand = Command.SetCommand(0x1401, 0x0001, data.ToArray());
                byte[] rtResponse = TryExecution(sdCommand);
                int rtCode = Command.SetResponse(rtResponse);
                return rtCode;
            }
            // ====================================================================================
            public int GetBitDevice(string iDeviceName, int iSize, int[] oData)
            {
                PlcDeviceType type;
                int addr;
                GetDeviceCode(iDeviceName, out type, out addr);
                return GetBitDevice(type, addr, iSize, oData);
            }
            // ====================================================================================
            public int GetBitDevice(PlcDeviceType iType, int iAddress, int iSize, int[] oData)
            {
                PlcDeviceType type = iType;
                int addr = iAddress;
                var data = new List<byte>(6)
                    {
                        (byte) addr
                      , (byte) (addr >> 8)
                      , (byte) (addr >> 16)
                      , (byte) type
                      , (byte) iSize
                      , (byte) (iSize >> 8)
                    };
                byte[] sdCommand = Command.SetCommand(0x0401, 0x0001, data.ToArray());
                byte[] rtResponse = TryExecution(sdCommand);
                int rtCode = Command.SetResponse(rtResponse);
                byte[] rtData = Command.Response;
                for (int i = 0; i < iSize; ++i)
                {
                    if (i % 2 == 0)
                    {
                        oData[i] = (rtCode == 0) ? ((rtData[i / 2] >> 4) & 0x01) : 0;
                    }
                    else
                    {
                        oData[i] = (rtCode == 0) ? (rtData[i / 2] & 0x01) : 0;
                    }
                }
                return rtCode;
            }
            // ====================================================================================
            public int WriteDeviceBlock(string iDeviceName, int iSize, int[] iData)
            {
                PlcDeviceType type;
                int addr;
                GetDeviceCode(iDeviceName, out type, out addr);
                return WriteDeviceBlock(type, addr, iSize, iData);
            }
            // ====================================================================================
            public int WriteDeviceBlock(PlcDeviceType iType, int iAddress, int iSize, int[] iData)
            {
                PlcDeviceType type = iType;
                int addr = iAddress;
                var data = new List<byte>(6)
                    {
                        (byte) addr
                      , (byte) (addr >> 8)
                      , (byte) (addr >> 16)
                      , (byte) type
                      , (byte) iSize
                      , (byte) (iSize >> 8)
                    };
                foreach (int t in iData)
                {
                    data.Add((byte)t);
                    data.Add((byte)(t >> 8));
                }
                byte[] sdCommand = Command.SetCommand(0x1401, 0x0000, data.ToArray());
                byte[] rtResponse = TryExecution(sdCommand);
                int rtCode = Command.SetResponse(rtResponse);
                return rtCode;
            }
            // ====================================================================================
            public int ReadDeviceBlock(string iDeviceName, int iSize, int[] oData)
            {
                PlcDeviceType type;
                int addr;
                GetDeviceCode(iDeviceName, out type, out addr);
                return ReadDeviceBlock(type, addr, iSize, oData);
            }
            // ====================================================================================
            public int ReadDeviceBlock(PlcDeviceType iType, int iAddress, int iSize, int[] oData)
            {
                PlcDeviceType type = iType;
                int addr = iAddress;
                var data = new List<byte>(6)
                    {
                        (byte) addr
                      , (byte) (addr >> 8)
                      , (byte) (addr >> 16)
                      , (byte) type
                      , (byte) iSize
                      , (byte) (iSize >> 8)
                    };
                byte[] sdCommand = Command.SetCommand(0x0401, 0x0000, data.ToArray());
                byte[] rtResponse = TryExecution(sdCommand);
                int rtCode = Command.SetResponse(rtResponse);
                byte[] rtData = Command.Response;
                for (int i = 0; i < iSize; ++i)
                {
                    oData[i] = (rtCode == 0) ? BitConverter.ToInt16(rtData, i * 2) : 0;
                }
                return rtCode;
            }
            // ====================================================================================
            public int SetDevice(string iDeviceName, int iData)
            {
                PlcDeviceType type;
                int addr;
                GetDeviceCode(iDeviceName, out type, out addr);
                return SetDevice(type, addr, iData);
            }
            // ====================================================================================
            public int SetDevice(PlcDeviceType iType, int iAddress, int iData)
            {
                PlcDeviceType type = iType;
                int addr = iAddress;
                var data = new List<byte>(6)
                    {
                        (byte) addr
                      , (byte) (addr >> 8)
                      , (byte) (addr >> 16)
                      , (byte) type
                      , 0x01
                      , 0x00
                      , (byte) iData
                      , (byte) (iData >> 8)
                    };
                byte[] sdCommand = Command.SetCommand(0x1401, 0x0000, data.ToArray());
                byte[] rtResponse = TryExecution(sdCommand);
                int rtCode = Command.SetResponse(rtResponse);
                return rtCode;
            }
            // ====================================================================================
            public int GetDevice(string iDeviceName, out int oData)
            {
                PlcDeviceType type;
                int addr;
                GetDeviceCode(iDeviceName, out type, out addr);
                return GetDevice(type, addr, out oData);
            }
            // ====================================================================================
            public int GetDevice(PlcDeviceType iType, int iAddress, out int oData)
            {
                PlcDeviceType type = iType;
                int addr = iAddress;
                var data = new List<byte>(6)
                    {
                        (byte) addr
                      , (byte) (addr >> 8)
                      , (byte) (addr >> 16)
                      , (byte) type
                      , 0x01
                      , 0x00
                    };
                byte[] sdCommand = Command.SetCommand(0x0401, 0x0000, data.ToArray());
                byte[] rtResponse = TryExecution(sdCommand);
                int rtCode = Command.SetResponse(rtResponse);
                if (0 < rtCode)
                {
                    oData = 0;
                }
                else
                {
                    byte[] rtData = Command.Response;
                    oData = BitConverter.ToInt16(rtData, 0);
                }
                return rtCode;
            }
            // ====================================================================================
            //public int GetCpuType(out string oCpuName, out int oCpuType)
            //{
            //    int rtCode = Command.Execute(0x0101, 0x0000, new byte[0]);
            //    oCpuName = "dummy";
            //    oCpuType = 0;
            //    return rtCode;
            //}
            // ====================================================================================
            public static PlcDeviceType GetDeviceType(string s)
            {
                return (s == "M") ? PlcDeviceType.M :
                       (s == "SM") ? PlcDeviceType.SM :
                       (s == "L") ? PlcDeviceType.L :
                       (s == "F") ? PlcDeviceType.F :
                       (s == "V") ? PlcDeviceType.V :
                       (s == "S") ? PlcDeviceType.S :
                       (s == "X") ? PlcDeviceType.X :
                       (s == "Y") ? PlcDeviceType.Y :
                       (s == "B") ? PlcDeviceType.B :
                       (s == "SB") ? PlcDeviceType.SB :
                       (s == "DX") ? PlcDeviceType.DX :
                       (s == "DY") ? PlcDeviceType.DY :
                       (s == "D") ? PlcDeviceType.D :
                       (s == "SD") ? PlcDeviceType.SD :
                       (s == "R") ? PlcDeviceType.R :
                       (s == "ZR") ? PlcDeviceType.ZR :
                       (s == "W") ? PlcDeviceType.W :
                       (s == "SW") ? PlcDeviceType.SW :
                       (s == "TC") ? PlcDeviceType.TC :
                       (s == "TS") ? PlcDeviceType.TS :
                       (s == "TN") ? PlcDeviceType.TN :
                       (s == "CC") ? PlcDeviceType.CC :
                       (s == "CS") ? PlcDeviceType.CS :
                       (s == "CN") ? PlcDeviceType.CN :
                       (s == "SC") ? PlcDeviceType.SC :
                       (s == "SS") ? PlcDeviceType.SS :
                       (s == "SN") ? PlcDeviceType.SN :
                       (s == "Z") ? PlcDeviceType.Z :
                       (s == "TT") ? PlcDeviceType.TT :
                       (s == "TM") ? PlcDeviceType.TM :
                       (s == "CT") ? PlcDeviceType.CT :
                       (s == "CM") ? PlcDeviceType.CM :
                       (s == "A") ? PlcDeviceType.A :
                                     PlcDeviceType.Max;
            }

            // ====================================================================================
            public static bool IsBitDevice(PlcDeviceType type)
            {
                return !((type == PlcDeviceType.D)
                      || (type == PlcDeviceType.SD)
                      || (type == PlcDeviceType.Z)
                      || (type == PlcDeviceType.ZR)
                      || (type == PlcDeviceType.R)
                      || (type == PlcDeviceType.W));
            }

            // ====================================================================================
            public static bool IsHexDevice(PlcDeviceType type)
            {
                return (type == PlcDeviceType.X)
                    || (type == PlcDeviceType.Y)
                    || (type == PlcDeviceType.B)
                    || (type == PlcDeviceType.W);
            }

            // ====================================================================================
            public static void GetDeviceCode(string iDeviceName, out PlcDeviceType oType, out int oAddress)
            {
                string s = iDeviceName.ToUpper();
                string strAddress;

                // 1文字取り出す
                string strType = s.Substring(0, 1);
                switch (strType)
                {
                    case "A":
                    case "B":
                    case "D":
                    case "F":
                    case "L":
                    case "M":
                    case "R":
                    case "V":
                    case "W":
                    case "X":
                    case "Y":
                        // 2文字目以降は数値のはずなので変換する
                        strAddress = s.Substring(1);
                        break;
                    case "Z":
                        // もう1文字取り出す
                        strType = s.Substring(0, 2);
                        // ファイルレジスタの場合     : 2
                        // インデックスレジスタの場合 : 1
                        strAddress = s.Substring(strType.Equals("ZR") ? 2 : 1);
                        break;
                    case "C":
                        // もう1文字取り出す
                        strType = s.Substring(0, 2);
                        switch (strType)
                        {
                            case "CC":
                            case "CM":
                            case "CN":
                            case "CS":
                            case "CT":
                                strAddress = s.Substring(2);
                                break;
                            default:
                                throw new Exception("Invalid format.");
                        }
                        break;
                    case "S":
                        // もう1文字取り出す
                        strType = s.Substring(0, 2);
                        switch (strType)
                        {
                            case "SD":
                            case "SM":
                                strAddress = s.Substring(2);
                                break;
                            default:
                                throw new Exception("Invalid format.");
                        }
                        break;
                    case "T":
                        // もう1文字取り出す
                        strType = s.Substring(0, 2);
                        switch (strType)
                        {
                            case "TC":
                            case "TM":
                            case "TN":
                            case "TS":
                            case "TT":
                                strAddress = s.Substring(2);
                                break;
                            default:
                                throw new Exception("Invalid format.");
                        }
                        break;
                    default:
                        throw new Exception("Invalid format.");
                }

                oType = GetDeviceType(strType);
                oAddress = IsHexDevice(oType) ? Convert.ToInt32(strAddress, BlockSize) :
                                                Convert.ToInt32(strAddress);
            }
            // &&&&& protected &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            abstract protected void DoConnect();
            abstract protected void DoDisconnect();
            abstract protected byte[] Execute(byte[] iCommand);
            // &&&&& private &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            private const int BlockSize = 0x0010;
            private McCommand Command { get; set; }
            // ================================================================================
            private byte[] TryExecution(byte[] iCommand)
            {
                byte[] rtResponse;
                int tCount = 10;
                do
                {
                    rtResponse = Execute(iCommand);
                    --tCount;
                    if (tCount < 0)
                    {
                        throw new Exception("PLCから正しい値が取得できません.");
                    }
                } while (Command.IsIncorrectResponse(rtResponse));
                return rtResponse;
            }
            // ####################################################################################
            // 通信に使用するコマンドを表現するインナークラス
            class McCommand
            {
                private McFrame FrameType { get; set; }  // フレーム種別
                private uint SerialNumber { get; set; }  // シリアル番号
                private uint NetwrokNumber { get; set; } // ネットワーク番号
                private uint PcNumber { get; set; }      // PC番号
                private uint IoNumber { get; set; }      // 要求先ユニットI/O番号
                private uint ChannelNumber { get; set; } // 要求先ユニット局番号
                private uint CpuTimer { get; set; }      // CPU監視タイマ
                private int ResultCode { get; set; }     // 終了コード
                public byte[] Response { get; private set; }    // 応答データ
                // ================================================================================
                // コンストラクタ
                public McCommand(McFrame iFrame)
                {
                    FrameType = iFrame;
                    SerialNumber = 0x0001u;
                    NetwrokNumber = 0x0000u;
                    PcNumber = 0x00FFu;
                    IoNumber = 0x03FFu;
                    ChannelNumber = 0x0000u;
                    CpuTimer = 0x0010u;
                }
                // ================================================================================
                public byte[] SetCommand(uint iMainCommand, uint iSubCommand, byte[] iData)
                {
                    var dataLength = (uint)(iData.Length + 6);
                    var ret = new List<byte>(iData.Length + 20);
                    uint frame = (FrameType == McFrame.MC3E) ? 0x0050u :
                                 (FrameType == McFrame.MC4E) ? 0x0054u : 0x0000u;
                    ret.Add((byte)frame);
                    ret.Add((byte)(frame >> 8));
                    if (FrameType == McFrame.MC4E)
                    {
                        ret.Add((byte)SerialNumber);
                        ret.Add((byte)(SerialNumber >> 8));
                        ret.Add(0x00);
                        ret.Add(0x00);
                    }
                    ret.Add((byte)NetwrokNumber);
                    ret.Add((byte)PcNumber);
                    ret.Add((byte)IoNumber);
                    ret.Add((byte)(IoNumber >> 8));
                    ret.Add((byte)ChannelNumber);
                    ret.Add((byte)dataLength);
                    ret.Add((byte)(dataLength >> 8));
                    ret.Add((byte)CpuTimer);
                    ret.Add((byte)(CpuTimer >> 8));
                    ret.Add((byte)iMainCommand);
                    ret.Add((byte)(iMainCommand >> 8));
                    ret.Add((byte)iSubCommand);
                    ret.Add((byte)(iSubCommand >> 8));
                    ret.AddRange(iData);
                    return ret.ToArray();
                }
                // ================================================================================
                public int SetResponse(byte[] iResponse)
                {
                    int min = (FrameType == McFrame.MC3E) ? 11 : 15;
                    if (min <= iResponse.Length)
                    {
                        var btCount = new[] { iResponse[min - 4], iResponse[min - 3] };
                        var btCode = new[] { iResponse[min - 2], iResponse[min - 1] };
                        int rsCount = BitConverter.ToUInt16(btCount, 0);
                        ResultCode = BitConverter.ToUInt16(btCode, 0);
                        Response = new byte[rsCount - 2];
                        Buffer.BlockCopy(iResponse, min, Response, 0, Response.Length);
                    }
                    return ResultCode;
                }
                // ================================================================================
                public bool IsIncorrectResponse(byte[] iResponse)
                {
                    var min = (FrameType == McFrame.MC3E) ? 11 : 15;
                    var btCount = new[] { iResponse[min - 4], iResponse[min - 3] };
                    var btCode  = new[] { iResponse[min - 2], iResponse[min - 1] };
                    var rsCount = BitConverter.ToUInt16(btCount, 0) - 2;
                    var rsCode  = BitConverter.ToUInt16(btCode, 0);
                    return (rsCode == 0 && rsCount != (iResponse.Length - min));
                }
            }
        }

        // ########################################################################################
        public class McProtocolTcp : McProtocolApp
        {
            // ====================================================================================
            // コンストラクタ
            public McProtocolTcp() : this("", 0) { }
            public McProtocolTcp(string iHostName, int iPortNumber)
                : base(iHostName, iPortNumber)
            {
                Client = new TcpClient();
            }

            // &&&&& protected &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            override protected void DoConnect()
            {
                TcpClient c = Client;
                if (!c.Connected)
                {
                    // Keep Alive機能の実装
                    var ka = new List<byte>(sizeof(uint) * 3);
                    ka.AddRange(BitConverter.GetBytes(1u));
                    ka.AddRange(BitConverter.GetBytes(45000u));
                    ka.AddRange(BitConverter.GetBytes(5000u));
                    c.Client.IOControl(IOControlCode.KeepAliveValues, ka.ToArray(), null);
                    c.Connect(HostName, PortNumber);
                    Stream = c.GetStream();
                }
            }
            // ====================================================================================
            override protected void DoDisconnect()
            {
                TcpClient c = Client;
                if (c.Connected)
                {
                    c.Close();
                }
            }
            // ================================================================================
            override protected byte[] Execute(byte[] iCommand)
            {
                NetworkStream ns = Stream;
                ns.Write(iCommand, 0, iCommand.Length);
                ns.Flush();

                using (var ms = new MemoryStream())
                {
                    var buff = new byte[256];
                    do
                    {
                        int sz = ns.Read(buff, 0, buff.Length);
                        if (sz == 0)
                        {
                            throw new Exception("切断されました");
                        }
                        ms.Write(buff, 0, sz);
                    } while (ns.DataAvailable);
                    return ms.ToArray();
                }
            }
            // &&&&& private &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            private TcpClient Client { get; set; }
            private NetworkStream Stream { get; set; }
        }
        // ########################################################################################
        public class McProtocolUdp : McProtocolApp
        {
            // ====================================================================================
            // コンストラクタ
            public McProtocolUdp(int iPortNumber) : this("", iPortNumber) { }
            public McProtocolUdp(string iHostName, int iPortNumber)
                : base(iHostName, iPortNumber)
            {
                Client = new UdpClient(iPortNumber);
            }

            // &&&&& protected &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            override protected void DoConnect()
            {
                UdpClient c = Client;
                c.Connect(HostName, PortNumber);
            }
            // ====================================================================================
            override protected void DoDisconnect()
            {
                // UDPでは何もしない
            }
            // ================================================================================
            override protected byte[] Execute(byte[] iCommand)
            {
                UdpClient c = Client;
                // 送信
                c.Send(iCommand, iCommand.Length);

                using (var ms = new MemoryStream())
                {
                    IPAddress ip = IPAddress.Parse(HostName);
                    var ep = new IPEndPoint(ip, PortNumber);
                    do
                    {
                        // 受信
                        byte[] buff = c.Receive(ref ep);
                        ms.Write(buff, 0, buff.Length);
                    } while (0 < c.Available);
                    return ms.ToArray();
                }
            }
            // &&&&& private &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            private UdpClient Client { get; set; }
        }
    }   // namespace Mitsubishi
}   // namespace Nc
