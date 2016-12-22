using System.Collections.Generic;
using CmsData;
using UtilityExtensions;
using System.Text.RegularExpressions;
using CmsData.Registration;
using CmsWeb.Areas.OnlineReg.Models;

namespace CmsWeb.Areas.OnlineReg.Controllers
{
    public partial class OnlineRegController
    {
        private Dictionary<int, Settings> _settings;
        public Dictionary<int, Settings> settings
        {
            get
            {
                if (_settings == null)
                    _settings = HttpContext.Items["RegSettings"] as Dictionary<int, Settings>;
                return _settings;
            }
        }

        public void SetHeaders(OnlineRegModel m2)
        {
            Session["gobackurl"] = m2.URL;
            SetHeaders2(m2.Orgid ?? m2.masterorgid ?? 0);
        }
        private void SetHeaders2(int id)
        {
            var org = DbUtil.Db.LoadOrganizationById(id);
            var shell = "";
            if ((settings == null || !settings.ContainsKey(id)) && org != null)
            {
                var setting = DbUtil.Db.CreateRegistrationSettings(id);
                shell = DbUtil.Db.ContentOfTypeHtml(setting.ShellBs)?.Body;
            }
            if (!shell.HasValue() && settings != null && settings.ContainsKey(id))
                shell = DbUtil.Db.ContentOfTypeHtml(settings[id].ShellBs)?.Body;
            if (!shell.HasValue())
            {
                shell = DbUtil.Db.ContentOfTypeHtml("ShellDefaultBs")?.Body;
                if(!shell.HasValue())
                    shell = DbUtil.Db.ContentOfTypeHtml("DefaultShellBs")?.Body;
            }


            if (shell != null && shell.HasValue())
            {
                var re = new Regex(@"(.*<!--FORM START-->\s*).*(<!--FORM END-->.*)", RegexOptions.Singleline);
                var t = re.Match(shell).Groups[1].Value.Replace("<!--FORM CSS-->", ViewExtensions2.Bootstrap3Css());
                ViewBag.hasshell = true;
                ViewBag.top = t;
                var b = re.Match(shell).Groups[2].Value;
                ViewBag.bottom = b;
            }
            else
                ViewBag.hasshell = false;
        }
        private void SetHeaders(int id)
        {
            Settings setting = null;
            var org = DbUtil.Db.LoadOrganizationById(id);
            if (org != null)
            {
                SetHeaders2(id);
                return;
            }
            var shell = "";
            if ((settings == null || !settings.ContainsKey(id)))
            {
                setting = DbUtil.Db.CreateRegistrationSettings(id);
                shell = DbUtil.Content(setting.Shell, null);
            }
            if (!shell.HasValue() && settings != null && settings.ContainsKey(id))
            {
                shell = DbUtil.Content(settings[id].Shell, null);
            }
            if (!shell.HasValue())
                shell = DbUtil.Content("ShellDiv-" + id, DbUtil.Content("ShellDefault", ""));

            var s = shell;
            if (s.HasValue())
            {
                var re = new Regex(@"(.*<!--FORM START-->\s*).*(<!--FORM END-->.*)", RegexOptions.Singleline);
                var t = re.Match(s).Groups[1].Value.Replace("<!--FORM CSS-->", 
                ViewExtensions2.jQueryUICss() +
                "\r\n<link href=\"/Content/styles/onlinereg.css?v=8\" rel=\"stylesheet\" type=\"text/css\" />\r\n"); 
                ViewBag.hasshell = true;
                ViewBag.top = t;
                var b = re.Match(s).Groups[2].Value;
                ViewBag.bottom = b;
            }
            else
            {
                ViewBag.hasshell = false;
                ViewBag.header = DbUtil.Content("OnlineRegHeader-" + id,
                    DbUtil.Content("OnlineRegHeader", ""));
                ViewBag.top = DbUtil.Content("OnlineRegTop-" + id,
                    DbUtil.Content("OnlineRegTop", ""));
                ViewBag.bottom = DbUtil.Content("OnlineRegBottom-" + id,
                    DbUtil.Content("OnlineRegBottom", ""));
            }
        }
        
    }
}