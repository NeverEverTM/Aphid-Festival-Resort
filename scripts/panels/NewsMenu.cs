using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;

public partial class NewsMenu : Control, MenuTrigger.ITrigger
{
    private MenuInstance menu;

    [Export] private AnimationPlayer player;
    [Export] private RichTextLabel newsBody;

    private const string PC_WEB_BLOGS = "https://neverevertm.github.io/ProjectColor/blogs/"; // Redirects to the github-hosted page for project color
    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex HTML_REMOVE();

    [GeneratedRegex(@"\n+\s+")]
    private static partial Regex FORMATTER();

    [GeneratedRegex(@"<(?=p|ul|\/p|\/ul)")]
    private static partial Regex CONVERTER_LEFT();

    [GeneratedRegex(@"(?<=p|\[p[^>]+|ul|\/ul)>")]
    private static partial Regex CONVERTER_RIGHT();

    [GeneratedRegex(@"<img[^>]+>")]
    private static partial Regex CONVERTER_IMG();

    [GeneratedRegex(@"<h2[^>]*>")]
    private static partial Regex CONVERTER_H2_LEFT();

    [GeneratedRegex(@"<\/h2>")]
    private static partial Regex CONVERTER_H2_RIGHT();

    [GeneratedRegex("(?<=src=\")([^\"]+)")]
    private static partial Regex IMG_SRC_RECOVERY();

    private bool is_busy;

    public override void _EnterTree()
    {
        menu = new("news", player, (_) =>
        {
            if (!is_busy)
            {
                newsBody.Text = Tr("lobby_news_placeholder");
                RequestGetJson(PC_WEB_BLOGS + "lookUp.json", (_data) => DisplayWebPage(3));
            }
        }, null);
    }
    public void SetMenu() =>
        _ = CanvasManager.Menus.SetTo(menu);

    private void DisplayWebPage(int _index)
    {
        newsBody.Text = "";
        is_busy = true;
        RequestGetJson(PC_WEB_BLOGS + "lookUp.json", (_data) =>
        {
            Dictionary _blog = Json.ParseString(_data[_index]).AsGodotDictionary();
            newsBody.Text += $"[color=coral][font_size=60]{_blog["title"].AsString()}[/font_size][/color]";
            newsBody.Text += $"\n[color=cyan]{DateTime.Parse(_blog["date"].AsString()):dd/MM/yy}[/color]";
            if (OptionsManager.Settings.Locale != OptionsManager.DEFAULT_LOCALE)
                newsBody.Text += $"\n[bgcolor=red]{Tr("warning_news_locale")}[/bgcolor]\n";
            RequestGetWebPage(PC_WEB_BLOGS + _blog["name"].AsString() + "-content.html", (_data) =>
            {
                var _matches = IMG_SRC_RECOVERY().Matches(_data);

                _data = CONVERTER_LEFT().Replace(_data, "[");
                _data = CONVERTER_RIGHT().Replace(_data, "]");
                _data = CONVERTER_H2_LEFT().Replace(_data, "[font_size=50][color=pink]");
                _data = CONVERTER_H2_RIGHT().Replace(_data, "[/color][/font_size]");
                _data = CONVERTER_IMG().Replace(_data, "[center][img=800]IMG_PLACEHOLDER[/img][/center]");

                for (int i = 0; i < _matches.Count; i++)
                {
                    int index = _data.IndexOf("IMG_PLACEHOLDER");
                    string _url = _matches[i].Value.Split('/')[1],
                        _filename = _url.Split('.')[0] + ".res";

                    _data = _data.Remove(index, "IMG_PLACEHOLDER".Length);
                    if (IsValidImage(_url))
                    {
                        _data = _data.Insert(index, SaveSystem.TEMP_CACHE_DIR + _filename);
                        RequestGetImage(_url, _filename);
                    }
                    else
                        _data = _data.Insert(index, "uid://pgscnb8dl5jr");
                }

                _data = HTML_REMOVE().Replace(_data, string.Empty);
                _data = FORMATTER().Replace(_data, "\n");

                newsBody.Text += _data;
                is_busy = false;
            });
        });
    }
    private HttpRequest GetHttpRequest(string _url, HttpRequest.RequestCompletedEventHandler _on_request_completed)
    {
        HttpRequest _request = new();
        AddChild(_request);
        _request.RequestCompleted += _on_request_completed;
        _request.RequestCompleted += (_result, _response_code, _headers, _body) =>
            _request.QueueFree();
        _request.Request(_url);
        return _request;
    }
    private void RequestGetJson(string _url, Action<string[]> _onRequestCompleted = null)
    {
        GetHttpRequest(_url, (_result, _response_code, _headers, _body) =>
        {
            string[] _data = Json.ParseString(StringExtensions.GetStringFromUtf8(_body)).AsStringArray();
            _onRequestCompleted?.Invoke(_data);
        });
    }
    private void RequestGetWebPage(string _url, Action<string> _onRequestCompleted = null)
    {
        GetHttpRequest(_url, (_result, _response_code, _headers, _body) =>
        {
            string _data = StringExtensions.GetStringFromUtf8(_body);
            _onRequestCompleted?.Invoke(_data);
        });
    }
    private void RequestGetImage(string _urlname, string _filename)
    {
        GetHttpRequest("https://neverevertm.github.io/ProjectColor/img/" + _urlname,
           (_result, _response_code, _headers, _body) =>
           {
               Image image = new();
               var error = image.LoadPngFromBuffer(_body);

               if (error != Error.Ok)
               {
                   Logger.Print(Logger.LogPriority.Warning, $"NewsMenu: <{_urlname}> could not be loaded.");
                   return;
               }
               ImageTexture texture = ImageTexture.CreateFromImage(image);
               ResourceSaver.Save(texture, SaveSystem.TEMP_CACHE_DIR + _filename);
               newsBody.Text += string.Empty;
           });
    }

    private static bool IsValidImage(string _urlname) =>
        _urlname.ToLower().EndsWith(".png");
}
