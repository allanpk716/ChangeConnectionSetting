using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;

namespace ChangeConnectionSetting
{
    public class IPDNSHelper
    {
        public struct NetworkCardInfo
        {
            /// <summary>
            /// IP地址
            /// </summary>
            public string IPAdress;
            /// <summary>
            /// 子掩码
            /// </summary>
            public string SubMark;
            /// <summary>
            /// 网管
            /// </summary>
            public string GateWay;
            /// <summary>
            /// 首要 DNS
            /// </summary>
            public string DNS1;
            /// <summary>
            /// 备用 DNS
            /// </summary>
            public string DNS2;
            /// <summary>
            /// 网络类型，有线网卡 or Wifi 等
            /// </summary>
            public NetworkInterfaceType NetType;
            /// <summary>
            /// 名称
            /// </summary>
            public string Name;
            /// <summary>
            /// 描述
            /// </summary>
            public string Description;
        }

        /// <summary>
        /// 获取所有网卡信息
        /// </summary>
        /// <returns></returns>
        public List<NetworkCardInfo> GetIpInfo()
        {
            List<NetworkCardInfo> NetworkCardInfoList = new List<NetworkCardInfo>();

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                NetworkCardInfo ncinfo = new NetworkCardInfo();

                // 网络名称
                ncinfo.Name = adapter.Name;
                // 网络描述
                ncinfo.Description = adapter.Description;
                // 保存网络类型
                ncinfo.NetType = adapter.NetworkInterfaceType;
                // IP配置信息
                IPInterfaceProperties ip = adapter.GetIPProperties(); 
                if (ip.UnicastAddresses.Count > 0)
                {
                    // IP地址
                    ncinfo.IPAdress = ip.UnicastAddresses[0].Address.ToString();
                    // 子网掩码
                    ncinfo.SubMark = ip.UnicastAddresses[0].IPv4Mask.ToString();
                }
                if (ip.GatewayAddresses.Count > 0)
                {
                    // 默认网关
                    ncinfo.GateWay = ip.GatewayAddresses[0].Address.ToString();
                }

                int DnsCount = ip.DnsAddresses.Count;
                if (DnsCount > 0)
                {
                    if (DnsCount == 1)
                    {
                        // 首要 DNS
                        ncinfo.DNS1 = ip.DnsAddresses[0].ToString();
                        // 备用DNS地址
                        ncinfo.DNS2 = string.Empty;
                    }
                    // 理论上最多只有两个，超过也就是两个噻
                    else
                    {
                        // 首要 DNS
                        ncinfo.DNS1 = ip.DnsAddresses[0].ToString();
                        // 备用DNS地址
                        ncinfo.DNS2 = ip.DnsAddresses[1].ToString();
                    }
                }

                NetworkCardInfoList.Add(ncinfo);
            }

            return NetworkCardInfoList;
        }   

        /// <summary>
        /// 设置首选网卡的 IP DNS 信息
        /// </summary>
        /// <param name="networkCardInfo">用 GetIpInfo 获取，然后修改项要改的信息</param>
        /// <param name="bAutoIP"></param>
        /// <param name="bAutoDNS">DNS 想要能自动获取，IP 地址就必须也是自动获取</param>
        /// <returns></returns>
        public bool SetIpInfo(NetworkCardInfo networkCardInfo, bool bAutoIP, bool bAutoDNS)
        {
            string[] ip = new string[] { networkCardInfo.IPAdress.Trim() };
            string[] SubMark = new string[] { networkCardInfo.SubMark.Trim() };
            string[] GateWay = new string[] { networkCardInfo.GateWay.Trim() };
            string[] DNS = new string[] { networkCardInfo.DNS1.Trim(), networkCardInfo.DNS2.Trim() };

            string description = networkCardInfo.Description.Trim();

            try
            {
                ManagementBaseObject inPar = null;
                ManagementBaseObject outPar = null;
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                // 禁用网卡
                string manage = "SELECT * FROM Win32_NetworkAdapter";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(manage);
                ManagementObjectCollection collection = searcher.Get();
                ManagementObject moTmp = null;

                foreach (ManagementObject mo in collection)
                {
                    if (description != mo["Description"].ToString())
                    {
                        continue;
                    }

                    moTmp = mo;
                }

                foreach (ManagementObject mo in moc)
                {
                    #region 调试输出所有的属性
//                     foreach (PropertyData pd in mo.Properties)
//                     {
//                         Trace.WriteLine("Name:" + pd.Name + ("-- Value:") + pd.Value);
//                     }
                    #endregion

                    // 如果网络连接是禁用的，或者是 描述不一致, 则跳过
                    if ((bool)mo["IPEnabled"] == false ||
                        description != mo["Description"].ToString()
                        )
                    {
                        continue;
                    }

                    // 设置为自动获取 IP 地址
                    if ((bool)mo["DHCPEnabled"] == false && bAutoIP == true)
                    {
                        inPar = mo.GetMethodParameters("EnableDHCP");
                        mo.InvokeMethod("EnableDHCP", inPar, null);
                    }

                    // 自动获取 DNS 只有在自动获取 IP 的时候才有效
                    // 设置自动获取 DNS
                    if (bAutoIP == true && bAutoDNS == true)
                    {
                        // 重置DNS为空
                        mo.InvokeMethod("SetDNSServerSearchOrder", null);
                    }

                    // 需要手动设置 IP 地址信息
                    if (bAutoIP == false && ip != null && SubMark != null && GateWay != null)
                    {
                        inPar = mo.GetMethodParameters("EnableStatic");
                        inPar["IPAddress"] = ip;//ip地址  
                        inPar["SubnetMask"] = SubMark; //子网掩码   
                        mo.InvokeMethod("EnableStatic", inPar, null);//执行  

                        inPar = mo.GetMethodParameters("SetGateways");
                        inPar["DefaultIPGateway"] = GateWay; //设置网关地址 1.网关;2.备用网关  
                        outPar = mo.InvokeMethod("SetGateways", inPar, null);//执行  
                    }

                    // 手动设置 DNS 信息
                    if (bAutoDNS == false && DNS != null)
                    {
                        inPar = mo.GetMethodParameters("SetDNSServerSearchOrder");
                        inPar["DNSServerSearchOrder"] = DNS; //设置DNS  1.DNS 2.备用DNS  
                        mo.InvokeMethod("SetDNSServerSearchOrder", inPar, null);// 执行  
                    }

                    if ((bAutoIP == true || bAutoDNS == true) && moTmp != null)
                    {
                        // 先禁用网卡，然后再启用，使得设置生效
                        moTmp.InvokeMethod("Disable", null);

                        moTmp.InvokeMethod("Enable", null);
                    }

                    // 只设置一张匹配到的网卡
                    break;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
                return false;
            }

            return true;
        }
    }
}
