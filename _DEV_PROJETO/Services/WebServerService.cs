using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LetreiroDigital.ViewModels;

namespace LetreiroDigital.Services
{
    public class WebServerService
    {
        private HttpListener? _listener;
        private readonly AppViewModel _viewModel;
        private bool _isRunning;
        private string _baseFolder;
        private string? _password;

        public string? Password => _password;

        public WebServerService(AppViewModel viewModel)
        {
            _viewModel = viewModel;
            // Define folder for static files (will be created on startup if needed)
            _baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        }

        public string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "localhost";
        }

        public void Start(string? password = null, int port = 8080)
        {
            if (_isRunning) return;

            // Use provided password (or null if none)
            _password = password;

            try
            {
                // Request Firewall Access (Async to not block if user cancels or takes time)
                ConfigureFirewall(port);

                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://*:{port}/");
                _listener.Start();
                _isRunning = true;

                Task.Run(() => ListenLoop());
                
                // Ensure wwwroot exists
                EnsureStaticFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar servidor web: {ex.Message}\nTente executar como Administrador ou mudar a porta.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureFirewall(int port)
        {
            try
            {
                Task.Run(() =>
                {
                    // Command to delete old rule + add new rule + reserve URL
                    // Note: sddl="D:(A;;GX;;;WD)" = Allow Generic Execute to Everyone (World)
                    string cmd = $"/c netsh advfirewall firewall delete rule name=\"LetreiroRemote\" & " +
                                 $"netsh advfirewall firewall add rule name=\"LetreiroRemote\" dir=in action=allow protocol=TCP localport={port} profile=any & " +
                                 $"netsh http add urlacl url=http://*:{port}/ sddl=\"D:(A;;GX;;;WD)\"";

                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = cmd,
                        Verb = "runas", // Triggers UAC prompt
                        UseShellExecute = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };
                    System.Diagnostics.Process.Start(psi);
                });
            }
            catch { }
        }

        public void Stop()
        {
            if (!_isRunning || _listener == null) return;
            _isRunning = false;
            try 
            {
                _listener.Stop();
                _listener.Close();
            }
            catch { }
        }

        private async void ListenLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebServer Error: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            string? rawUrl = context.Request.RawUrl;
            
            if (string.IsNullOrEmpty(rawUrl)) return;

            // Security Check
            if (!CheckAuth(context)) return;

            // API Endpoints
            if (rawUrl.StartsWith("/api/"))
            {
                HandleApi(context);
                return;
            }

            // Static Files
            ServeStaticFile(context);
        }

        private void HandleApi(HttpListenerContext context)
        {
            var response = context.Response;
            if (response == null) return;

            if (context.Request.Url == null)
            {
                response.StatusCode = 400;
                response.Close();
                return;
            }

            string path = context.Request.Url.AbsolutePath.ToLower();
            string method = context.Request.HttpMethod;

            try
            {
                if (path == "/api/status" && method == "GET")
                {
                    // Serialize schedule manually
                    var scheduleList = new System.Collections.Generic.List<object>();
                    var currentSchedule = _viewModel.CurrentSchedule;
                    for (int i = 0; i < currentSchedule.Count; i++)
                    {
                        scheduleList.Add(new { 
                            index = i, 
                            time = currentSchedule[i].Time, 
                            content = currentSchedule[i].Content,
                            duration = currentSchedule[i].Duration,
                            lead = currentSchedule[i].Lead,
                            active = i == _viewModel.CurrentItemIndex
                        });
                    }

                    // Serialize schedule tabs
                    var tabsList = new System.Collections.Generic.List<object>();
                    foreach (var tab in _viewModel.ScheduleTabs)
                    {
                        var tabSchedule = _viewModel.WeeklySchedule.ContainsKey(tab) ? _viewModel.WeeklySchedule[tab] : new System.Collections.Generic.List<LetreiroDigital.Models.ScheduleItem>();
                        tabsList.Add(new { name = tab, itemCount = tabSchedule.Count });
                    }

                    var status = new
                    {
                        currentText = _viewModel.DisplayText,
                        timerRunning = _viewModel.TimerRunning,
                        timer = _viewModel.TimerFormatted,
                        timerProgress = _viewModel.TimerProgress,
                        currentItemIndex = _viewModel.CurrentItemIndex,
                        currentDay = _viewModel.CurrentDay,
                        schedule = scheduleList,
                        scheduleTabs = tabsList,
                        // Visibility states
                        showBanner = _viewModel.ShowBanner,
                        showClock = _viewModel.ShowClock,
                        showTimer = _viewModel.ShowTimer,
                        showSchedule = _viewModel.ShowSchedule,
                        // Warning settings
                        enableWarning = _viewModel.EnableWarning,
                        warningSeconds = _viewModel.WarningSeconds,
                        warningActive = _viewModel.WarningActive,
                        // TV Mode Settings
                        tvEventSize = _viewModel.TvEventSize,
                        tvListSize = _viewModel.TvListSize,
                        tvTimerSize = _viewModel.TvTimerSize,
                        tvClockSize = _viewModel.TvClockSize,
                        tvFooterSize = _viewModel.TvFooterSize
                    };
                    SendJson(response, status);
                }
                else if (path == "/api/command" && method == "POST")
                {
                    using var reader = new StreamReader(context.Request.InputStream);
                    string body = reader.ReadToEnd();

                    // Bug #7 fix: usa ExtractJsonValue("action") em vez de body.Contains()
                    // Evita falsos positivos: ex. "preview" ativaria "prev", "stop" ativaria palavras com "stop"
                    string action = ExtractJsonValue(body, "action");

                    if (action == "next") _viewModel.AdvanceToNextItem();
                    else if (action == "prev") _viewModel.PrevItem();
                    else if (action == "playpause") _viewModel.ToggleTimerPause();
                    else if (action == "stop") _viewModel.EmergencyStop();
                    else if (action == "toggle_banner") _viewModel.ToggleBanner();
                    else if (action == "select_item")
                    {
                         try {
                             string valStr = ExtractJsonValue(body, "value");
                             if (int.TryParse(valStr, out int index))
                             {
                                 var sched = _viewModel.CurrentSchedule;
                                 if (index >= 0 && index < sched.Count)
                                     _viewModel.SelectItem(sched[index], index);
                             }
                         } catch {}
                    }
                    else if (action == "set_tv_size")
                    {
                        // Expected: { "action": "set_tv_size", "target": "TvEventSize", "value": 50 }
                        try {
                            string target = ExtractJsonValue(body, "target");
                            string valStr = ExtractJsonValue(body, "value");
                            
                            if (int.TryParse(valStr, out int val))
                            {
                                Application.Current.Dispatcher.Invoke(() => {
                                    switch(target)
                                    {
                                        case "TvEventSize": _viewModel.TvEventSize = val; break;
                                        case "TvListSize": _viewModel.TvListSize = val; break;
                                        case "TvTimerSize": _viewModel.TvTimerSize = val; break;
                                        case "TvClockSize": _viewModel.TvClockSize = val; break;
                                        case "TvFooterSize": _viewModel.TvFooterSize = val; break;
                                    }
                                });
                            }
                        } catch {}
                    }
                    else if (action == "change_day")
                    {
                        try {
                            string dayName = ExtractJsonValue(body, "value");
                            if (!string.IsNullOrEmpty(dayName))
                                Application.Current.Dispatcher.Invoke(() => _viewModel.ChangeDay(dayName));
                        } catch {}
                    }
                    else if (action == "set_warning")
                    {
                        try {
                            string enableStr = ExtractJsonValue(body, "enable");
                            string secondsStr = ExtractJsonValue(body, "seconds");
                            Application.Current.Dispatcher.Invoke(() => {
                                if (!string.IsNullOrEmpty(enableStr))
                                    _viewModel.EnableWarning = enableStr == "true";
                                if (int.TryParse(secondsStr, out int secs) && secs > 0)
                                    _viewModel.WarningSeconds = secs;
                            });
                        } catch {}
                    }
                    else if (action == "toggle_clock")
                        Application.Current.Dispatcher.Invoke(() => _viewModel.ToggleClock());
                    else if (action == "toggle_timer")
                        Application.Current.Dispatcher.Invoke(() => _viewModel.ToggleTimer());
                    else if (action == "toggle_schedule")
                    {
                        Application.Current.Dispatcher.Invoke(() => _viewModel.ShowSchedule = !_viewModel.ShowSchedule);
                    }
                    else if (action == "add_time")
                    {
                        Application.Current.Dispatcher.Invoke(() => _viewModel.TimerSvc.AddSeconds(60));
                    }
                    else if (action == "sub_time")
                    {
                        Application.Current.Dispatcher.Invoke(() => _viewModel.TimerSvc.AddSeconds(-60));
                    }
                    else if (action == "reset_timer")
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            if (_viewModel.CurrentItem != null)
                            {
                                var secs = DataService.ParseDuration(_viewModel.CurrentItem.Duration);
                                if (secs.HasValue) _viewModel.TimerSvc.Start(secs.Value);
                            }
                        });
                    }
                    
                    SendJson(response, new { success = true });
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                byte[] err = Encoding.UTF8.GetBytes(ex.Message);
                response.OutputStream.Write(err, 0, err.Length);
                response.Close();
            }
        }

        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                string keyToken = $"\"{key}\"";
                int idxKey = json.IndexOf(keyToken);
                if (idxKey == -1) return "";
                
                int idxValStart = json.IndexOf(":", idxKey) + 1;
                int idxValEnd = json.IndexOf(",", idxValStart);
                if (idxValEnd == -1) idxValEnd = json.IndexOf("}", idxValStart); // Last item
                
                if (idxValStart > 0 && idxValEnd > idxValStart)
                {
                    return json.Substring(idxValStart, idxValEnd - idxValStart)
                               .Replace("\"", "").Trim();
                }
            }
            catch {}
            return "";
        }

        private void SendJson(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json";
            response.AddHeader("Access-Control-Allow-Origin", "*");
            
            var sb = new StringBuilder();
            sb.Append("{");
            foreach (var prop in data.GetType().GetProperties())
            {
                var val = prop.GetValue(data);
                sb.Append($"\"{prop.Name}\":");
                
                if (val is System.Collections.IEnumerable list && !(val is string))
                {
                    sb.Append("[");
                    foreach (var item in list)
                    {
                        sb.Append("{");
                        foreach (var subProp in item.GetType().GetProperties())
                        {
                            var subVal = subProp.GetValue(item);
                            sb.Append($"\"{subProp.Name}\":");
                            if (subVal is string || subVal == null) sb.Append($"\"{subVal}\","); 
                            else if (subVal is bool b) sb.Append($"{(b ? "true" : "false")},");
                            else sb.Append($"{subVal},");
                        }
                        if (sb[sb.Length - 1] == ',') sb.Length--;
                        sb.Append("},");
                    }
                    if (sb[sb.Length - 1] == ',') sb.Length--;
                    sb.Append("],");
                }
                else
                {
                    if (val is string || val == null) sb.Append($"\"{val}\",");
                    else if (val is bool b) sb.Append($"{(b ? "true" : "false")},");
                    else sb.Append($"{val},"); // Bug #8 fix: remove .ToLower() desnecessário em números; val nunca é null aqui (já tratado acima)
                }
            }
            if (sb.Length > 1 && sb[sb.Length - 1] == ',') sb.Length--;
            sb.Append("}");

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private void ServeStaticFile(HttpListenerContext context)
        {
            var response = context.Response;
            if (response == null) return;

            string filename = context.Request.Url!.AbsolutePath.Substring(1);
            if (string.IsNullOrEmpty(filename)) filename = "index.html";

            string filePath = Path.Combine(_baseFolder, filename);

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] buffer = File.ReadAllBytes(filePath);
                    response.ContentLength64 = buffer.Length;
                    if (filename.EndsWith(".html")) response.ContentType = "text/html";
                    else if (filename.EndsWith(".css")) response.ContentType = "text/css";
                    else if (filename.EndsWith(".js")) response.ContentType = "application/javascript";
                    
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                }
                catch
                {
                    response.StatusCode = 500;
                    response.Close();
                }
            }
            else
            {
                response.StatusCode = 404;
                response.Close();
            }
        }

        private void EnsureStaticFiles()
        {
            if (!Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);

            // Copy from Assets if exists (development/deployed)
            string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Web", "index.html");
            string wwwPath = Path.Combine(_baseFolder, "index.html");

            if (File.Exists(assetPath))
            {
                // Copy if missing or if asset is newer (allows updating)
                if (!File.Exists(wwwPath) || File.GetLastWriteTime(assetPath) > File.GetLastWriteTime(wwwPath))
                {
                    try { File.Copy(assetPath, wwwPath, true); } catch { }
                }
            }
            else if (!File.Exists(wwwPath))
            {
                // Minimal fallback
                try 
                {
                   File.WriteAllText(wwwPath, "<html><body style='background:#111;color:#fff;font-family:sans-serif;text-align:center;padding:50px;'><h1>Letreiro Digital</h1><p>Interface Web não encontrada. Verifique a instalação.</p></body></html>");
                } catch { }
            }
        }
        private bool CheckAuth(HttpListenerContext context)
        {
            // If no password set, everything is allowed
            if (string.IsNullOrEmpty(_password)) return true;

            string? rawUrl = context.Request.RawUrl;
            
            // 1. Check URL Parameter (first connection)
            if (rawUrl != null && rawUrl.Contains($"key={_password}")) return true;

            // 2. Check Header (API calls)
            string? token = context.Request.Headers["x-access-token"];
            if (token == _password) return true;

            // 3. Allow Static Files (index.html needs to load to ask for password)
            // But block sensitive files if any (none yet). 
            // Better strategy: API calls return 401, Index loads but frontend checks auth.
            if (rawUrl == null || !rawUrl.StartsWith("/api/")) return true;

            if (context.Response != null)
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
            }
            return false;
        }
    }
}
