using System.Net;
using System.Net.Sockets;

namespace Puerto92.Helpers
{
    /// <summary>
    /// Helper para obtener la dirección IP real del cliente
    /// </summary>
    public static class IPHelper
    {
        /// <summary>
        /// Obtiene la dirección IP real del cliente
        /// </summary>
        /// <param name="httpContext">Contexto HTTP actual</param>
        /// <returns>Dirección IP del cliente</returns>
        public static string ObtenerDireccionIPReal(HttpContext? httpContext)
        {
            if (httpContext == null)
                return ObtenerIPLocalMaquina();

            // 1. Intentar obtener IP de headers (si está detrás de proxy/load balancer)
            var ipFromHeaders = ObtenerIPDesdeHeaders(httpContext);
            if (!string.IsNullOrEmpty(ipFromHeaders) && !EsIPLocal(ipFromHeaders))
                return ipFromHeaders;

            // 2. Obtener IP de la conexión remota
            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp) && !EsIPLocal(remoteIp))
                return remoteIp;

            // 3. Si es localhost, obtener la IP real de la máquina en la red local
            return ObtenerIPLocalMaquina();
        }

        /// <summary>
        /// Obtiene la IP desde los headers de la solicitud HTTP
        /// </summary>
        private static string? ObtenerIPDesdeHeaders(HttpContext httpContext)
        {
            // Header X-Forwarded-For (usado por proxies)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                {
                    var ip = ips[0].Trim();
                    if (!string.IsNullOrEmpty(ip))
                        return ip;
                }
            }

            // Header X-Real-IP (usado por Nginx y otros)
            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp.Trim();

            return null;
        }

        /// <summary>
        /// Verifica si una dirección IP es localhost
        /// </summary>
        private static bool EsIPLocal(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return true;

            // Normalizar la IP
            ip = ip.Trim();

            // IPv6 localhost
            if (ip == "::1" || ip == "0:0:0:0:0:0:0:1")
                return true;

            // IPv4 localhost
            if (ip == "127.0.0.1" || ip.StartsWith("127."))
                return true;

            return false;
        }

        /// <summary>
        /// Obtiene la dirección IP real de la máquina en la red local
        /// </summary>
        public static string ObtenerIPLocalMaquina()
        {
            try
            {
                // Obtener todas las interfaces de red
                var host = Dns.GetHostEntry(Dns.GetHostName());
                
                // Buscar la primera dirección IPv4 que no sea localhost
                foreach (var ip in host.AddressList)
                {
                    // Solo IPv4 y que no sea localhost
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipString = ip.ToString();
                        if (!EsIPLocal(ipString))
                            return ipString;
                    }
                }

                // Si no encuentra IPv4, buscar IPv6 que no sea localhost
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        var ipString = ip.ToString();
                        if (!EsIPLocal(ipString))
                            return ipString;
                    }
                }
            }
            catch (Exception)
            {
                // Si falla, devolver localhost
            }

            return "127.0.0.1";
        }

        /// <summary>
        /// Formatea la dirección IP para mostrarla de forma más legible
        /// </summary>
        public static string FormatearIP(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return "Desconocida";

            // Si es IPv6 localhost, convertir a IPv4
            if (ip == "::1")
                return "127.0.0.1 (localhost)";

            // Si es IPv4 localhost, agregar nota
            if (ip == "127.0.0.1")
                return "127.0.0.1 (localhost)";

            return ip;
        }

        /// <summary>
        /// Obtiene información detallada de la IP para auditoría
        /// </summary>
        public static string ObtenerInformacionIP(HttpContext? httpContext)
        {
            if (httpContext == null)
                return $"IP Local: {ObtenerIPLocalMaquina()}";

            var ipReal = ObtenerDireccionIPReal(httpContext);
            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A";
            var localIp = httpContext.Connection.LocalIpAddress?.ToString() ?? "N/A";

            return $"IP: {ipReal} (Remote: {remoteIp}, Local: {localIp})";
        }
    }
}