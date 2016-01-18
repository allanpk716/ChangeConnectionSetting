using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeConnectionSetting
{
    [Serializable]
    public class ChangeConnectionSetting
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 触发关键词
        /// </summary>
        public string KeyWord { get; set; }

        /// <summary>
        /// IP地址 V4
        /// </summary>
        public string IPAdress_V4 { get; set; }

        /// <summary>
        /// 子网掩码 V4
        /// </summary>
        public string SubNetMask_V4 { get; set; }

        /// <summary>
        /// 默认网关 V4
        /// </summary>
        public string DefaultGateway_V4 { get; set; }

        /// <summary>
        /// 默认 DNS V4
        /// </summary>
        public string DNS_V4_Default { get; set; }

        /// <summary>
        /// 备用 DNS V4
        /// </summary>
        public string DNS_V4_BackUp { get; set; }
    }
}
