using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PlugNPayHub.PosControl.Messages;
using PlugNPayHub.Utils;

namespace PlugNPayHub.PosControl
{
    class PosHub
    {
        private static readonly Encoding CurrentEncoding = Encoding.UTF8;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private HttpListener _httpListener;
        private readonly Dictionary<string, Func<Pos, string, Task<IResponse>>> _actions = new Dictionary<string, Func<Pos, string, Task<IResponse>>>();
        private readonly Dictionary<string, Pos> _poses = new Dictionary<string, Pos>();

        public void Start(string bindUrl)
        {
            Ensure.NotNull(bindUrl, nameof(bindUrl));

            _httpListener = new HttpListener { IgnoreWriteExceptions = false };
            _httpListener.Prefixes.Add(bindUrl);
            _httpListener.Start();


            AsyncCallback acceptConnection = null;
            acceptConnection = state =>
            {
                try
                {
                    HttpListenerContext clientContext = _httpListener.EndGetContext(state);
                    Log.Info($"New Ecr connection from: {clientContext.Request.RemoteEndPoint}");

                    ProcessAsync(clientContext);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    _httpListener.BeginGetContext(acceptConnection, null);
                }
            };

            _httpListener.BeginGetContext(acceptConnection, null);

            RegisterActions();
        }

        public void Stop()
        {
            try
            {
                if (!_httpListener.IsListening)
                    return;

                _httpListener.Stop();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public Pos RegisterPos(Pos pos)
        {
            Ensure.NotNull(pos, nameof(pos));
            Ensure.NotNull(pos.PosId, nameof(pos.PosId));

            return _poses[pos.PosId] = pos;
        }

        #region Process
        
        private async void ProcessAsync(HttpListenerContext context)
        {
            try
            {
                IResponse response = new Response { Result = ResponseResults.Error };

                try
                {
                    Ensure.NotNull(context, nameof(context));

                    string[] url = context.Request.RawUrl.Split('/');
                    string actionName = url[url.Length - 1].ToLower();

                    string content = null;

                    if (context.Request.ContentLength64 != 0)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (Stream input = context.Request.InputStream)
                            {
                                int readBytes;
                                byte[] buffer = new byte[1024];

                                while ((readBytes = await input.ReadAsync(buffer, 0, buffer.Length)) != 0 && ms.Length < context.Request.ContentLength64)
                                    ms.Write(buffer, 0, readBytes);
                            }

                            content = CurrentEncoding.GetString(ms.ToArray());
                        }
                    }

                    response = await ProcessActionAsync(actionName, content);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);

                    response.Text = ex.Message;
                }
                finally
                {
                    try
                    {
                        string responseContent;

                        using (context.Response)
                        {
                            context.Response.ContentEncoding = CurrentEncoding;
                            context.Response.ContentType = "application/json";

                            using (Stream output = context.Response.OutputStream)
                            {
                                byte[] data = CurrentEncoding.GetBytes(responseContent = Converter.Serialize(response));
                                output.Write(data, 0, data.Length);
                            }
                        }

                        Log.Info($"Response:{Environment.NewLine}{responseContent}{Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error on sending response, {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private async Task<IResponse> ProcessActionAsync(string actionName, string receivedData)
        {
            Ensure.NotNull(actionName, nameof(actionName));
            Ensure.NotNull(receivedData, nameof(receivedData));

            Func<Pos, string, Task<IResponse>> action;
            if (!_actions.TryGetValue(actionName, out action))
                throw new NotSupportedException($"Received request [{actionName}] is not supported");

            Log.Info($"Request{Environment.NewLine}{actionName}:{Environment.NewLine}{receivedData}{Environment.NewLine}");

            Request request = Converter.Deserialize<Request>(receivedData);
            if (request == null)
                throw new Exception($"Received request cannot be deserialized: {receivedData}");

            if (string.IsNullOrEmpty(request.PosId))
                throw new Exception($"Received request missing '{nameof(request.PosId)}': {receivedData}");

            Pos pos;
            if (!_poses.TryGetValue(request.PosId, out pos) || pos == null)
                throw new Exception($"Pos '{request.PosId}' is not found");

            if (string.IsNullOrEmpty(request.Content))
                throw new Exception($"Received request {request.Content} is null");

            return await action(pos, request.Content);
        }

        #endregion

        #region Actions

        private void RegisterActions()
        {
            RegisterAction(nameof(Authorize), Authorize);
            RegisterAction(nameof(Confirm), Confirm);
        }

        private void RegisterAction(string actionName, Func<Pos, string, Task<IResponse>> onAction)
        {
            Ensure.NotNull(actionName, nameof(actionName));
            Ensure.NotNull(onAction, nameof(onAction));

            _actions[actionName.ToLower()] = onAction;
        }

        private async Task<IResponse> Authorize(Pos pos, string content)
        {
            Ensure.NotNull(pos, nameof(pos));
            Ensure.NotNull(content, nameof(content));

            AuthorizeRequest authRequest = Converter.Deserialize<AuthorizeRequest>(content);
            if (authRequest == null)
                throw new Exception($"Received request cannot {nameof(content)} be deserialized: {content}");

            return await pos.AuthorizeAsync(authRequest);
        }

        private async Task<IResponse> Confirm(Pos pos, string content)
        {
            Ensure.NotNull(pos, nameof(pos));
            Ensure.NotNull(content, nameof(content));

            ConfirmRequest confirmRequest = Converter.Deserialize<ConfirmRequest>(content);
            if (confirmRequest == null)
                throw new Exception($"Received request cannot {nameof(content)} be deserialized: {content}");

            return await pos.ConfirmAsync(confirmRequest);
        }

        #endregion
    }
}
