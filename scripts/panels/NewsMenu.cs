using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;

public partial class NewsMenu : Control
{
    [Export] private InteractableArea2D interactArea;
    [Export] private AnimationPlayer animator;
    [Export] private RichTextLabel newsBody;
    [Export] private Container blogContainer;
    [Export] private PackedScene blogPost;
    [Export] private Sprite2D glow;

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
    private MenuInstance menu;
    private readonly System.Collections.Generic.List<Dictionary> blogs = [];

    public override void _EnterTree()
    {
        menu = new("news", animator,
        (_) => GenerateBlogs(),
        (_) =>
        {
            if (is_busy || animator.IsPlaying())
                return false;

            if (newsBody.Visible)
            {
                animator.Play("close_blog");
                return false;
            }
            return true;
        },
        null,
        () =>
        {
            for (int i = 0; i < blogContainer.GetChildCount(); i++)
                blogContainer.GetChild(i).QueueFree();
        }
        );
        GetAvailableBlogs();
        interactArea.OnInteractOnly.Add(SetMenu);
        Tween _glow = glow.CreateTween();
        _glow.TweenProperty(glow, "self_modulate", new Color(1, 1, 1, 0), 1);
        _glow.TweenProperty(glow, "self_modulate", new Color(1, 1, 1, 0.5f), 1);
        _glow.SetLoops();
        _glow.Play();
        animator.AnimationFinished += (_animation) =>
        {
            if (_animation == "close_blog")
                newsBody.Text = string.Empty;
        };
    }
    public void SetMenu() =>
        _ = CanvasManager.Menus.SetTo(menu);

    private void GenerateBlogs()
    {
        for (int i = 0; i < blogContainer.GetChildCount(); i++)
            blogContainer.GetChild(i).QueueFree();

        for (int i = 0; i < blogs.Count; i++)
        {
            int index = i;
            Control _blog = blogPost.Instantiate() as Control;
            _blog.GetChild<Label>(0).Text = blogs[i]["title"].AsString();
            _blog.GetChild<Label>(1).Text = blogs[i]["date"].AsString();
            _blog.GetChild<BaseButton>(2).Pressed += () => DisplayBlog(index);
            blogContainer.AddChild(_blog);
        }
    }
    private void GetAvailableBlogs()
    {
        RequestGetJson(PC_WEB_BLOGS + "lookUp.json", (_data) =>
        {
            for (int i = 0; i < _data.Length; i++)
            {
                if (i == 2) // i dont know why this one doesnt read correctly, nor i know why it wont stop logging even if i catch it
                    continue;

                Dictionary _blog = Json.ParseString(_data[i]).AsGodotDictionary();
                if (_blog["icon"].AsString() == "aphid")
                    blogs.Add(_blog);
            }
        });
    }

    // Web Request Methods
    private void DisplayBlog(int _index)
    {
        newsBody.Text = Tr("lobby_news_placeholder");
        animator.Play("open_blog");
        is_busy = true;

        // display the given blog post
        Dictionary _blog = blogs[_index];
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

            newsBody.Text = string.Empty;
            newsBody.AppendText($"[color=coral][font_size=60]{_blog["title"].AsString()}[/font_size][/color]");
            newsBody.AppendText($"\n[color=cyan]{DateTime.Parse(_blog["date"].AsString()):dd/MM/yy}[/color]");
            if (OptionsManager.Settings.IntFlags["Locale"].Value != 0)
                newsBody.AppendText($"\n[bgcolor=red]{Tr("warning_news_locale")}[/bgcolor]\n");
            newsBody.AppendText(_data);
            newsBody.ScrollToLine(0);

            is_busy = false;
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
        if (FileAccess.FileExists(SaveSystem.TEMP_CACHE_DIR + _filename))
        {
            newsBody.AppendText(string.Empty); // update text
            return;
        }
        GetHttpRequest("https://neverevertm.github.io/ProjectColor/img/" + _urlname,
           (_result, _response_code, _headers, _body) =>
           {
               Image image = new();
               var error = image.LoadPngFromBuffer(_body);

               if (error != Error.Ok)
               {
                   DebugLogger.Print(DebugLogger.LogPriority.Warning, $"NewsMenu: <{_urlname}> could not be loaded.");
                   return;
               }
               ImageTexture texture = ImageTexture.CreateFromImage(image);
               ResourceSaver.Save(texture, SaveSystem.TEMP_CACHE_DIR + _filename);
               newsBody.AppendText(string.Empty); // update text
           });
    }

    private static bool IsValidImage(string _urlname) =>
        _urlname.ToLower().EndsWith(".png");
}
