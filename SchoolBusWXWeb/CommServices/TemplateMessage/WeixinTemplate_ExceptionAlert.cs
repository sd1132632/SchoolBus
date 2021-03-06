﻿using Senparc.Weixin.Entities.TemplateMessage;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using System;

namespace SchoolBusWXWeb.CommServices.TemplateMessage
{
    public class WeixinTemplate_ExceptionAlert : TemplateMessageBase
    {
        const string TEMPLATE_ID = "casFbgmmdJcNnZJA3YcpRlCecBG2Whp70AvMsqmCLDY";//每个公众号都不同，需要根据实际情况修改

        public TemplateDataItem first { get; set; }
        /// <summary>
        /// Time
        /// </summary>
        public TemplateDataItem keyword1 { get; set; }
        /// <summary>
        /// Host
        /// </summary>
        public TemplateDataItem keyword2 { get; set; }
        /// <summary>
        /// Service
        /// </summary>
        public TemplateDataItem keyword3 { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        public TemplateDataItem keyword4 { get; set; }
        /// <summary>
        /// Message
        /// </summary>
        public TemplateDataItem keyword5 { get; set; }

        public TemplateDataItem remark { get; set; }

        public WeixinTemplate_ExceptionAlert(string _first, string host, string service, string status, string message,
            string _remark, string url = null, string templateId = TEMPLATE_ID)
            : base(templateId, url, "系统异常告警通知")
        {
            first = new TemplateDataItem(_first);
            keyword1 = new TemplateDataItem(SystemTime.Now.LocalDateTime.ToString());
            keyword2 = new TemplateDataItem(host);
            keyword3 = new TemplateDataItem(service);
            keyword4 = new TemplateDataItem(status);
            keyword5 = new TemplateDataItem(message);
            remark = new TemplateDataItem(_remark);
        }
    }
}
