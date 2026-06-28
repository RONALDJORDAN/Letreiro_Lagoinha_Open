using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace LetreiroDigital.Services
{
    public class HardwareId
    {
        public static string GetMotherboardSerial()
        {
            string serial = "";
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject share in searcher.Get())
                {
                    serial = share["SerialNumber"]?.ToString()?.Trim() ?? "";
                }
            }
            catch { serial = "OFFLINE-ID-GENERIC"; }
            
            if (string.IsNullOrEmpty(serial)) serial = "GENERIC-DEV-ID";

            // Transformamos em Hash para não expor o serial real e manter um padrão
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(serial));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 16);
            }
        }

        public static object GetMachineInfo()
        {
            return new
            {
                name = Environment.MachineName,
                os = Environment.OSVersion.ToString(),
                user = Environment.UserName,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}
