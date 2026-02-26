using System.Net;
using System.Web;

namespace BMS.ControlPanel.Services;

/// <summary>
/// Handles Discord OAuth callback by listening on a local HTTP server
/// </summary>
public class OAuthCallbackHandler : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;
    private const int LocalPort = 3000;
    public const string LocalCallbackUrl = "http://localhost:3000/oauth/callback";

    public event EventHandler<OAuthCallbackEventArgs>? CallbackReceived;

    public void StartListening()
    {
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{LocalPort}/");
            _listener.Start();
            
            _cancellationTokenSource = new CancellationTokenSource();
            _listenerTask = ListenForCallbackAsync(_cancellationTokenSource.Token);
            
            System.Diagnostics.Debug.WriteLine("OAuth callback listener started on port 3000");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting OAuth listener: {ex.Message}");
        }
    }

    public void StopListening()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            System.Diagnostics.Debug.WriteLine("OAuth callback listener stopped");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping OAuth listener: {ex.Message}");
        }
    }

    private async Task ListenForCallbackAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                try
                {
                    // Parse the callback URL
                    if (request.RawUrl?.StartsWith("/oauth/callback") == true)
                    {
                        var queryString = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
                        var code = queryString["code"];
                        var state = queryString["state"];
                        var error = queryString["error"];
                        var errorDescription = queryString["error_description"];

                        if (!string.IsNullOrEmpty(error))
                        {
                            CallbackReceived?.Invoke(this, new OAuthCallbackEventArgs
                            {
                                Success = false,
                                Error = error,
                                ErrorDescription = errorDescription
                            });

                            SendErrorResponse(response, error, errorDescription);
                        }
                        else if (!string.IsNullOrEmpty(code))
                        {
                            CallbackReceived?.Invoke(this, new OAuthCallbackEventArgs
                            {
                                Success = true,
                                Code = code,
                                State = state
                            });

                            SendSuccessResponse(response);
                        }
                        else
                        {
                            SendErrorResponse(response, "invalid_request", "No authorization code received");
                        }
                    }
                    else
                    {
                        SendErrorResponse(response, "invalid_request", "Invalid callback URL");
                    }
                }
                finally
                {
                    response.Close();
                }
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Operation aborted
            {
                // This is expected when stopping the listener
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing OAuth callback: {ex.Message}");
            }
        }
    }

    private void SendSuccessResponse(HttpListenerResponse response)
    {
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Login Successful</title>
    <style>
        body { font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #1a1a2e; color: white; }
        .container { text-align: center; }
        h1 { color: #00d4ff; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>✓ Login Successful</h1>
        <p>You have successfully logged in. You can close this window.</p>
        <script>
            setTimeout(function() { window.close(); }, 2000);
        </script>
    </div>
</body>
</html>";
        var buffer = System.Text.Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private void SendErrorResponse(HttpListenerResponse response, string error, string? errorDescription)
    {
        response.StatusCode = 400;
        response.ContentType = "text/html; charset=utf-8";
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Login Failed</title>
    <style>
        body {{ font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #1a1a2e; color: white; }}
        .container {{ text-align: center; }}
        h1 {{ color: #e94560; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>✗ Login Failed</h1>
        <p><strong>{error}</strong></p>
        {(string.IsNullOrEmpty(errorDescription) ? "" : $"<p>{errorDescription}</p>")}
        <p>You can close this window.</p>
    </div>
</body>
</html>";
        var buffer = System.Text.Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    public void Dispose()
    {
        StopListening();
        _cancellationTokenSource?.Dispose();
    }
}

public class OAuthCallbackEventArgs : EventArgs
{
    public bool Success { get; set; }
    public string? Code { get; set; }
    public string? State { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}
