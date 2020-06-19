using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using SchoolBusWXWeb.Business;
using SchoolBusWXWeb.CommServices;
using SchoolBusWXWeb.CommServices.MessageHandlers.CustomMessageHandler;
using SchoolBusWXWeb.Filters;
using SchoolBusWXWeb.Hubs;
using SchoolBusWXWeb.Models;
using SchoolBusWXWeb.Repository;
using SchoolBusWXWeb.Utilities;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.NeuChar.MessageHandlers;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.MessageHandlers.Middleware;
using Senparc.Weixin.RegisterServices;

#if !DEBUG
using System.IO;
using Microsoft.AspNetCore.DataProtection;
#endif

namespace SchoolBusWXWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if !DEBUG
            services.AddDataProtection().SetApplicationName("SchoolBusWeb").PersistKeysToFileSystem(new DirectoryInfo(@"/var/schooldpkeys/"));
#endif
            services.Configure<SiteConfig>(Configuration.GetSection("SiteConfig"));
            services.AddScoped<ISchoolBusBusines, SchoolBusBusines>();
            services.AddScoped<ISchoolBusRepository, SchoolBusRepository>();
            services.AddScoped<MqttHelper>();
            // services.AddStartupTask<MqttStartupFilter>();
            services.AddLoggingFileUI();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMemoryCache(); // ʹ�ñ��ػ���������
            services.AddSession();
            services.AddSignalR();
            services.AddSenparcGlobalServices(Configuration).AddSenparcWeixinServices(Configuration);
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            services.AddRazorPages();
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting, IHostApplicationLifetime appLifetime, MqttHelper mQtt)
        {
            #region ���Ƴ�����������
            // ������Ӧ�������ɹ��Ժ�Ҳ����Startup.Configure()����������
            //appLifetime.ApplicationStarted.Register(async () =>
            //{
            //    await mQtt.ConnectMqttServerAsync();
            //});
            //// �����ڳ���������������˳���ʱ���������󶼱�������ɡ�������ڴ����������Actionί�д����Ժ��˳�
            //appLifetime.ApplicationStopped.Register(async () =>
            //{
            //    MqttHelper.IsReconnect = false;
            //    await MqttHelper.MqttClient.DisconnectAsync();
            //});
            // �����ڳ�������ִ���˳��Ĺ����У���ʱ�����������ڱ�����Ӧ�ó���Ҳ��ȵ�����¼���ɺ����˳���
            //appLifetime.ApplicationStopping.Register(() =>
            //{

            //});
            #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseAuthorization();
            app.UseSession();
            #region ΢�����
            //RegisterService.Start(env, senparcSetting.Value)
            //    .UseSenparcGlobal()  // ���� CO2NET ȫ��ע�ᣬ���룡
            //    .RegisterTraceLog(ConfigTraceLog) //����TraceLog
            //    .UseSenparcWeixin(senparcWeixinSetting.Value, senparcSetting.Value)
            //    .RegisterMpAccount(senparcWeixinSetting.Value, "�����ܲ��ԡ����ں�");

            app.UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
            {
                #region ע����־�����裬���飩
                globalRegister.RegisterTraceLog(ConfigTraceLog);//����TraceLog
                #endregion
            }, true).UseSenparcWeixin(senparcWeixinSetting.Value, weixinRegister =>
            {
                weixinRegister.RegisterMpAccount(senparcWeixinSetting.Value, "�����ܲ��ԡ����ں�");
            });
            app.UseMessageHandlerForMp("/WeixinAsync", CustomMessageHandler.GenerateMessageHandler, options =>
            {
                //�˴�Ϊί�У����Ը���������̬�ж��������������룩
                options.AccountSettingFunc = context => senparcWeixinSetting.Value;
                //�� MessageHandler ���첽����δ�ṩ��дʱ������ͬ�����������裩
                options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;

            });
            #endregion
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/chathub");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

        }
        /// <summary>
        /// ����΢�Ÿ�����־
        /// </summary>
        private void ConfigTraceLog()
        {
            //������ΪDebug״̬ʱ��/App_Data/WeixinTraceLog/Ŀ¼�»�������־�ļ���¼���е�API������־����ʽ�����汾����ر�

            //���ȫ�ֵ�IsDebug��Senparc.CO2NET.Config.IsDebug��Ϊfalse���˴����Ե�������true�������Զ�Ϊtrue
            SenparcTrace.SendCustomLog("ϵͳ��־", "ϵͳ����");//ֻ��Senparc.Weixin.Config.IsDebug = true���������Ч

            //ȫ���Զ�����־��¼�ص�
            SenparcTrace.OnLogFunc = () =>
            {
                //����ÿ�δ���Log����Ҫִ�еĴ���
            };

            //����������WeixinException���쳣ʱ����
            WeixinTrace.OnWeixinExceptionFunc = ex =>
            {
                //����ÿ�δ���WeixinExceptionLog����Ҫִ�еĴ���

                //����ģ����Ϣ������Ա                             -- DPBMARK Redis
                var eventService = new EventService();
                eventService.ConfigOnWeixinExceptionFunc(ex);      // DPBMARK_END
            };
        }
    }
}
